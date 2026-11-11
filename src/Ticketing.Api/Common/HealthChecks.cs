using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Cannot connect to PostgreSQL.");
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
