using Microsoft.EntityFrameworkCore;
using Ticketing.Api.Common;
using Ticketing.Api.Data;
using Ticketing.Api.Data.Configurations;
using Ticketing.Api.Domain;
using Ticketing.Api.Features.Auth;

namespace Ticketing.Api.Features.Bookings;

public sealed class BookingService(TicketingDbContext db, CurrentUser user, TimeProvider clock)
{
    public async Task<Result<BookingResponse>> BookAsync(BookTicketRequest request, CancellationToken ct)
    {
        var userId = user.Id;

        // State checks: things a validator can't know without the database.
        var ticketType = await db.TicketTypes
            .AsNoTracking()
            .Include(t => t.Event)
            .FirstOrDefaultAsync(t => t.Id == request.TicketTypeId, ct);

        if (ticketType is null)
        {
            return BookingErrors.TicketTypeNotFound;
        }

        if (ticketType.Event.Status != EventStatus.Published)
        {
            return BookingErrors.NotOnSale;
        }

        if (ticketType.Event.StartsAt <= clock.GetUtcNow())
        {
            return BookingErrors.EventStarted;
        }

        var alreadyBooked = await db.Bookings
            .Where(b => b.UserId == userId && b.TicketTypeId == request.TicketTypeId && b.Status == BookingStatus.Confirmed)
            .SumAsync(b => b.Quantity, ct);

        if (alreadyBooked + request.Quantity > ticketType.MaxPerUser)
        {
            return BookingErrors.LimitExceeded(ticketType.MaxPerUser);
        }

        // One atomic statement: "take N seats if N seats are left". PostgreSQL locks the row,
        // so concurrent bookings queue up instead of failing. No read-modify-write, no retries.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var reserved = await db.TicketTypes
            .Where(t => t.Id == request.TicketTypeId && t.QuantityLeft >= request.Quantity)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.QuantityLeft, t => t.QuantityLeft - request.Quantity), ct);

        if (reserved == 0)
        {
            return BookingErrors.SoldOut;
        }

        var booking = new Booking
        {
            TicketTypeId = request.TicketTypeId,
            UserId = userId,
            Quantity = request.Quantity,
            CreatedAt = clock.GetUtcNow(),
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return booking.ToResponse(ticketType);
    }

    public async Task<Result> CancelAsync(Guid bookingId, CancellationToken ct)
    {
        var userId = user.Id;
        var booking = await db.Bookings
            .Include(b => b.TicketType).ThenInclude(t => t.Event)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId, ct);

        if (booking is null)
        {
            return BookingErrors.NotFound;
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return BookingErrors.AlreadyCancelled;
        }

        if (booking.TicketType.Event.StartsAt <= clock.GetUtcNow())
        {
            return BookingErrors.EventStarted;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        booking.Status = BookingStatus.Cancelled;
        await db.SaveChangesAsync(ct);

        // Give the seats back atomically, same idea as booking.
        await db.TicketTypes
            .Where(t => t.Id == booking.TicketTypeId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.QuantityLeft, t => t.QuantityLeft + booking.Quantity), ct);

        await transaction.CommitAsync(ct);
        return Result.Success();
    }

    public async Task<List<BookingResponse>> ListMineAsync(CancellationToken ct)
    {
        var userId = user.Id;
        return await db.Bookings
            .IgnoreQueryFilters([EventConfiguration.NotCancelledFilter]) // show bookings for cancelled events too
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BookingResponse(
                b.Id,
                b.TicketTypeId,
                b.TicketType.Name,
                b.TicketType.EventId,
                b.TicketType.Event.Title,
                b.Quantity,
                b.Quantity * b.TicketType.Price,
                b.Status.ToString(),
                b.CreatedAt))
            .ToListAsync(ct);
    }
}
