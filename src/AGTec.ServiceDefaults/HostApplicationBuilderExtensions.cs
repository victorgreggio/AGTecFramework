using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace AGTec.ServiceDefaults;

/// <summary>
/// Composition entry points shared by every AGTecFramework host (API, Worker, gRPC).
/// A single <see cref="AddAGTecServiceDefaults"/> call wires the cross-cutting concerns
/// that every service must have: telemetry, health, service discovery, and resilient
/// outbound HTTP. Services add their own domain wiring on top.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds OpenTelemetry, default health checks, service discovery, and standard HTTP
    /// resilience to any host builder (works for <c>WebApplication</c>, <c>Host</c>, and
    /// Aspire hosts alike).
    /// </summary>
    public static TBuilder AddAGTecServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Standard Polly resilience (timeout, retry, circuit breaker, hedging) on every outbound HttpClient.
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// Registers the liveness/readiness split that maps onto Kubernetes probes:
    /// a "self" liveness check, tagged <c>live</c>; readiness checks (DB, broker, …)
    /// are added by the service and surface on <c>/health</c>.
    /// </summary>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder
            .Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        return builder;
    }
}
