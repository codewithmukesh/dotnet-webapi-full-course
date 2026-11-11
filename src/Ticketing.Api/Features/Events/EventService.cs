using Microsoft.EntityFrameworkCore;
using Ticketing.Api.Data;
using Ticketing.Api.Domain;

namespace Ticketing.Api.Features.Events;

public sealed class EventService(TicketingDbContext db, TimeProvider clock, ILogger<EventService> logger)
{
    public async Task<List<EventSummaryResponse>> ListAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var events = await db.Events
            .Where(e => e.StartsAt > now)
            .OrderBy(e => e.StartsAt)
            .ToListAsync(ct);

        var result = new List<EventSummaryResponse>();
        foreach (var e in events)
        {
            // One extra query per event. Looks harmless with 3 events. Chapter 5.
            var ticketTypes = await db.TicketTypes.Where(t => t.EventId == e.Id).ToListAsync(ct);

            result.Add(new EventSummaryResponse(
                e.Id,
                e.Title,
                e.Venue.Name,
                e.Venue.City,
                e.StartsAt,
                e.Status.ToString(),
                ticketTypes.Count == 0 ? null : ticketTypes.Min(t => t.Price),
                ticketTypes.Sum(t => t.QuantityLeft)));
        }

        return result;
    }

    public async Task<EventDetailsResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        var found = await db.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        return found?.ToDetails();
    }

    public async Task<EventDetailsResponse> CreateAsync(CreateEventRequest request, CancellationToken ct)
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

    public async Task<EventDetailsResponse?> UpdateAsync(Guid id, UpdateEventRequest request, CancellationToken ct)
    {
        var found = await db.Events.Include(e => e.TicketTypes).FirstOrDefaultAsync(e => e.Id == id, ct);
        if (found is null)
        {
            return null;
        }

        found.Title = request.Title;
        found.Description = request.Description;
        found.Venue = new Venue(request.Venue, request.City);
        found.StartsAt = request.StartsAt.ToUniversalTime();
        await db.SaveChangesAsync(ct);

        return found.ToDetails();
    }

    public async Task<bool> SetStatusAsync(Guid id, EventStatus status, CancellationToken ct)
    {
        var found = await db.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (found is null)
        {
            return false;
        }

        found.Status = status;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<TicketTypeResponse?> AddTicketTypeAsync(Guid eventId, UpsertTicketTypeRequest request, CancellationToken ct)
    {
        if (!await db.Events.AnyAsync(e => e.Id == eventId, ct))
        {
            return null;
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
}
