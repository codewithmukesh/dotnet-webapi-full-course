using System.ComponentModel.DataAnnotations;

namespace Ticketing.Api.Features.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    // HMAC-SHA256 needs at least 256 bits. A short key fails at boot, not at the first login.
    [Required, MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    [Range(1, 60)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; init; } = 7;
}
