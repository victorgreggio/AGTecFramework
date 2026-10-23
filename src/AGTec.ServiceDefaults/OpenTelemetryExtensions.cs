using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace AGTec.ServiceDefaults;

/// <summary>
/// OpenTelemetry wiring: one instrumentation pipeline (traces, metrics, logs) with exporters chosen by
/// configuration — OTLP when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set, Azure Monitor when
/// <c>APPLICATIONINSIGHTS_CONNECTION_STRING</c> is set; both may run, and neither keeps telemetry in-process.
/// </summary>
public static class OpenTelemetryExtensions
{
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var azureMonitor = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;

            if (!string.IsNullOrWhiteSpace(azureMonitor))
            {
                logging.AddAzureMonitorLogExporter(options => options.ConnectionString = azureMonitor);
            }
        });

        builder
            .Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(builder.Environment.ApplicationName)
                    // String-named sources, so no package reference. Wolverine propagates trace context
                    // across the outbox, linking a publisher's request to the consuming handler.
                    .AddSource("Wolverine")
                    .AddSource("Marten")
                    .AddSource("Npgsql")
                    .AddSource("AGTec.Ai")
                    .AddAspNetCoreInstrumentation()
                    // gRPC calls ride on HttpClient instrumentation in .NET.
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters(azureMonitor);

        return builder;
    }

    private static void AddOpenTelemetryExporters<TBuilder>(this TBuilder builder, string? azureMonitorConnectionString)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlp = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlp)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        if (!string.IsNullOrWhiteSpace(azureMonitorConnectionString))
        {
            // Add only exporters to the existing instrumentation, unlike UseAzureMonitor() which re-registers it.
            builder
                .Services.AddOpenTelemetry()
                .WithTracing(tracing =>
                    tracing.AddAzureMonitorTraceExporter(options =>
                        options.ConnectionString = azureMonitorConnectionString
                    )
                )
                .WithMetrics(metrics =>
                    metrics.AddAzureMonitorMetricExporter(options =>
                        options.ConnectionString = azureMonitorConnectionString
                    )
                );
        }
    }
}
