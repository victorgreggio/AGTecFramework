namespace AGTec.Tenancy;

/// <summary>
/// The tenant the current request is scoped to. Resolved per request from the route and validated
/// against the caller's token claim; every data path (DB session, vector store, object store) keys
/// off it so a request can never touch another tenant's data.
/// </summary>
public interface ITenantContext
{
    /// <summary>Whether a tenant has been resolved for the current request.</summary>
    bool HasTenant { get; }

    /// <summary>The resolved tenant id. Throws if accessed outside a tenant-scoped request.</summary>
    string TenantId { get; }

    /// <summary>Optional workspace scope within the tenant.</summary>
    string? WorkspaceId { get; }
}

/// <summary>Scoped, mutable <see cref="ITenantContext"/> populated by the tenant-resolution middleware.</summary>
internal sealed class TenantContext : ITenantContext
{
    private string? _tenantId;

    public bool HasTenant => _tenantId is not null;

    public string TenantId =>
        _tenantId ?? throw new InvalidOperationException("No tenant is in scope for the current request.");

    public string? WorkspaceId { get; private set; }

    public void Set(string tenantId, string? workspaceId)
    {
        _tenantId = tenantId;
        WorkspaceId = workspaceId;
    }
}
