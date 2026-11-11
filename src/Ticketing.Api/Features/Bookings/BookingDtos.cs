using Ticketing.Api.Domain;

namespace Ticketing.Api.Features.Bookings;

// UserId comes from the request body until chapter 8 replaces it with the logged-in user.
public sealed record BookTicketRequest(Guid TicketTypeId, Guid UserId, int Quantity);

public sealed record BookingResponse(
    Guid Id,
    Guid TicketTypeId,
    string TicketType,
    Guid EventId,
    string Event,
    int Quantity,
    decimal Total,
    string Status,
    DateTimeOffset CreatedAt);

internal static class BookingMappings
{
    public static BookingResponse ToResponse(this Booking b, TicketType t) => new(
        b.Id,
        t.Id,
        t.Name,
        t.EventId,
        t.Event.Title,
        b.Quantity,
        b.Quantity * t.Price,
        b.Status.ToString(),
        b.CreatedAt);
}
