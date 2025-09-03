using AGTec.Common.Monitor;
using AGTec.Microservice.Database;
using Correlate.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AGTec.Microservice;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseAGTecMicroservice(this IApplicationBuilder app, IHostEnvironment hostEnv)
    {
        return app.UseCommonAGTecMicroservice(hostEnv);
    }

    // With SQL Server + Migrations
    public static IApplicationBuilder UseAGTecMicroservice<TContext>(this IApplicationBuilder app,
        IHostEnvironment hostEnv) where TContext : DbContext
    {
        // Run Migrations
        using (var scope = app.ApplicationServices.CreateScope())
        {
            if (scope.ServiceProvider.GetService<TContext>() is DbContext dbContext)
                DbInitializer.Initialize(dbContext);
        }

        return app.UseCommonAGTecMicroservice(hostEnv);
    }

    private static IApplicationBuilder UseCommonAGTecMicroservice(this IApplicationBuilder app,
        IHostEnvironment hostEnv)
    {
        app.UseSerilogRequestLogging();

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();

        app.UseHealthChecks("/health");

        app.UseAGTecMonitor(hostEnv);

        app.UseCorrelate();

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseCors("CorsPolicy");

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapOpenApi();
            endpoints.MapControllers().RequireAuthorization();
        });

        return app;
    }
}