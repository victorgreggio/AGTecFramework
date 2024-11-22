using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace AGTec.Common.Monitor;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseAGTecMonitor(this IApplicationBuilder app, IHostEnvironment hostEnv)
    {
        if (hostEnv.IsDevelopment()) app.UseMiniProfiler();

        return app;
    }
}