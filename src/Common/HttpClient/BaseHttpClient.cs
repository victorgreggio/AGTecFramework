using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AGTec.Common.HttpClient.Configuration;
using AGTec.Common.HttpClient.Extensions;

namespace AGTec.Common.HttpClient;

public abstract class BaseHttpClient : IBaseHttpClient
{
    private readonly System.Net.Http.HttpClient _httpClient;

    protected BaseHttpClient(System.Net.Http.HttpClient httpClient,
        IEndpointConfiguration configuration)
    {
        httpClient.BaseAddress = new Uri(configuration.BaseUrl);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        _httpClient = httpClient;
        Configuration = configuration;
    }

    protected BaseHttpClient(System.Net.Http.HttpClient httpClient, ISecuredEndpointConfiguration configuration)
        : this(httpClient, (IEndpointConfiguration)configuration)
    {
        IsSecured = true;
    }

    protected bool IsSecured { get; }

    protected IEndpointConfiguration Configuration { get; }

    #region IDisposable

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #endregion

    protected async Task<HttpResponseMessage> SendRequest(string requestUri)
    {
        return await SendRequest(requestUri, HttpMethod.Get)
            .ConfigureAwait(false);
    }

    protected async Task<HttpResponseMessage> SendRequest(string requestUri, HttpMethod method)
    {
        return await SendRequest(requestUri, method, new object())
            .ConfigureAwait(false);
    }

    protected async Task<HttpResponseMessage> SendRequest<T>(string requestUri, HttpMethod method, T model)
        where T : class
    {
        try
        {
            var client = await GetConfiguredHttpClient();

            if (method == HttpMethod.Post)
                return await client.PostAsJsonAsync(requestUri, model)
                    .ConfigureAwait(false);

            if (method == HttpMethod.Put)
                return await client.PutAsJsonAsync(requestUri, model)
                    .ConfigureAwait(false);

            if (method == HttpMethod.Delete)
                return await client.DeleteAsync(requestUri)
                    .ConfigureAwait(false);

            return await client.GetAsync(requestUri)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error executing {method} on {requestUri}.", ex);
        }
    }

    private async Task<System.Net.Http.HttpClient> GetConfiguredHttpClient()
    {
        if (IsSecured)
            await ((ISecuredEndpointConfiguration)Configuration).SetAuthentication(_httpClient);

        return _httpClient;
    }
}