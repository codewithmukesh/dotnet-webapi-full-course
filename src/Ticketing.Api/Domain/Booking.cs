namespace Ticketing.Api.Domain;

public enum BookingStatus
{
    Pending,
    Confirmed,
    Cancelled,
}

public sealed class Booking
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TicketTypeId { get; init; }
    public Guid UserId { get; init; }
    public int Quantity { get; init; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public DateTimeOffset CreatedAt { get; init; }

    public TicketType TicketType { get; init; } = null!;
}
