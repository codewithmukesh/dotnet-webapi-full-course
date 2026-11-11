namespace Ticketing.Api.Domain;

public enum EventStatus
{
    Draft,
    Published,
    Cancelled,
}

public sealed class Event
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required Venue Venue { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public Guid OrganizerId { get; init; }
    public EventStatus Status { get; set; } = EventStatus.Draft;
    public DateTimeOffset CreatedAt { get; init; }

    public List<TicketType> TicketTypes { get; init; } = [];
}

// Complex type: stored as columns on the events table, no table and no Id of its own.
public sealed record Venue(string Name, string City);
