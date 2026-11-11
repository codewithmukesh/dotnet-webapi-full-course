namespace Ticketing.Api.Domain;

public enum UserRole
{
    Attendee,
    Organizer,
    Admin,
}

public sealed class User
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Email { get; init; }
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; init; } = UserRole.Attendee;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class RefreshToken
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid UserId { get; init; }

    // Only the SHA-256 hash is stored. A database leak doesn't hand out working tokens.
    public required string TokenHash { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? ReplacedById { get; set; }

    public User User { get; init; } = null!;

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
