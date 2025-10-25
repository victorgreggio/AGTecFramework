using Microsoft.AspNetCore.Authorization;

namespace AGTec.Services.ServiceDefaults.Authentication.Requirements;
internal class ScopeAuthorizationRequirement : IAuthorizationRequirement
{
    public ScopeAuthorizationRequirement(params string[] allowedScopes)
    {
        AllowedScopes = allowedScopes;
    }

    public string[] AllowedScopes { get; }
}