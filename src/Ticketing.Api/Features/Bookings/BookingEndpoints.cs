using Ticketing.Api.Common;

namespace Ticketing.Api.Features.Bookings;

public static class BookingEndpoints
{
    public static IServiceCollection AddBookingFeature(this IServiceCollection services) =>
        services.AddScoped<BookingService>();

    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings").WithTags("Bookings");

        group.MapPost("/", async (BookTicketRequest request, BookingService bookings, CancellationToken ct) =>
            (await bookings.BookAsync(request, ct)).ToOk())
        .WithValidation<BookTicketRequest>();

        // userId as a query parameter is temporary: chapter 8 takes it from the access token.
        group.MapGet("/", async (Guid userId, BookingService bookings, CancellationToken ct) =>
            TypedResults.Ok(await bookings.ListForUserAsync(userId, ct)));

        group.MapPost("/{id:guid}/cancel", async (Guid id, Guid userId, BookingService bookings, CancellationToken ct) =>
            (await bookings.CancelAsync(id, userId, ct)).ToNoContent());

        return app;
    }
}
