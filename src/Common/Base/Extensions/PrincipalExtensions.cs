using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace AGTec.Common.Base.Extensions;

public static class PrincipalExtensions
{
    public static string GetUsernameFromClaim(this IPrincipal principal)
    {
        if (principal is ClaimsPrincipal claimsPrincipal)
        {
            var usernameClaim = claimsPrincipal.Identities.FirstOrDefault()?.Claims
                .FirstOrDefault(c => c.Type == "preferred_username");
            var clientIdClaim = claimsPrincipal.Identities.FirstOrDefault()?.Claims
                .FirstOrDefault(c => c.Type == "client_id");

            if (usernameClaim != null && string.IsNullOrWhiteSpace(usernameClaim.Value) == false)
                return usernameClaim.Value;

            if (clientIdClaim != null && string.IsNullOrWhiteSpace(clientIdClaim.Value) == false)
                return clientIdClaim.Value;
        }

        return "NoUsername";
    }
}