using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;

namespace AGTec.Services.ServiceDefaults.Auth.Attributes;

public class ScopeAuthorizeAttribute : AuthorizeAttribute
{
    public const string POLICY_PREFIX = "ScopeAuthorize";

    public ScopeAuthorizeAttribute(params string[] scopes)
    {
        if (scopes.Length <= 0 || scopes.Any(s => string.IsNullOrWhiteSpace(s)))
            throw new Exception("Invalid scope(s).");

        Scopes = scopes;
        Policy = $"{POLICY_PREFIX}{string.Join("|", scopes)}";
    }

    public string[] Scopes { get; set; }
}