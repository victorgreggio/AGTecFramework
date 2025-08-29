using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using System.IO;

namespace AGTec.Worker;

public static class HostBuilderFactory
{
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(configHost =>
            {
                configHost.SetBasePath(Directory.GetCurrentDirectory());
                configHost.AddJsonFile("hostsettings.json", true)
                    .AddEnvironmentVariables("AGTEC_");
                configHost.AddCommandLine(args);
            })
            .ConfigureAppConfiguration((hostContext, configApp) =>
            {
                configApp.AddJsonFile("appsettings.json", true)
                    .AddJsonFile(
                    $"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                    true)
                    .AddEnvironmentVariables("AGTEC_");
            })
            .UseSerilog((context, config) => config.MinimumLevel.Debug().Enrich.FromLogContext().WriteTo.Console())
            .ConfigureServices((hostContext, services) =>
            {
                // Service Discovery
                services.AddServiceDiscovery();

                // Http Resilience
                services.ConfigureHttpClientDefaults(http =>
                {
                    http.AddStandardResilienceHandler();
                    http.AddServiceDiscovery();
                });

                // OpenTelemetry
                services.AddOpenTelemetry()
                    .WithLogging(logging => logging.AddAzureMonitorLogExporter())
                    .WithMetrics(metrics => metrics.AddHttpClientInstrumentation().AddRuntimeInstrumentation())
                    .WithTracing(tracing => tracing.AddHttpClientInstrumentation())
                    .UseAzureMonitorExporter();

            })
            .UseConsoleLifetime();
    }
}
