using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ticketing.Api.Features.Auth;
using Ticketing.Api.Features.Events;

namespace Ticketing.Api.Tests;

// Small helpers so each test reads like the scenario it checks.
internal static class ApiClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<HttpClient> CreateUserAsync(this TicketingApiFactory factory, AccountType type)
    {
        var client = factory.CreateClient();
        var email = $"{type.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@test.dev";
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "correct-horse-battery", type), Json);
        response.EnsureSuccessStatusCode();

        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    public static async Task<(Guid EventId, Guid TicketTypeId)> CreatePublishedEventAsync(
        this HttpClient organizer, int seats, int maxPerUser = 4)
    {
        var created = await organizer.PostAsJsonAsync("/api/events",
            new UpsertEventRequest("Test Event", null, "Hall", "Chennai", DateTimeOffset.UtcNow.AddMonths(1)), Json);
        created.EnsureSuccessStatusCode();
        var eventId = (await created.Content.ReadFromJsonAsync<EventDetailsResponse>(Json))!.Id;

        var ticketType = await organizer.PostAsJsonAsync($"/api/events/{eventId}/ticket-types",
            new UpsertTicketTypeRequest("General", 499, seats, maxPerUser), Json);
        ticketType.EnsureSuccessStatusCode();
        var ticketTypeId = (await ticketType.Content.ReadFromJsonAsync<TicketTypeResponse>(Json))!.Id;

        (await organizer.PostAsync($"/api/events/{eventId}/publish", null)).EnsureSuccessStatusCode();
        return (eventId, ticketTypeId);
    }
}
