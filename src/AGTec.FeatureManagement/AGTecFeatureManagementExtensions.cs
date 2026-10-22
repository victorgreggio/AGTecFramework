using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace AGTec.FeatureManagement;

/// <summary>Registration helpers for AGTec's feature-flag conventions.</summary>
public static class AGTecFeatureManagementExtensions
{
    /// <summary>
    /// Registers Microsoft.FeatureManagement reading the <c>FeatureManagement</c> configuration section,
    /// with the built-in filters and AGTec's tenant-aware targeting so rollouts target per tenant
    /// (<see cref="AGTecTenantTargetingContextAccessor"/>). The flag source is configuration by default;
    /// Azure App Configuration (AGTec.FeatureManagement.Azure) feeds the same section with runtime refresh.
    /// Requires <c>AddAGTecTenancy()</c> for the per-tenant targeting to resolve a tenant.
    /// </summary>
    public static IFeatureManagementBuilder AddAGTecFeatureManagement(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        return services.AddFeatureManagement().WithTargeting<AGTecTenantTargetingContextAccessor>();
    }
}
