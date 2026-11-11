namespace Ticketing.Api.Features.Bookings;

public static class BookingEndpoints
{
    public static IServiceCollection AddBookingFeature(this IServiceCollection services) =>
        services.AddScoped<BookingService>();

    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings").WithTags("Bookings");

        group.MapPost("/", async Task<Results<Ok<BookingResponse>, NotFound, Conflict<string>>> (
            BookTicketRequest request, BookingService bookings, CancellationToken ct) =>
        {
            var (outcome, booking) = await bookings.BookAsync(request, ct);
            return outcome switch
            {
                BookingOutcome.Booked => TypedResults.Ok(booking!),
                BookingOutcome.NotFound => TypedResults.NotFound(),
                _ => TypedResults.Conflict("Sold out."),
            };
        });

        return app;
    }
}
