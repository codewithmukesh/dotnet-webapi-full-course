using Ticketing.Api.Common;

namespace Ticketing.Api.Features.Events;

public static class EventErrors
{
    public static readonly Error NotFound =
        Error.NotFound("events.not_found", "The event was not found.");

    public static readonly Error TicketTypeNotFound =
        Error.NotFound("events.ticket_type_not_found", "The ticket type was not found.");

    public static readonly Error NotOwner =
        Error.Forbidden("events.not_owner", "Only the event's organizer can do this.");

    public static readonly Error NotDraft =
        Error.Conflict("events.not_draft", "Only a draft event can be published.");

    public static readonly Error NoTicketTypes =
        Error.Conflict("events.no_ticket_types", "Add at least one ticket type before publishing.");

    public static readonly Error AlreadyStarted =
        Error.Conflict("events.already_started", "The event has already started.");

    public static readonly Error QuantityBelowSold =
        Error.Conflict("events.quantity_below_sold", "Total quantity can't be lower than the tickets already sold.");

    public static readonly Error ConcurrentUpdate =
        Error.Conflict("events.concurrent_update", "Someone else changed this ticket type. Reload and try again.");
}
