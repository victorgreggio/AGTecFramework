namespace AGTec.Security.MultiTenant;

/// <summary>
/// A tenant's identity-provider configuration. In a multi-tenant system each tenant can bring its own OIDC
/// provider; a request to <c>/{routePrefix}/{tenantId}/...</c> is authenticated against <em>this tenant's</em>
/// issuer, so the IdP that validates a token is the tenant boundary. The master tenant (used to manage the
/// system) is typically built from configuration; customer tenants are persisted by the host's
/// <see cref="ITenantOidcRegistry"/> implementation. This is a plain POCO — no persistence attributes — so the
/// host is free to store it however it likes (e.g. as a Marten document).
/// </summary>
public sealed class TenantOidcRegistration
{
    /// <summary>Tenant id (also the natural document identity).</summary>
    public string TenantId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>OIDC authority (issuer) base URL, e.g. https://login.example.com/realms/acme.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Explicit discovery document URL; defaults to {Authority}/.well-known/openid-configuration.</summary>
    public string? MetadataAddress { get; set; }

    /// <summary>Expected token audience (validated when <see cref="ValidateAudience"/> is true).</summary>
    public string? Audience { get; set; }

    public bool ValidateAudience { get; set; }

    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Scope/role the caller must hold; falls back to the host's global required scope when null.</summary>
    public string? RequiredScope { get; set; }

    /// <summary>Public client id used for the browser's interactive (authorization-code + PKCE) login.</summary>
    public string? ClientId { get; set; }

    /// <summary>Space-delimited scopes requested during interactive login (e.g. "openid api").</summary>
    public string? Scopes { get; set; }

    /// <summary>Email domains that map to this tenant (home-realm discovery): a user who signs in with
    /// <c>name@&lt;domain&gt;</c> is routed to this tenant's identity provider.</summary>
    public List<string> EmailDomains { get; set; } = [];

    public bool IsMaster { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>The discovery document URL: <see cref="MetadataAddress"/> if set, else derived from the authority.</summary>
    public string ResolveMetadataAddress() =>
        string.IsNullOrWhiteSpace(MetadataAddress)
            ? $"{Authority.TrimEnd('/')}/.well-known/openid-configuration"
            : MetadataAddress;
}
