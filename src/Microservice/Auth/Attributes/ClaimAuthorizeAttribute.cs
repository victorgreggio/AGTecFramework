using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace AGTec.Microservice.Auth.Attributes;

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