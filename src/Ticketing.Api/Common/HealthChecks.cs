using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using Ticketing.Api.Data;

namespace Ticketing.Api.Common;

public static class HealthChecks
{
    private const string Ready = "ready";

    public static IServiceCollection AddTicketingHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("postgres", tags: [Ready])
            .AddCheck<RedisHealthCheck>("redis", tags: [Ready]);
        return services;
    }

    public static IEndpointRouteBuilder MapTicketingHealthChecks(this IEndpointRouteBuilder app)
    {
        // Liveness: "is the process alive?" No dependencies. If this fails, restart the container.
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

        // Readiness: "can it serve traffic?" Checks Postgres + Redis. If this fails, stop sending requests,
        // but DON'T restart: restarting the API doesn't fix a database that is down.
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(Ready) });

        return app;
    }
}

internal sealed class DatabaseHealthCheck(TicketingDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!await db.Database.CanConnectAsync(cancellationToken))
        {
            return HealthCheckResult.Unhealthy("Cannot connect to PostgreSQL.");
        }

        // Connected is not enough: on a fresh database every query fails until the migration script runs.
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).Count();
        return pending == 0
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy($"{pending} pending migration(s). Apply the migration script.");
    }
}

internal sealed class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Ping {latency.TotalMilliseconds:F1} ms");
        }
        catch (RedisException ex)
        {
            return HealthCheckResult.Unhealthy("Cannot reach Redis.", ex);
        }
    }
}
