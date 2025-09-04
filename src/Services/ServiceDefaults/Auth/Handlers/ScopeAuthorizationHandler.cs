using AGTec.Services.ServiceDefaults.Auth.Configuration;
using AGTec.Services.ServiceDefaults.Auth.Requirements;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace AGTec.Services.ServiceDefaults.Auth.Handlers;
internal class ScopeAuthorizationHandler : AuthorizationHandler<ScopeAuthorizationRequirement>
{
    private const string ScopeClaim = "scope";
    private readonly string _authIssuer;

    public ScopeAuthorizationHandler(IAuthConfiguration authConfiguration)
    {
        _authIssuer = authConfiguration.AuthIssuer;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        ScopeAuthorizationRequirement requirement)
    {
        if (context.User.HasClaim(c =>
                c.Issuer == _authIssuer && c.Type == ScopeClaim && requirement.AllowedScopes.Contains(c.Value)))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}