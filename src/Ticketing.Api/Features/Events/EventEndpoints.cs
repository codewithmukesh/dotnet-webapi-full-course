using Ticketing.Api.Domain;

namespace Ticketing.Api.Features.Events;

public static class EventEndpoints
{
    public static IServiceCollection AddEventFeature(this IServiceCollection services) =>
        services.AddScoped<EventService>();

    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events").WithTags("Events");

        group.MapGet("/", async (EventService events, CancellationToken ct) =>
            TypedResults.Ok(await events.ListAsync(ct)));

        group.MapGet("/{id:guid}", GetById).WithName(nameof(GetById));

        group.MapPost("/", async (CreateEventRequest request, EventService events, CancellationToken ct) =>
        {
            var created = await events.CreateAsync(request, ct);
            return TypedResults.CreatedAtRoute(created, nameof(GetById), new { id = created.Id });
        });

        group.MapPut("/{id:guid}", async Task<Results<Ok<EventDetailsResponse>, NotFound>> (
            Guid id, UpdateEventRequest request, EventService events, CancellationToken ct) =>
            await events.UpdateAsync(id, request, ct) is { } updated ? TypedResults.Ok(updated) : TypedResults.NotFound());

        group.MapPost("/{id:guid}/publish", async Task<Results<NoContent, NotFound>> (
            Guid id, EventService events, CancellationToken ct) =>
            await events.SetStatusAsync(id, EventStatus.Published, ct) ? TypedResults.NoContent() : TypedResults.NotFound());

        group.MapPost("/{id:guid}/cancel", async Task<Results<NoContent, NotFound>> (
            Guid id, EventService events, CancellationToken ct) =>
            await events.SetStatusAsync(id, EventStatus.Cancelled, ct) ? TypedResults.NoContent() : TypedResults.NotFound());

        group.MapPost("/{id:guid}/ticket-types", async Task<Results<Ok<TicketTypeResponse>, NotFound>> (
            Guid id, UpsertTicketTypeRequest request, EventService events, CancellationToken ct) =>
            await events.AddTicketTypeAsync(id, request, ct) is { } created ? TypedResults.Ok(created) : TypedResults.NotFound());

        return app;
    }

    private static async Task<Results<Ok<EventDetailsResponse>, NotFound>> GetById(
        Guid id, EventService events, CancellationToken ct) =>
        await events.GetAsync(id, ct) is { } found ? TypedResults.Ok(found) : TypedResults.NotFound();
}
