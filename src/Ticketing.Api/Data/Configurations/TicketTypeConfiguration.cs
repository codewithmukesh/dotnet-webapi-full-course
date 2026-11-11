using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Api.Domain;

namespace Ticketing.Api.Data.Configurations;

internal sealed class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
{
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
        builder.ToTable("ticket_types");

        builder.Property(t => t.Name).HasMaxLength(100);
        builder.Property(t => t.Price).HasPrecision(18, 2);

        // uint + IsRowVersion() = PostgreSQL xmin. No extra column, Postgres bumps it on every update.
        builder.Property(t => t.Version).IsRowVersion();

        builder.HasOne(t => t.Event)
            .WithMany(e => e.TicketTypes)
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Same named filter as Event. A filter on the principal alone turns joins into silent row loss.
        builder.HasQueryFilter(EventConfiguration.NotCancelledFilter, t => t.Event.Status != EventStatus.Cancelled);
    }
}
