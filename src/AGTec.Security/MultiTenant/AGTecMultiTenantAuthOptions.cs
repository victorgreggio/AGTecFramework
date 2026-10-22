namespace AGTec.Security.MultiTenant;

/// <summary>
/// Options for <see cref="AGTecMultiTenantSecurityExtensions.AddAGTecMultiTenantJwtBearer"/>. Everything that
/// is product-specific (the master tenant id, the route shape, the claim the resolved tenant is written as)
/// is configurable, so the handler stays identity-provider- and application-agnostic.
/// </summary>
public sealed class AGTecMultiTenantAuthOptions
{
    /// <summary>The authentication scheme name registered for the multi-tenant handler.</summary>
    public string SchemeName { get; set; } = "MultiTenant";

    /// <summary>Tenant used for routes that carry no <c>{tenantId}</c> (e.g. system-management endpoints).</summary>
    public string MasterTenantId { get; set; } = "master";

    /// <summary>Route parameter carrying the tenant id (e.g. <c>/tenants/{tenantId}/...</c>).</summary>
    public string RouteParameterName { get; set; } = "tenantId";

    /// <summary>First path segment that precedes the tenant id, used as a fallback when the route value is not
    /// yet bound at authentication time (e.g. <c>tenants</c> for <c>/tenants/{tenantId}/...</c>).</summary>
    public string RoutePrefix { get; set; } = "tenants";

    /// <summary>Claim name the resolved tenant id is written to on the authenticated principal (and
    /// <c>HttpContext.Items</c>), so downstream code can read the validated tenant.</summary>
    public string TenantClaimType { get; set; } = "tenant";

    /// <summary>Claim type mapped to <see cref="System.Security.Claims.ClaimsIdentity.Name"/>.</summary>
    public string NameClaimType { get; set; } = "name";

    /// <summary>Claim type treated as the role claim during authorization.</summary>
    public string RoleClaimType { get; set; } = "role";
}
