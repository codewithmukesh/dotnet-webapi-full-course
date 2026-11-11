using Ticketing.Api.Common;

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

        group.MapPost("/", async (UpsertEventRequest request, EventService events, CancellationToken ct) =>
        {
            var created = await events.CreateAsync(request, ct);
            return TypedResults.CreatedAtRoute(created, nameof(GetById), new { id = created.Id });
        })
        .WithValidation<UpsertEventRequest>();

        group.MapPut("/{id:guid}", async (Guid id, UpsertEventRequest request, EventService events, CancellationToken ct) =>
            (await events.UpdateAsync(id, request, ct)).ToOk())
        .WithValidation<UpsertEventRequest>();

        group.MapPost("/{id:guid}/publish", async (Guid id, EventService events, CancellationToken ct) =>
            (await events.PublishAsync(id, ct)).ToNoContent());

        group.MapPost("/{id:guid}/cancel", async (Guid id, EventService events, CancellationToken ct) =>
            (await events.CancelAsync(id, ct)).ToNoContent());

        group.MapPost("/{id:guid}/ticket-types", async (
            Guid id, UpsertTicketTypeRequest request, EventService events, CancellationToken ct) =>
            (await events.AddTicketTypeAsync(id, request, ct)).ToOk())
        .WithValidation<UpsertTicketTypeRequest>();

        group.MapPut("/{id:guid}/ticket-types/{ticketTypeId:guid}", async (
            Guid id, Guid ticketTypeId, UpsertTicketTypeRequest request, EventService events, CancellationToken ct) =>
            (await events.UpdateTicketTypeAsync(id, ticketTypeId, request, ct)).ToOk())
        .WithValidation<UpsertTicketTypeRequest>();

        return app;
    }

    private static async Task<Results<Ok<EventDetailsResponse>, ProblemHttpResult>> GetById(
        Guid id, EventService events, CancellationToken ct) =>
        (await events.GetAsync(id, ct)).ToOk();
}
