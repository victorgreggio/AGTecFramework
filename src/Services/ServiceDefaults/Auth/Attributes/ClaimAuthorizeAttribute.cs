using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;

namespace AGTec.Services.ServiceDefaults.Auth.Attributes;

public class ClaimAuthorizeAttribute : AuthorizeAttribute
{
    public const string POLICY_PREFIX = "ClaimAuthorize";

    public ClaimAuthorizeAttribute(params string[] claims)
    {
        if (claims.Length <= 0 || claims.Any(c => string.IsNullOrWhiteSpace(c)))
            throw new Exception("Invalid claim(s).");

        Claims = claims;
        Policy = $"{POLICY_PREFIX}{string.Join("|", claims)}";
    }

    public string[] Claims { get; set; }
}