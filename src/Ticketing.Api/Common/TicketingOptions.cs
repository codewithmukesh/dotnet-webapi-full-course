using System.ComponentModel.DataAnnotations;

namespace Ticketing.Api.Common;

public sealed class TicketingOptions
{
    public const string SectionName = "Ticketing";

    [Range(1, 20)]
    public int MaxTicketsPerBooking { get; init; }

    [Range(1, 100)]
    public int MaxPageSize { get; init; }
}
