namespace Ticketing.Api.Features.Events;

public static class EventEndpoints
{
    public static IServiceCollection AddEventFeature(this IServiceCollection services)
    {
        services.AddSingleton<IEventStore, InMemoryEventStore>();
        services.AddScoped<EventService>();
        return services;
    }

    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events").WithTags("Events");

        group.MapGet("/", (EventService events) => TypedResults.Ok(events.List()));

        group.MapGet("/{id:guid}", GetById).WithName(nameof(GetById));

        group.MapPost("/", (CreateEventRequest request, EventService events) =>
        {
            var created = events.Create(request);
            return TypedResults.CreatedAtRoute(created, nameof(GetById), new { id = created.Id });
        });

        return app;
    }

    private static Results<Ok<EventResponse>, NotFound> GetById(Guid id, EventService events) =>
        events.Get(id) is { } found ? TypedResults.Ok(found) : TypedResults.NotFound();
}
