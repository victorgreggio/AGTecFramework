using Microsoft.AspNetCore.Authorization;

namespace AGTec.Services.ServiceDefaults.Auth.Requirements;

internal class ClaimAuthorizationRequirement : IAuthorizationRequirement
{
    public ClaimAuthorizationRequirement(string[] claimTypes)
    {
        ClaimTypes = claimTypes;
    }

    public string[] ClaimTypes { get; }
}