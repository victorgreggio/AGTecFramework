using System.Linq;
using Microsoft.AspNetCore.SignalR;

namespace AGTec.Common.SignalR;

public class UserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        var principal = connection.User;

        var usernameClaim = principal.Identities.FirstOrDefault()?.Claims
            .FirstOrDefault(c => c.Type == "preferred_username");
        var clientIdClaim = principal.Identities.FirstOrDefault()?.Claims.FirstOrDefault(c => c.Type == "client_id");

        if (usernameClaim != null && string.IsNullOrWhiteSpace(usernameClaim.Value) == false)
            return usernameClaim.Value;

        if (clientIdClaim != null && string.IsNullOrWhiteSpace(clientIdClaim.Value) == false)
            return clientIdClaim.Value;

        return string.Empty;
    }
}