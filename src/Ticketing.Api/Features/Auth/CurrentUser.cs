using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Ticketing.Api.Features.Auth;

// The logged-in user, read from the validated access token. Scoped: one per request.
public sealed class CurrentUser(IHttpContextAccessor accessor)
{
    public ClaimsPrincipal Principal => accessor.HttpContext?.User ?? new ClaimsPrincipal();

    public Guid Id => Guid.TryParse(Principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
        ? id
        : throw new InvalidOperationException("No authenticated user. Did the endpoint forget RequireAuthorization()?");
}
