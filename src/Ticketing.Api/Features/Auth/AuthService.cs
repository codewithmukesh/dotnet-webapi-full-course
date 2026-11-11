using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Ticketing.Api.Common;
using Ticketing.Api.Data;
using Ticketing.Api.Domain;

namespace Ticketing.Api.Features.Auth;

public sealed partial class AuthService(
    TicketingDbContext db,
    TokenService tokens,
    IPasswordHasher<User> hasher,
    IOptions<JwtOptions> jwt,
    TimeProvider clock,
    ILogger<AuthService> logger)
{
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var user = new User
        {
            Email = request.Email.Trim().ToLowerInvariant(),
            Role = request.AccountType == AccountType.Organizer ? UserRole.Organizer : UserRole.Attendee,
            CreatedAt = clock.GetUtcNow(),
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // The unique index is the real check. Two parallel sign-ups with one email can't both win.
            return AuthErrors.EmailTaken;
        }

        return await IssueTokensAsync(user, ct);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            return AuthErrors.InvalidCredentials;
        }

        var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return AuthErrors.InvalidCredentials;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, request.Password);
        }

        return await IssueTokensAsync(user, ct);
    }

    // Rotation: every refresh token works once. Using it returns a new pair and retires the old one.
    public async Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var hash = TokenService.Hash(request.RefreshToken);
        var stored = await db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        // Reuse detection: a rotated token came back. Someone has a copy. Kill every session of this user.
        if (stored.ReplacedById is not null)
        {
            LogRefreshTokenReuse(logger, stored.UserId);
            await RevokeAllAsync(stored.UserId, now, ct);
            return AuthErrors.InvalidRefreshToken;
        }

        if (!stored.IsActive(now))
        {
            return AuthErrors.InvalidRefreshToken;
        }

        var result = await IssueTokensAsync(stored.User, ct, replacing: stored);
        return result;
    }

    public async Task<Result> RevokeAsync(RefreshRequest request, Guid userId, CancellationToken ct)
    {
        var hash = TokenService.Hash(request.RefreshToken);
        var revoked = await db.RefreshTokens
            .Where(t => t.TokenHash == hash && t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, clock.GetUtcNow()), ct);

        return revoked == 0 ? AuthErrors.InvalidRefreshToken : Result.Success();
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct, RefreshToken? replacing = null)
    {
        var now = clock.GetUtcNow();
        var (accessToken, expiresAt) = tokens.CreateAccessToken(user);
        var rawRefreshToken = TokenService.CreateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenService.Hash(rawRefreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(jwt.Value.RefreshTokenDays),
        };
        db.RefreshTokens.Add(refreshToken);

        if (replacing is not null)
        {
            replacing.RevokedAt = now;
            replacing.ReplacedById = refreshToken.Id;
        }

        await db.SaveChangesAsync(ct);
        return new AuthResponse(accessToken, expiresAt, rawRefreshToken);
    }

    private Task<int> RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken ct) =>
        db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refresh token reuse detected for user {UserId}. All sessions revoked.")]
    private static partial void LogRefreshTokenReuse(ILogger logger, Guid userId);
}
