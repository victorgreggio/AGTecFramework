using System;
using Microsoft.AspNetCore.Authorization;

namespace AGTec.Microservice.Auth.Attributes;

public class ClaimOrScopeAuthorizeAttribute : AuthorizeAttribute
{
    public const string POLICY_PREFIX = "ClaimOrScopeAuthorize";

    public ClaimOrScopeAuthorizeAttribute(string claim, string scope)
    {
        if (string.IsNullOrWhiteSpace(claim) || string.IsNullOrWhiteSpace(scope))
            throw new Exception("Invalid claim and/or scope.");

        Claim = claim;
        Scope = scope;
        Policy = $"{POLICY_PREFIX}{claim}|{scope}";
    }

    public string Claim { get; set; }
    public string Scope { get; set; }
}