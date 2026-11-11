namespace Ticketing.Api.Features.Events;

public static class EventEndpoints
{
    // Temporary in-memory store. Chapter 4 replaces it with EF Core + PostgreSQL.
    private static readonly List<EventResponse> Events = [];

    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events").WithTags("Events");

        group.MapGet("/", () => TypedResults.Ok(Events));

        group.MapGet("/{id:guid}", GetById).WithName(nameof(GetById));

        group.MapPost("/", (CreateEventRequest request) =>
        {
            var created = new EventResponse(Guid.CreateVersion7(), request.Title, request.Venue, request.StartsAt);
            Events.Add(created);
            return TypedResults.CreatedAtRoute(created, nameof(GetById), new { id = created.Id });
        });

        return app;
    }

    private static Results<Ok<EventResponse>, NotFound> GetById(Guid id)
    {
        var found = Events.FirstOrDefault(e => e.Id == id);
        return found is null ? TypedResults.NotFound() : TypedResults.Ok(found);
    }
}
