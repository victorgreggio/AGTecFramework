using AGTec.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement.FeatureFilters;

namespace AGTec.FeatureManagement;

/// <summary>
/// Supplies the Microsoft.FeatureManagement targeting context from AGTec's <see cref="ITenantContext"/>,
/// so percentage rollouts and targeted-audience filters evaluate per tenant: the tenant id becomes a
/// target group and the workspace (or tenant) id is the stable user id used for consistent percentage
/// bucketing. Resolves the request-scoped tenant through <see cref="IHttpContextAccessor"/> so the
/// accessor itself can be a singleton (as Microsoft.FeatureManagement expects).
/// </summary>
public sealed class AGTecTenantTargetingContextAccessor(IHttpContextAccessor httpContextAccessor)
    : ITargetingContextAccessor
{
    public ValueTask<TargetingContext> GetContextAsync()
    {
        var tenant = httpContextAccessor.HttpContext?.RequestServices.GetService<ITenantContext>();

        if (tenant is not { HasTenant: true })
        {
            return ValueTask.FromResult(new TargetingContext { UserId = string.Empty, Groups = [] });
        }

        var context = new TargetingContext
        {
            UserId = tenant.WorkspaceId ?? tenant.TenantId,
            Groups = [tenant.TenantId],
        };

        return ValueTask.FromResult(context);
    }
}
