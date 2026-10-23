using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AGTec.Security.MultiTenant;

/// <summary>
/// Caches an OpenID Connect <see cref="ConfigurationManager{T}"/> per discovery endpoint, so each tenant's
/// issuer metadata and signing keys are fetched once and auto-refreshed. Shared across requests (singleton).
/// </summary>
public sealed class OidcConfigurationCache
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<
        string,
        ConfigurationManager<OpenIdConnectConfiguration>
    > _managers = new();

    public async Task<OpenIdConnectConfiguration> GetAsync(
        string metadataAddress,
        bool requireHttps,
        CancellationToken ct
    )
    {
        var manager = _managers.GetOrAdd(
            metadataAddress,
            address => new ConfigurationManager<OpenIdConnectConfiguration>(
                address,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = requireHttps }
            )
        );

        return await manager.GetConfigurationAsync(ct);
    }
}
