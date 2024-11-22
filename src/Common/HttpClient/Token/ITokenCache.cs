using System.Threading.Tasks;

namespace AGTec.Common.HttpClient.Token;

public interface ITokenCache
{
    Task<string> GetAccessToken(bool forceRefresh = false);
}