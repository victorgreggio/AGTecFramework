using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Elasticsearch;

namespace AGTec.Worker;

public static class HostBuilderFactory
{
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(configHost =>
            {
                configHost.SetBasePath(Directory.GetCurrentDirectory());
                configHost.AddJsonFile("hostsettings.json", true);
                configHost.AddEnvironmentVariables("PREFIX_");
                configHost.AddCommandLine(args);
            })
            .ConfigureAppConfiguration((hostContext, configApp) =>
            {
                configApp.AddJsonFile("appsettings.json", true);
                configApp.AddJsonFile(
                    $"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                    true);
                configApp.AddJsonFile("k8s/appsettings.k8s.json", true);
                configApp.AddEnvironmentVariables("PREFIX_");
                configApp.AddCommandLine(args);
            })
            .UseSerilog((context, config) =>
            {
                if (context.HostingEnvironment.IsDevelopment())
                    config.MinimumLevel
                        .Debug()
                        .Enrich
                        .FromLogContext()
                        .WriteTo
                        .File("log.txt", rollingInterval: RollingInterval.Day);
                else
                    config.MinimumLevel
                        .Information()
                        .Enrich
                        .FromLogContext()
                        .WriteTo
                        .File(new ElasticsearchJsonFormatter(), "log.txt", rollingInterval: RollingInterval.Day);
            })
            .UseConsoleLifetime();
    }
}