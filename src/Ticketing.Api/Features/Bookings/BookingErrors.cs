using Ticketing.Api.Common;

namespace Ticketing.Api.Features.Bookings;

public static class BookingErrors
{
    public static readonly Error NotFound =
        Error.NotFound("bookings.not_found", "The booking was not found.");

    public static readonly Error TicketTypeNotFound =
        Error.NotFound("bookings.ticket_type_not_found", "The ticket type was not found.");

    public static readonly Error NotOnSale =
        Error.Conflict("bookings.not_on_sale", "Tickets for this event are not on sale.");

    public static readonly Error EventStarted =
        Error.Conflict("bookings.event_started", "The event has already started.");

    public static readonly Error SoldOut =
        Error.Conflict("bookings.sold_out", "Sold out.");

    public static readonly Error AlreadyCancelled =
        Error.Conflict("bookings.already_cancelled", "The booking is already cancelled.");

    public static Error LimitExceeded(int maxPerUser) =>
        Error.Conflict("bookings.limit_exceeded", $"You can book at most {maxPerUser} tickets of this type.");
}
