namespace AGTec.Security.MultiTenant;

/// <summary>
/// Normalises OIDC endpoints discovered from a provider's metadata onto the tenant's browser-facing origin.
/// </summary>
public static class OidcEndpointNormalizer
{
    /// <summary>
    /// Rewrite a discovered OIDC endpoint onto the tenant Authority's (browser-facing) origin, keeping its
    /// path and query. Some providers (e.g. Keycloak with a fixed frontend hostname + dynamic backchannel)
    /// return token/logout endpoints on an internal (backchannel) host; the browser must call them on the same
    /// public origin as the authorization endpoint, so normalise all browser-facing endpoints through here.
    /// Returns the endpoint unchanged when either value is missing or not an absolute URI.
    /// </summary>
    public static string? WithFrontendOrigin(string? endpoint, string authority)
    {
        if (
            string.IsNullOrWhiteSpace(endpoint)
            || string.IsNullOrWhiteSpace(authority)
            || !Uri.TryCreate(endpoint, UriKind.Absolute, out var url)
            || !Uri.TryCreate(authority, UriKind.Absolute, out var origin)
        )
        {
            return endpoint;
        }

        return $"{origin.Scheme}://{origin.Authority}{url.PathAndQuery}";
    }
}
