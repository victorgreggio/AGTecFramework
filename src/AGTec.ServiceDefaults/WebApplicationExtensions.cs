using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AGTec.ServiceDefaults;

/// <summary>
/// Endpoint wiring for web hosts. Exposes the two probes Kubernetes expects, kept
/// separate so a pod is only declared "ready" once its dependencies are reachable.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Maps:
    /// <list type="bullet">
    ///   <item><c>/alive</c> — liveness: only the "self" check (process is up).</item>
    ///   <item><c>/health</c> — readiness: every registered check (DB, broker, …).</item>
    /// </list>
    /// Wire these to the pod's <c>livenessProbe</c> and <c>readinessProbe</c> respectively.
    /// </summary>
    public static WebApplication MapAGTecDefaultEndpoints(this WebApplication app)
    {
        // Liveness — restart the pod if this fails.
        app.MapHealthChecks(
            "/alive",
            new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("live") }
        );

        // Readiness — stop routing traffic to the pod if this fails.
        app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });

        return app;
    }
}
