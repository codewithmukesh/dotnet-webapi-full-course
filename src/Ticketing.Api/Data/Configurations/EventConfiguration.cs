using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Api.Domain;

namespace Ticketing.Api.Data.Configurations;

internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public const string NotCancelledFilter = "NotCancelled";

    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.Property(e => e.Title).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        builder.ComplexProperty(e => e.Venue, venue =>
        {
            venue.Property(v => v.Name).HasMaxLength(200);
            venue.Property(v => v.City).HasMaxLength(100);
        });

        // The organizer who owns the event. Restrict: deleting a user must not delete their events.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Keyset pagination (chapter 7) sorts by StartsAt, then Id.
        builder.HasIndex(e => new { e.StartsAt, e.Id });

        // Named filter: can be switched off on its own with IgnoreQueryFilters([NotCancelledFilter]).
        builder.HasQueryFilter(NotCancelledFilter, e => e.Status != EventStatus.Cancelled);
    }
}
