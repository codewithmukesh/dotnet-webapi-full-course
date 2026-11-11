namespace Ticketing.Api.Features.Events;

public sealed record EventResponse(Guid Id, string Title, string Venue, DateTimeOffset StartsAt);

public sealed record CreateEventRequest(string Title, string Venue, DateTimeOffset StartsAt);
