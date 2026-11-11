using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Ticketing.Api.Features.Auth;
using Ticketing.Api.Features.Events;

namespace Ticketing.Api.Tests;

[Collection(SharedApi.Name)]
public sealed class AuthTests(TicketingApiFactory factory)
{
    [Fact]
    public async Task Registering_the_same_email_twice_is_a_conflict()
    {
        var client = factory.CreateClient();
        var email = $"dup-{Guid.NewGuid():N}@test.dev";

        var first = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "correct-horse-battery"), ApiClient.Json, TestContext.Current.CancellationToken);
        var second = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email.ToUpperInvariant(), "correct-horse-battery"), ApiClient.Json, TestContext.Current.CancellationToken);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_organizer_cannot_publish_someone_elses_event()
    {
        var owner = await factory.CreateUserAsync(AccountType.Organizer);
        var intruder = await factory.CreateUserAsync(AccountType.Organizer);
        var created = await owner.PostAsJsonAsync("/api/events",
            new UpsertEventRequest("Mine", null, "Hall", "Pune", DateTimeOffset.UtcNow.AddMonths(1)), ApiClient.Json, TestContext.Current.CancellationToken);
        var eventId = (await created.Content.ReadFromJsonAsync<EventDetailsResponse>(ApiClient.Json, TestContext.Current.CancellationToken))!.Id;

        var response = await intruder.PostAsync($"/api/events/{eventId}/publish", null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_reused_refresh_token_revokes_every_session()
    {
        var client = factory.CreateClient();
        var registered = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"reuse-{Guid.NewGuid():N}@test.dev", "correct-horse-battery"), ApiClient.Json, TestContext.Current.CancellationToken);
        var first = (await registered.Content.ReadFromJsonAsync<AuthResponse>(ApiClient.Json, TestContext.Current.CancellationToken))!;

        var rotated = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(first.RefreshToken), ApiClient.Json, TestContext.Current.CancellationToken);
        var second = (await rotated.Content.ReadFromJsonAsync<AuthResponse>(ApiClient.Json, TestContext.Current.CancellationToken))!;

        var replay = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(first.RefreshToken), ApiClient.Json, TestContext.Current.CancellationToken);
        var afterReplay = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(second.RefreshToken), ApiClient.Json, TestContext.Current.CancellationToken);

        replay.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        afterReplay.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
