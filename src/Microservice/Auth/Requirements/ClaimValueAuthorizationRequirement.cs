using Microsoft.AspNetCore.Authorization;

namespace AGTec.Microservice.Auth.Requirements;

internal class ClaimValueAuthorizationRequirement : IAuthorizationRequirement
{
    public ClaimValueAuthorizationRequirement(string claimType, string[] claimValues)
    {
        ClaimType = claimType;
        ClaimValues = claimValues;
    }

    public string ClaimType { get; }
    public string[] ClaimValues { get; }
}