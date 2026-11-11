using FluentValidation;
using Microsoft.Extensions.Options;
using Ticketing.Api.Common;

namespace Ticketing.Api.Features.Events;

// Validators check the SHAPE of a request. Whether the event exists or is sold out is the service's job.
public sealed class UpsertEventRequestValidator : AbstractValidator<UpsertEventRequest>
{
    public UpsertEventRequestValidator(TimeProvider clock)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Venue).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StartsAt)
            .Must(startsAt => startsAt > clock.GetUtcNow())
            .WithMessage("The event must start in the future.");
    }
}

public sealed class UpsertTicketTypeRequestValidator : AbstractValidator<UpsertTicketTypeRequest>
{
    public UpsertTicketTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(x => x.TotalQuantity).InclusiveBetween(1, 100_000);
        RuleFor(x => x.MaxPerUser)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(x => x.TotalQuantity)
            .WithMessage("'Max Per User' can't be more than the total quantity.");
    }
}

public sealed class EventQueryValidator : AbstractValidator<EventQuery>
{
    public EventQueryValidator(IOptions<TicketingOptions> options)
    {
        // Without a cap, ?pageSize=1000000 is a denial of service you wrote yourself.
        RuleFor(x => x.PageSize).InclusiveBetween(1, options.Value.MaxPageSize);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.To).GreaterThan(x => x.From).When(x => x.From is not null && x.To is not null);
        RuleFor(x => x.Cursor)
            .Must(cursor => EventCursor.TryDecode(cursor, out _))
            .When(x => x.Cursor is not null)
            .WithMessage("The cursor is invalid. Use the nextCursor value from the previous page.");
    }
}
