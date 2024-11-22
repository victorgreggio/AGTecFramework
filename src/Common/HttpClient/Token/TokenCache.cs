using System;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace AGTec.Common.HttpClient.Token;

public class TokenCache : ITokenCache
{
    private readonly string _authorityIdentity;
    private readonly string _client;
    private readonly string _scope;
    private readonly string _secret;
    private DateTime _tokenCreation;
    private TokenResponse _tokenResponse;

    public TokenCache(string client,
        string secret,
        string scope,
        string authorityIdentity)
    {
        _authorityIdentity = authorityIdentity;
        _client = client;
        _secret = secret;
        _scope = scope;
    }

    public async Task<string> GetAccessToken(bool forceRefresh = false)
    {
        if (!forceRefresh && IsTokenValid()) return _tokenResponse.AccessToken;

        await RequestToken().ConfigureAwait(false);

        if (!IsTokenValid())
            throw new InvalidOperationException(
                "An unexpected token validation error has occured during a token request.");

        return _tokenResponse.AccessToken;
    }

    private bool IsTokenValid()
    {
        return _tokenResponse != null &&
               _tokenResponse.IsError == false &&
               string.IsNullOrWhiteSpace(_tokenResponse.AccessToken) == false &&
               _tokenCreation.AddSeconds(_tokenResponse.ExpiresIn) > DateTime.UtcNow;
    }

    private async Task RequestToken()
    {
        _tokenCreation = DateTime.UtcNow;
        _tokenResponse = await new System.Net.Http.HttpClient()
            .RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = _authorityIdentity + "connect/token",
                ClientId = _client,
                ClientSecret = _secret,
                Scope = _scope
            })
            .ConfigureAwait(false);
    }
}