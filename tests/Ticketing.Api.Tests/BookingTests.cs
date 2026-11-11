using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Ticketing.Api.Features.Auth;
using Ticketing.Api.Features.Bookings;
using Ticketing.Api.Features.Events;

namespace Ticketing.Api.Tests;

[Collection(SharedApi.Name)]
public sealed class BookingTests(TicketingApiFactory factory)
{
    [Fact]
    public async Task Fifty_parallel_bookings_for_ten_seats_sell_exactly_ten()
    {
        var organizer = await factory.CreateUserAsync(AccountType.Organizer);
        var (eventId, ticketTypeId) = await organizer.CreatePublishedEventAsync(seats: 10);

        var attendees = await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => factory.CreateUserAsync(AccountType.Attendee)));

        // All 50 fire at once, like a ticket drop.
        var responses = await Task.WhenAll(attendees.Select(client =>
            client.PostAsJsonAsync("/api/bookings", new BookTicketRequest(ticketTypeId, 1), ApiClient.Json, TestContext.Current.CancellationToken)));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(10);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(40);

        var details = await organizer.GetFromJsonAsync<EventDetailsResponse>($"/api/events/{eventId}", ApiClient.Json, TestContext.Current.CancellationToken);
        details!.TicketTypes.Single().QuantityLeft.ShouldBe(0);
    }

    [Fact]
    public async Task Booking_more_than_the_per_user_limit_is_rejected()
    {
        var organizer = await factory.CreateUserAsync(AccountType.Organizer);
        var (_, ticketTypeId) = await organizer.CreatePublishedEventAsync(seats: 100, maxPerUser: 2);
        var attendee = await factory.CreateUserAsync(AccountType.Attendee);

        var first = await attendee.PostAsJsonAsync("/api/bookings", new BookTicketRequest(ticketTypeId, 2), ApiClient.Json, TestContext.Current.CancellationToken);
        var second = await attendee.PostAsJsonAsync("/api/bookings", new BookTicketRequest(ticketTypeId, 1), ApiClient.Json, TestContext.Current.CancellationToken);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("bookings.limit_exceeded");
    }

    [Fact]
    public async Task Cancelling_a_booking_returns_the_seats()
    {
        var organizer = await factory.CreateUserAsync(AccountType.Organizer);
        var (eventId, ticketTypeId) = await organizer.CreatePublishedEventAsync(seats: 5);
        var attendee = await factory.CreateUserAsync(AccountType.Attendee);

        var booked = await attendee.PostAsJsonAsync("/api/bookings", new BookTicketRequest(ticketTypeId, 3), ApiClient.Json, TestContext.Current.CancellationToken);
        var booking = await booked.Content.ReadFromJsonAsync<BookingResponse>(ApiClient.Json, TestContext.Current.CancellationToken);

        var cancelled = await attendee.PostAsync($"/api/bookings/{booking!.Id}/cancel", null, TestContext.Current.CancellationToken);

        cancelled.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var details = await organizer.GetFromJsonAsync<EventDetailsResponse>($"/api/events/{eventId}", ApiClient.Json, TestContext.Current.CancellationToken);
        details!.TicketTypes.Single().QuantityLeft.ShouldBe(5);
    }
}
