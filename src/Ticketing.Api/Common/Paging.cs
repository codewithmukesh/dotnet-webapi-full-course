using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace Ticketing.Api.Common;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, string? NextCursor);

// Keyset cursor: "the last row I saw". Opaque to clients, so we can change it later.
public readonly record struct EventCursor(DateTimeOffset StartsAt, Guid Id)
{
    public string Encode() =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes(
            string.Create(CultureInfo.InvariantCulture, $"{StartsAt.UtcTicks}:{Id}")));

    public static bool TryDecode(string? value, out EventCursor cursor)
    {
        cursor = default;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        try
        {
            var parts = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(value)).Split(':');
            if (parts.Length == 2
                && long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
                && Guid.TryParse(parts[1], out var id))
            {
                cursor = new EventCursor(new DateTimeOffset(ticks, TimeSpan.Zero), id);
                return true;
            }
        }
        catch (FormatException)
        {
            // Not valid Base64Url: treat as an invalid cursor.
        }

        return false;
    }
}
