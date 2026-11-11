using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Api.Domain;

namespace Ticketing.Api.Data.Configurations;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);

        // Never let a delete wipe out sold tickets. Cancel instead of delete.
        builder.HasOne(b => b.TicketType)
            .WithMany()
            .HasForeignKey(b => b.TicketTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(EventConfiguration.NotCancelledFilter, b => b.TicketType.Event.Status != EventStatus.Cancelled);

        builder.HasIndex(b => new { b.UserId, b.TicketTypeId });
    }
}
