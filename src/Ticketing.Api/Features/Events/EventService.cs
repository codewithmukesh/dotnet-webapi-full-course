using Microsoft.EntityFrameworkCore;
using Ticketing.Api.Common;
using Ticketing.Api.Data;
using Ticketing.Api.Domain;

namespace Ticketing.Api.Features.Events;

public sealed class EventService(TicketingDbContext db, TimeProvider clock, ILogger<EventService> logger)
{
    public async Task<PagedResponse<EventSummaryResponse>> ListAsync(EventQuery query, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var events = db.Events.Where(e => e.StartsAt > now);

        // Filters: each one is optional and only added when present. No query monster.
        if (!string.IsNullOrWhiteSpace(query.City))
        {
            events = events.Where(e => e.Venue.City == query.City);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            events = events.Where(e => EF.Functions.ILike(e.Title, $"%{query.Search}%"));
        }

        if (query.From is { } from)
        {
            var fromUtc = from.ToUniversalTime();
            events = events.Where(e => e.StartsAt >= fromUtc);
        }

        if (query.To is { } to)
        {
            var toUtc = to.ToUniversalTime();
            events = events.Where(e => e.StartsAt < toUtc);
        }

        // Keyset pagination: "rows after the last one I saw", using the (StartsAt, Id) index.
        // The cost is the same for page 1 and page 40,000. OFFSET has to walk past every skipped row.
        if (EventCursor.TryDecode(query.Cursor, out var cursor))
        {
            events = events.Where(e => EF.Functions.GreaterThan(
                ValueTuple.Create(e.StartsAt, e.Id),
                ValueTuple.Create(cursor.StartsAt, cursor.Id)));
        }

        // Fetch one extra row to know if there is a next page.
        var items = await events
            .OrderBy(e => e.StartsAt).ThenBy(e => e.Id)
            .Take(query.PageSize + 1)
            .Select(e => new EventSummaryResponse(
                e.Id,
                e.Title,
                e.Venue.Name,
                e.Venue.City,
                e.StartsAt,
                e.Status.ToString(),
                e.TicketTypes.Min(t => (decimal?)t.Price),
                e.TicketTypes.Sum(t => t.QuantityLeft)))
            .ToListAsync(ct);

        if (items.Count <= query.PageSize)
        {
            return new PagedResponse<EventSummaryResponse>(items, null);
        }

        items.RemoveAt(items.Count - 1);
        var last = items[^1];
        return new PagedResponse<EventSummaryResponse>(items, new EventCursor(last.StartsAt, last.Id).Encode());
    }

    public async Task<Result<EventDetailsResponse>> GetAsync(Guid id, CancellationToken ct)
    {
        var found = await db.Events
            .AsNoTracking()
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        return found is null ? EventErrors.NotFound : found.ToDetails();
    }

    public async Task<EventDetailsResponse> CreateAsync(UpsertEventRequest request, CancellationToken ct)
    {
        var created = new Event
        {
            Title = request.Title,
            Description = request.Description,
            Venue = new Venue(request.Venue, request.City),
            StartsAt = request.StartsAt.ToUniversalTime(), // Npgsql only accepts UTC offsets for timestamptz
            CreatedAt = clock.GetUtcNow(),
        };

        db.Events.Add(created);
        await db.SaveChangesAsync(ct);

        logger.EventCreated(created.Title, created.Id, created.Venue.Name);
        return created.ToDetails();
    }

    public async Task<Result<EventDetailsResponse>> UpdateAsync(Guid id, UpsertEventRequest request, CancellationToken ct)
    {
        var found = await db.Events.Include(e => e.TicketTypes).FirstOrDefaultAsync(e => e.Id == id, ct);
        if (found is null)
        {
            return EventErrors.NotFound;
        }

        if (found.StartsAt <= clock.GetUtcNow())
        {
            return EventErrors.AlreadyStarted;
        }

        found.Title = request.Title;
        found.Description = request.Description;
        found.Venue = new Venue(request.Venue, request.City);
        found.StartsAt = request.StartsAt.ToUniversalTime();
        await db.SaveChangesAsync(ct);

        return found.ToDetails();
    }

    public async Task<Result> PublishAsync(Guid id, CancellationToken ct)
    {
        var found = await db.Events.Include(e => e.TicketTypes).FirstOrDefaultAsync(e => e.Id == id, ct);
        if (found is null)
        {
            return EventErrors.NotFound;
        }

        if (found.Status != EventStatus.Draft)
        {
            return EventErrors.NotDraft;
        }

        if (found.TicketTypes.Count == 0)
        {
            return EventErrors.NoTicketTypes;
        }

        found.Status = EventStatus.Published;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> CancelAsync(Guid id, CancellationToken ct)
    {
        var found = await db.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (found is null)
        {
            return EventErrors.NotFound;
        }

        if (found.StartsAt <= clock.GetUtcNow())
        {
            return EventErrors.AlreadyStarted;
        }

        found.Status = EventStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<TicketTypeResponse>> AddTicketTypeAsync(Guid eventId, UpsertTicketTypeRequest request, CancellationToken ct)
    {
        if (!await db.Events.AnyAsync(e => e.Id == eventId, ct))
        {
            return EventErrors.NotFound;
        }

        var ticketType = new TicketType
        {
            EventId = eventId,
            Name = request.Name,
            Price = request.Price,
            TotalQuantity = request.TotalQuantity,
            QuantityLeft = request.TotalQuantity,
            MaxPerUser = request.MaxPerUser,
        };

        db.TicketTypes.Add(ticketType);
        await db.SaveChangesAsync(ct);
        return ticketType.ToResponse();
    }

    public async Task<Result<TicketTypeResponse>> UpdateTicketTypeAsync(
        Guid eventId, Guid ticketTypeId, UpsertTicketTypeRequest request, CancellationToken ct)
    {
        var ticketType = await db.TicketTypes.FirstOrDefaultAsync(t => t.Id == ticketTypeId && t.EventId == eventId, ct);
        if (ticketType is null)
        {
            return EventErrors.TicketTypeNotFound;
        }

        var sold = ticketType.TotalQuantity - ticketType.QuantityLeft;
        if (request.TotalQuantity < sold)
        {
            return EventErrors.QuantityBelowSold;
        }

        ticketType.Name = request.Name;
        ticketType.Price = request.Price;
        ticketType.TotalQuantity = request.TotalQuantity;
        ticketType.QuantityLeft = request.TotalQuantity - sold;
        ticketType.MaxPerUser = request.MaxPerUser;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A booking changed QuantityLeft (and xmin) after we read the row. Don't overwrite it.
            return EventErrors.ConcurrentUpdate;
        }

        return ticketType.ToResponse();
    }
}
