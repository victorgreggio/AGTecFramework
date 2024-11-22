using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AGTec.Common.HttpClient.Configuration;

public class BasicAuthEndpointConfiguration : EnpointConfiguration, ISecuredEndpointConfiguration
{
    private static readonly string AUTH_SCHEMA = "Basic";
    private static readonly string SEPARATOR = ":";

    public string Username { get; set; }
    public string Password { get; set; }

    public Task SetAuthentication(System.Net.Http.HttpClient client)
    {
        var base64String = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(
                string.Concat(Username, SEPARATOR, Password)));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(AUTH_SCHEMA, base64String);

        return Task.CompletedTask;
    }
}