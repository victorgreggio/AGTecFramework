using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Tenancy;

public static class AGTecTenancyExtensions
{
    /// <summary>Registers the request-scoped <see cref="ITenantContext"/>.</summary>
    public static IServiceCollection AddAGTecTenancy(
        this IServiceCollection services,
        Action<AGTecTenancyOptions>? configure = null
    )
    {
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<TenantContext>());

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }

    /// <summary>
    /// Resolves the tenant for each request. Place after <c>UseRouting</c> (so route values exist)
    /// and after <c>UseAuthentication</c> (so the claim check can run).
    /// </summary>
    public static IApplicationBuilder UseAGTecTenancy(this IApplicationBuilder app) =>
        app.UseMiddleware<TenantResolutionMiddleware>();
}
