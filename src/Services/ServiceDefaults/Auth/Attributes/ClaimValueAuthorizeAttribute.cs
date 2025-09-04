using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;

namespace AGTec.Services.ServiceDefaults.Auth.Attributes;

public class ClaimValueAuthorizeAttribute : AuthorizeAttribute
{
    public const string POLICY_PREFIX = "ClaimValueAuthorize";

    public ClaimValueAuthorizeAttribute(string claim, params string[] values)
    {
        if (string.IsNullOrWhiteSpace(claim)
            || values.Length <= 0
            || values.Any(v => string.IsNullOrWhiteSpace(v)))
            throw new Exception("Invalid claim and/or value(s).");

        Values = values;
        Policy = $"{POLICY_PREFIX}{claim}|{string.Join("|", values)}";
    }

    public string Claim { get; set; }
    public string[] Values { get; set; }
}