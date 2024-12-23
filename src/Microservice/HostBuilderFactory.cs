using System.IO;
using AGTec.Common.BackgroundTaskQueue;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AGTec.Microservice;

public static class HostBuilderFactory
{
    public static IHostBuilder CreateHostBuilder<TStartup>(string[] args) where TStartup : class
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(configHost =>
            {
                configHost.SetBasePath(Directory.GetCurrentDirectory());
                configHost.AddJsonFile("hostsettings.json", true)
                    .AddEnvironmentVariables("AGTEC_");
                configHost.AddCommandLine(args);
            })
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", true, true)
                    .AddJsonFile(
                        $"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json",
                        true)
                    .AddEnvironmentVariables("AGTEC_");
            })
            .UseSerilog((context, config) =>
            {
                config.MinimumLevel
                    .Debug()
                    .Enrich
                    .FromLogContext()
                    .WriteTo
                    .Console();
            })
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<TStartup>(); })
            .ConfigureServices(services => { services.AddHostedService<QueuedHostedService>(); });
    }
}
