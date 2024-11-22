using System;
using System.Linq;
using System.Threading.Tasks;
using AGTec.Microservice.Auth.Attributes;
using AGTec.Microservice.Auth.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AGTec.Microservice.Auth.Providers;

public class AuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    public AuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return FallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
    {
        return FallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(ScopeAuthorizeAttribute.POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            var policy = new AuthorizationPolicyBuilder();
            policy.AddRequirements(new ScopeAuthorizationRequirement(
                policyName.Substring(ScopeAuthorizeAttribute.POLICY_PREFIX.Length).Split('|')));
            return Task.FromResult(policy.Build());
        }

        if (policyName.StartsWith(ClaimOrScopeAuthorizeAttribute.POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            var policy = new AuthorizationPolicyBuilder();
            var paramList = policyName.Substring(ClaimOrScopeAuthorizeAttribute.POLICY_PREFIX.Length).Split('|');
            policy.AddRequirements(new ClaimOrScopeAuthorizationRequirement(paramList[0], paramList[1]));
            return Task.FromResult(policy.Build());
        }

        if (policyName.StartsWith(ClaimAuthorizeAttribute.POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            var policy = new AuthorizationPolicyBuilder();
            var paramList = policyName.Substring(ClaimAuthorizeAttribute.POLICY_PREFIX.Length).Split('|');
            policy.AddRequirements(new ClaimAuthorizationRequirement(paramList));
            return Task.FromResult(policy.Build());
        }

        if (policyName.StartsWith(ClaimValueAuthorizeAttribute.POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            var policy = new AuthorizationPolicyBuilder();
            var paramList = policyName.Substring(ClaimValueAuthorizeAttribute.POLICY_PREFIX.Length).Split('|');
            policy.AddRequirements(new ClaimValueAuthorizationRequirement(paramList[0], paramList.Skip(1).ToArray()));
            return Task.FromResult(policy.Build());
        }

        return FallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}