using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Ticketing.Api.Data;

namespace Ticketing.Api.Tests;

// One real PostgreSQL + one real Redis for the whole test run. Same versions as compose.yaml.
public sealed class TicketingApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:8-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        // Same migrations as production. No EnsureCreated, no InMemory provider.
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TicketingDbContext>().Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", "test-only-signing-key-0123456789-0123456789");
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning"); // quiet test output
        builder.UseSetting("RateLimits:AuthPerIp", "10000"); // every test user signs up from the same in-memory "IP"
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }
}

// Every test class in this collection shares the one factory (and the containers).
[CollectionDefinition(Name)]
public sealed class SharedApi : ICollectionFixture<TicketingApiFactory>
{
    public const string Name = "api";
}
