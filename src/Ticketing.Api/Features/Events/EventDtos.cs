using Ticketing.Api.Domain;

namespace Ticketing.Api.Features.Events;

public sealed record EventSummaryResponse(
    Guid Id,
    string Title,
    string Venue,
    string City,
    DateTimeOffset StartsAt,
    string Status,
    decimal? LowestPrice,
    int TicketsLeft);

public sealed record EventDetailsResponse(
    Guid Id,
    string Title,
    string? Description,
    string Venue,
    string City,
    DateTimeOffset StartsAt,
    string Status,
    IReadOnlyList<TicketTypeResponse> TicketTypes);

public sealed record TicketTypeResponse(
    Guid Id,
    string Name,
    decimal Price,
    int TotalQuantity,
    int QuantityLeft,
    int MaxPerUser);

public sealed record CreateEventRequest(
    string Title,
    string? Description,
    string Venue,
    string City,
    DateTimeOffset StartsAt);

public sealed record UpdateEventRequest(
    string Title,
    string? Description,
    string Venue,
    string City,
    DateTimeOffset StartsAt);

public sealed record UpsertTicketTypeRequest(
    string Name,
    decimal Price,
    int TotalQuantity,
    int MaxPerUser);

// Manual mapping. No AutoMapper: it went commercial, and this is 20 lines you can read.
internal static class EventMappings
{
    public static EventDetailsResponse ToDetails(this Event e) => new(
        e.Id,
        e.Title,
        e.Description,
        e.Venue.Name,
        e.Venue.City,
        e.StartsAt,
        e.Status.ToString(),
        [.. e.TicketTypes.OrderBy(t => t.Price).Select(t => t.ToResponse())]);

    public static TicketTypeResponse ToResponse(this TicketType t) =>
        new(t.Id, t.Name, t.Price, t.TotalQuantity, t.QuantityLeft, t.MaxPerUser);
}
