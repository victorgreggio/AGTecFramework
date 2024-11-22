using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Profiling.SqlFormatters;

namespace AGTec.Common.Monitor;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAGTecMonitor(this IServiceCollection services,
        IHostEnvironment hostEnv)
    {
        if (hostEnv.IsDevelopment())
            services.AddMiniProfiler(options =>
            {
                options.RouteBasePath = "/profiler";
                options.SqlFormatter = new InlineFormatter();
            }).AddEntityFramework();

        return services;
    }
}