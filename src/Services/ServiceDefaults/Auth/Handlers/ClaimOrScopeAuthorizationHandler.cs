using AGTec.Services.ServiceDefaults.Auth.Configuration;
using AGTec.Services.ServiceDefaults.Auth.Requirements;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace AGTec.Services.ServiceDefaults.Auth.Handlers;


internal class ClaimOrScopeAuthorizationHandler : AuthorizationHandler<ClaimOrScopeAuthorizationRequirement>
{
    private const string ScopeClaim = "scope";
    private const string LocalAuthIssuer = "LOCAL AUTHORITY";
    private readonly string _authIssuer;

    public ClaimOrScopeAuthorizationHandler(IAuthConfiguration authConfiguration)
    {
        _authIssuer = authConfiguration.AuthIssuer;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        ClaimOrScopeAuthorizationRequirement requirement)
    {
        if (context.User.HasClaim(c =>
                (c.Issuer == _authIssuer || c.Issuer == LocalAuthIssuer) &&
                c.Type == requirement.ClaimType))
            context.Succeed(requirement);

        if (context.User.HasClaim(c =>
                (c.Issuer == _authIssuer || c.Issuer == LocalAuthIssuer) &&
                c.Type == ScopeClaim && requirement.AllowedScopes.Contains(c.Value)))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}