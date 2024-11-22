using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AGTec.Common.HttpClient.Extensions;

public static class HttpClientExtensions
{
    private const string JsonMediaType = "application/json";

    public static async Task<HttpResponseMessage> PostAsJsonAsync(this System.Net.Http.HttpClient client,
        string requestUrl, object model)
    {
        var json = JsonConvert.SerializeObject(model);
        var stringContent = new StringContent(json, Encoding.UTF8, JsonMediaType);
        var response = await client.PostAsync(requestUrl, stringContent);
        return response;
    }

    public static async Task<HttpResponseMessage> PutAsJsonAsync(this System.Net.Http.HttpClient client,
        string requestUrl, object model)
    {
        var json = JsonConvert.SerializeObject(model);
        var stringContent = new StringContent(json, Encoding.UTF8, JsonMediaType);
        var response = await client.PutAsync(requestUrl, stringContent);
        return response;
    }
}