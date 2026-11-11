using FluentValidation;
using Ticketing.Api.Common;

namespace Ticketing.Api.Features.Auth;

public enum AccountType
{
    Attendee,
    Organizer,
}

public sealed record RegisterRequest(string Email, string Password, AccountType AccountType = AccountType.Attendee);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken);

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(128);
        RuleFor(x => x.AccountType).IsInEnum();
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(100);
}

public static class AuthErrors
{
    // Same message for "no such email" and "wrong password": don't tell attackers which emails exist.
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("auth.invalid_credentials", "Invalid email or password.");

    public static readonly Error EmailTaken =
        Error.Conflict("auth.email_taken", "An account with this email already exists.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("auth.invalid_refresh_token", "The refresh token is invalid or expired. Log in again.");
}
