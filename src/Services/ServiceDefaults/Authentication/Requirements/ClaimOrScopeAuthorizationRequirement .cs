using Microsoft.AspNetCore.Authorization;

namespace AGTec.Services.ServiceDefaults.Authentication.Requirements;

internal class ClaimOrScopeAuthorizationRequirement : IAuthorizationRequirement
{
    public ClaimOrScopeAuthorizationRequirement(string claimType, params string[] allowedScopes)
    {
        ClaimType = claimType;
        AllowedScopes = allowedScopes;
    }

    public string ClaimType { get; }
    public string[] AllowedScopes { get; }
}