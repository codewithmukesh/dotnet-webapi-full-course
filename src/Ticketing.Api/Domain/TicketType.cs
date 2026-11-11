namespace Ticketing.Api.Domain;

public sealed class TicketType
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid EventId { get; init; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public int TotalQuantity { get; set; }
    public int QuantityLeft { get; set; }
    public int MaxPerUser { get; set; }

    // Mapped to PostgreSQL's xmin system column: the concurrency token.
    public uint Version { get; init; }

    public Event Event { get; init; } = null!;
}
