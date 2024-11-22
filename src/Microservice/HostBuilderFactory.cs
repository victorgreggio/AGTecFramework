using AGTec.Common.BackgroundTaskQueue;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Elasticsearch;

namespace AGTec.Microservice;

public static class HostBuilderFactory
{
    public static IHostBuilder CreateHostBuilder<TStartup>(string[] args) where TStartup : class
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", true, true)
                    .AddJsonFile("k8s/appsettings.k8s.json", true);
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
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<TStartup>(); })
            .ConfigureServices(services => { services.AddHostedService<QueuedHostedService>(); });
    }
}