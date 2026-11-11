using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Ticketing.Api.Common;

public static class ProxySupport
{
    // Behind a reverse proxy (Traefik, Caddy, nginx, a cloud load balancer), every request arrives FROM the proxy.
    // The real client IP is in the X-Forwarded-For header. We only believe that header when the request
    // comes from a network we trust, otherwise anyone can send "X-Forwarded-For: 1.2.3.4" and pick their own IP.
    public static IServiceCollection AddTicketingForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Default: only loopback is trusted. Add the proxy's network, e.g. "Proxy:TrustedNetworks:0" = "172.18.0.0/16".
            foreach (var cidr in configuration.GetSection("Proxy:TrustedNetworks").Get<string[]>() ?? [])
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(cidr));
            }
        });
        return services;
    }
}
