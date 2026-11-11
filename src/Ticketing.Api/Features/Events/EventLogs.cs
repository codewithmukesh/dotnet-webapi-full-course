namespace Ticketing.Api.Features.Events;

internal static partial class EventLogs
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Created event {Title} ({Id}) at {Venue}")]
    public static partial void EventCreated(this ILogger logger, string title, Guid id, string venue);
}
