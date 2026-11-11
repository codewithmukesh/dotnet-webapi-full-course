using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Ticketing.Api.Domain;

namespace Ticketing.Api.Features.Auth;

public static class Policies
{
    // Role check: "is this person an organizer at all?" Runs before the endpoint.
    public const string Organizer = nameof(Organizer);

    // Resource check: "does this organizer own THIS event?" Needs the event, so it runs inside the service.
    public const string EventOwner = nameof(EventOwner);
}

public sealed class EventOwnerRequirement : IAuthorizationRequirement;

public sealed class EventOwnerHandler : AuthorizationHandler<EventOwnerRequirement, Event>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, EventOwnerRequirement requirement, Event resource)
    {
        var isOwner = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value == resource.OrganizerId.ToString();
        if (isOwner || context.User.IsInRole(nameof(UserRole.Admin)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
