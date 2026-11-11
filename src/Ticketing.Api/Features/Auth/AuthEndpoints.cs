using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Ticketing.Api.Common;
using Ticketing.Api.Domain;

namespace Ticketing.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IServiceCollection AddAuthFeature(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configure JwtBearer from the validated JwtOptions instead of reading config twice.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false; // keep "sub" and "role" as-is, no legacy SOAP claim URIs
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = TokenService.SigningKey(jwt.Value),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.FromSeconds(30), // default is 5 minutes on top of the token lifetime
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Organizer, policy => policy.RequireRole(nameof(UserRole.Organizer), nameof(UserRole.Admin)))
            .AddPolicy(Policies.EventOwner, policy => policy.AddRequirements(new EventOwnerRequirement()));

        services.AddSingleton<IAuthorizationHandler, EventOwnerHandler>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<TokenService>();
        services.AddScoped<AuthService>();
        services.AddScoped<CurrentUser>();
        services.AddHttpContextAccessor();

        return services;
    }

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, AuthService auth, CancellationToken ct) =>
            (await auth.RegisterAsync(request, ct)).ToOk())
        .WithValidation<RegisterRequest>();

        group.MapPost("/login", async (LoginRequest request, AuthService auth, CancellationToken ct) =>
            (await auth.LoginAsync(request, ct)).ToOk())
        .WithValidation<LoginRequest>();

        group.MapPost("/refresh", async (RefreshRequest request, AuthService auth, CancellationToken ct) =>
            (await auth.RefreshAsync(request, ct)).ToOk())
        .WithValidation<RefreshRequest>();

        group.MapPost("/revoke", async (RefreshRequest request, AuthService auth, CurrentUser user, CancellationToken ct) =>
            (await auth.RevokeAsync(request, user.Id, ct)).ToNoContent())
        .WithValidation<RefreshRequest>()
        .RequireAuthorization();

        return app;
    }
}
