namespace Ticketing.Api.Features.Events;

// Per-request business logic. Scoped: one instance per HTTP request.
public sealed class EventService(IEventStore store, TimeProvider clock, ILogger<EventService> logger)
{
    public IReadOnlyCollection<EventResponse> List() =>
        [.. store.GetAll().Where(e => e.StartsAt > clock.GetUtcNow()).OrderBy(e => e.StartsAt)];

    public EventResponse? Get(Guid id) => store.Find(id);

    public EventResponse Create(CreateEventRequest request)
    {
        var created = new EventResponse(Guid.CreateVersion7(), request.Title, request.Venue, request.StartsAt);
        store.Add(created);
        logger.EventCreated(created.Title, created.Id, created.Venue);
        return created;
    }
}
