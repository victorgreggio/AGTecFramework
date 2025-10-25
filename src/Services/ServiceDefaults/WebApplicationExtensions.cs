using AGTec.Services.ServiceDefaults.Database;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        // CORS
        app.UseCors();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // Swagger (only in Development)
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                var descriptions = app.DescribeApiVersions();
                foreach (var description in descriptions)
                {
                    var url = $"/swagger/{description.GroupName}/swagger.json";
                    var name = description.GroupName.ToUpperInvariant();
                    options.SwaggerEndpoint(url, name);
                }
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health").AllowAnonymous();

        app.MapOpenApi();
       
        return app;
    }
}

