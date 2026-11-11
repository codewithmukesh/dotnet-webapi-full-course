using System.Collections.Concurrent;

namespace Ticketing.Api.Features.Events;

// In-memory store until chapter 4. Holds state, so it MUST be a singleton.
public interface IEventStore
{
    IReadOnlyCollection<EventResponse> GetAll();
    EventResponse? Find(Guid id);
    void Add(EventResponse item);
}

public sealed class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<Guid, EventResponse> _events = new();

    public IReadOnlyCollection<EventResponse> GetAll() => [.. _events.Values];

    public EventResponse? Find(Guid id) => _events.GetValueOrDefault(id);

    public void Add(EventResponse item) => _events[item.Id] = item;
}
