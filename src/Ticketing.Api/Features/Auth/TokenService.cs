using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Ticketing.Api.Domain;

namespace Ticketing.Api.Features.Auth;

public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider clock)
{
    private readonly JwtOptions _jwt = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user)
    {
        var now = clock.GetUtcNow();
        var expiresAt = now.AddMinutes(_jwt.AccessTokenMinutes);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("role", user.Role.ToString()),
            ]),
            SigningCredentials = new SigningCredentials(SigningKey(_jwt), SecurityAlgorithms.HmacSha256),
        });

        return (token, expiresAt);
    }

    // A refresh token is just 256 random bits. The client gets the raw value, the database gets the hash.
    public static string CreateRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    public static SymmetricSecurityKey SigningKey(JwtOptions jwt) => new(Encoding.UTF8.GetBytes(jwt.SigningKey));
}
