using FluentValidation;
using Microsoft.Extensions.Options;
using Ticketing.Api.Common;

namespace Ticketing.Api.Features.Bookings;

public sealed class BookTicketRequestValidator : AbstractValidator<BookTicketRequest>
{
    public BookTicketRequestValidator(IOptions<TicketingOptions> options)
    {
        RuleFor(x => x.TicketTypeId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, options.Value.MaxTicketsPerBooking);
    }
}
