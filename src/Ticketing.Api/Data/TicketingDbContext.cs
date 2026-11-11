using Microsoft.EntityFrameworkCore;
using Ticketing.Api.Domain;

namespace Ticketing.Api.Data;

public sealed class TicketingDbContext(DbContextOptions<TicketingDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicketingDbContext).Assembly);
}
