using System.Threading.Tasks;
using AGTec.Common.HttpClient.Token;
using IdentityModel.Client;

namespace AGTec.Common.HttpClient.Configuration;

public class OAuthEndpointConfiguration : EnpointConfiguration, IOAuthEndpointConfiguration
{
    private ITokenCache _tokenCache;

    private ITokenCache TokenCache =>
        _tokenCache ?? (_tokenCache = new TokenCache(Client, Secret, Scope, AuthorityIdentity));

    public string Client { get; set; }
    public string Secret { get; set; }
    public string Scope { get; set; }
    public string AuthorityIdentity { get; set; }

    public async Task SetAuthentication(System.Net.Http.HttpClient client)
    {
        client.SetBearerToken(await TokenCache.GetAccessToken());
    }
}