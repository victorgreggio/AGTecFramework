using AGTec.Services.ServiceDefaults.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Services.ServiceDefaults;

public static class WebApplicationExtensions
{
    public static WebApplication UseServiceDefaults(this WebApplication app) => app.UseBaseDefaultServices();
    public static WebApplication UseServiceDefaults<TContext>(this WebApplication app) where TContext : DbContext
    {
        app.UseBaseDefaultServices();

        // Run Migrations
        using (var scope = app.Services.CreateScope())
        {
            if (scope.ServiceProvider.GetService<TContext>() is DbContext dbContext)
                DbInitializer.Initialize(dbContext);
        }
        return app;
    }

    private static WebApplication UseBaseDefaultServices(this WebApplication app)
    {

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();

        app.MapHealthChecks("/health");

        app.MapOpenApi();
       
        return app;
    }
}

