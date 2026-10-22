namespace AGTec.Security.MultiTenant;

/// <summary>
/// Global (not tenant-scoped) registry of tenant IdP configurations. Authentication happens before any tenant
/// context can be trusted, so this must be queryable <em>without</em> a tenant. A typical implementation builds
/// the master tenant from configuration and persists customer tenants (e.g. as single-tenanted Marten
/// documents). Register your implementation in DI; <see cref="AGTecMultiTenantSecurityExtensions"/> consumes it.
/// </summary>
public interface ITenantOidcRegistry
{
    /// <summary>Resolves a tenant's IdP configuration by id, or <c>null</c> if the tenant is unknown.</summary>
    Task<TenantOidcRegistration?> FindAsync(string tenantId, CancellationToken ct = default);

    /// <summary>Resolves the tenant whose email domains include <paramref name="domain"/> (home-realm discovery).</summary>
    Task<TenantOidcRegistration?> FindByEmailDomainAsync(string domain, CancellationToken ct = default);

    /// <summary>All registered tenants (master + customers).</summary>
    Task<IReadOnlyList<TenantOidcRegistration>> ListAsync(CancellationToken ct = default);

    /// <summary>Registers a new customer tenant. Implementations throw if the id is the master id or exists.</summary>
    Task AddAsync(TenantOidcRegistration registration, CancellationToken ct = default);
}
