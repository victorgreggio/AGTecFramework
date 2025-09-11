using AGTec.Common.BackgroundTaskQueue;
using AGTec.Services.ServiceDefaults.Filters;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace AGTec.Services.ServiceDefaults;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.AddBaseServiceDefaults();
        builder.Services.AddHealthChecks().AddSelfCheck();
        return builder;
    }

    public static IHostApplicationBuilder AddServiceDefaults<TContext>(this IHostApplicationBuilder builder) where TContext : DbContext
    {
        builder.AddBaseServiceDefaults();
        builder.Services.AddHealthChecks().AddSelfCheck().AddDbContextCheck<TContext>();
        return builder;
    }

    private static IHostApplicationBuilder AddBaseServiceDefaults(this IHostApplicationBuilder builder)
    {
        // OpenTelemetry
        builder.ConfigureOpenTelemetry();

        // Service Discovery
        builder.Services.AddServiceDiscovery();

        // Http Resilience
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        // Queued Tasks
        builder.Services.AddHostedService<QueuedHostedService>();
        builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

        // Controllers with Action Filters
        builder.Services.AddControllersWithViews(opts =>
        {
            opts.Filters.Add<InvalidModelStateFilter>();
        });

        // OpenAPI
        builder.Services.AddOpenApi();

        return builder;
    }

    private static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            })
            .UseAzureMonitor();

        return builder;
    }

    private static IHealthChecksBuilder AddSelfCheck(this IHealthChecksBuilder builder) => builder.AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
    private static IHealthChecksBuilder AddDbContextCheck<TContext>(this IHealthChecksBuilder builder) where TContext : DbContext
        => builder.AddDbContextCheck<TContext>("Database", HealthStatus.Unhealthy, ["ready"]);
}

