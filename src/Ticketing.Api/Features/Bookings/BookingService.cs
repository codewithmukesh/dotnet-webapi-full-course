using Microsoft.EntityFrameworkCore;
using Ticketing.Api.Data;
using Ticketing.Api.Domain;

namespace Ticketing.Api.Features.Bookings;

public enum BookingOutcome
{
    Booked,
    NotFound,
    SoldOut,
}

public sealed class BookingService(TicketingDbContext db, TimeProvider clock)
{
    // One atomic statement: "take N seats if N seats are left". PostgreSQL locks the row,
    // so concurrent bookings queue up instead of failing. No read-modify-write, no retries.
    public async Task<(BookingOutcome Outcome, BookingResponse? Booking)> BookAsync(BookTicketRequest request, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var reserved = await db.TicketTypes
            .Where(t => t.Id == request.TicketTypeId && t.QuantityLeft >= request.Quantity)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.QuantityLeft, t => t.QuantityLeft - request.Quantity), ct);

        if (reserved == 0)
        {
            var exists = await db.TicketTypes.AnyAsync(t => t.Id == request.TicketTypeId, ct);
            return (exists ? BookingOutcome.SoldOut : BookingOutcome.NotFound, null);
        }

        var booking = new Booking
        {
            TicketTypeId = request.TicketTypeId,
            UserId = request.UserId,
            Quantity = request.Quantity,
            CreatedAt = clock.GetUtcNow(),
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var ticketType = await db.TicketTypes
            .AsNoTracking()
            .Include(t => t.Event)
            .FirstAsync(t => t.Id == request.TicketTypeId, ct);

        return (BookingOutcome.Booked, booking.ToResponse(ticketType));
    }
}
