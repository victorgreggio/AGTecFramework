using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;

namespace AGTec.Microservice;

public static class HostBuilderFactory
{
    public static IHostBuilder CreateHostBuilder<TStartup>(string[] args) where TStartup : class =>
        Host.CreateDefaultBuilder(args)
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
            .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true)
            .AddEnvironmentVariables("AGTEC_");
        })
        .UseSerilog((context, config) => config.MinimumLevel.Debug().Enrich.FromLogContext().WriteTo.Console())
        .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<TStartup>(); });
}
