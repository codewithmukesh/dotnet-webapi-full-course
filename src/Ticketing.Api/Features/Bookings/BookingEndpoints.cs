using Ticketing.Api.Common;

namespace Ticketing.Api.Features.Bookings;

public static class BookingEndpoints
{
    public static IServiceCollection AddBookingFeature(this IServiceCollection services) =>
        services.AddScoped<BookingService>();

    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        // Any logged-in user can book. Who they are comes from the token, never from the request body.
        var group = app.MapGroup("/api/bookings").WithTags("Bookings").RequireAuthorization();

        group.MapPost("/", async (BookTicketRequest request, BookingService bookings, CancellationToken ct) =>
            (await bookings.BookAsync(request, ct)).ToOk())
        .WithValidation<BookTicketRequest>()
        .RequireRateLimiting(RateLimiting.Bookings);

        group.MapGet("/me", async (BookingService bookings, CancellationToken ct) =>
            TypedResults.Ok(await bookings.ListMineAsync(ct)));

        group.MapPost("/{id:guid}/cancel", async (Guid id, BookingService bookings, CancellationToken ct) =>
            (await bookings.CancelAsync(id, ct)).ToNoContent());

        return app;
    }
}
