using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Ticketing.Api.Common;

// Limits live in configuration: production stays strict, the test suite can raise them.
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimits";

    [Range(1, 1000)]
    public int BookingsPerUser { get; init; } = 5;

    [Range(1, 3600)]
    public int BookingsPeriodSeconds { get; init; } = 10;

    [Range(1, 100_000)]
    public int AuthPerIp { get; init; } = 10;

    [Range(1, 3600)]
    public int AuthWindowSeconds { get; init; } = 60;
}

public static class RateLimiting
{
    public const string Bookings = "bookings";
    public const string Auth = "auth";

    public static IServiceCollection AddTicketingRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitOptions>()
            .BindConfiguration(RateLimitOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services.AddRateLimiter(options =>
        {
            // Ticket bots: each USER gets a bucket of booking attempts (default 5, refilled every 10 s).
            // Partition by user id, not IP: a whole office shares one IP, a bot can rotate IPs.
            options.AddPolicy(Bookings, context =>
            {
                var limits = Limits(context);
                return RateLimitPartition.GetTokenBucketLimiter(
                    context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? ClientIp(context),
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = limits.BookingsPerUser,
                        TokensPerPeriod = limits.BookingsPerUser,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(limits.BookingsPeriodSeconds),
                        QueueLimit = 0,
                    });
            });

            // Password guessing and sign-up spam: per IP, because nobody is logged in yet (default 10/minute).
            options.AddPolicy(Auth, context =>
            {
                var limits = Limits(context);
                return RateLimitPartition.GetFixedWindowLimiter(
                    ClientIp(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.AuthPerIp,
                        Window = TimeSpan.FromSeconds(limits.AuthWindowSeconds),
                        QueueLimit = 0,
                    });
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                var problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests. Slow down and try again shortly.",
                    },
                });
            };
        });
    }

    private static RateLimitOptions Limits(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;

    // The real client IP once UseForwardedHeaders has run (see ProxySupport.cs).
    private static string ClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
