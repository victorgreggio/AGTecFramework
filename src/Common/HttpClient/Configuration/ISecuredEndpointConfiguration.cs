using System.Threading.Tasks;

namespace AGTec.Common.HttpClient.Configuration;

public interface ISecuredEndpointConfiguration : IEndpointConfiguration
{
    Task SetAuthentication(System.Net.Http.HttpClient client);
}