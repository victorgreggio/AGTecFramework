using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

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
            .UseSerilog((context, config) =>
            {
                config.MinimumLevel
                    .Debug()
                    .Enrich
                    .FromLogContext()
                    .WriteTo
                    .Console();
            })
            .UseConsoleLifetime();
    }
}
