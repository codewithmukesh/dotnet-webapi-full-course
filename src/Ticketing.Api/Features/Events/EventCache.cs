using System.Globalization;

namespace Ticketing.Api.Features.Events;

internal static class EventCache
{
    public const string ListTag = "events:list";

    public static string EventTag(Guid id) => $"event:{id}";

    public static string DetailsKey(Guid id) => $"events:{id}:details";

    // Every filter is part of the key, or two different searches would share one cached page.
    public static string ListKey(EventQuery q) => string.Create(
        CultureInfo.InvariantCulture,
        $"events:list:{q.City}|{q.Search}|{q.From:O}|{q.To:O}|{q.Cursor}|{q.PageSize}");
}
