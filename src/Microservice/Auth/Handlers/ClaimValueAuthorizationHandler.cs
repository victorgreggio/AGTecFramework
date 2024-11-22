using System.Linq;
using System.Threading.Tasks;
using AGTec.Microservice.Auth.Configuration;
using AGTec.Microservice.Auth.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace AGTec.Microservice.Auth.Handlers;

internal class ClaimValueAuthorizationHandler : AuthorizationHandler<ClaimValueAuthorizationRequirement>
{
    private const string LocalAuthIssuer = "LOCAL AUTHORITY";
    private readonly string _authIssuer;

    public ClaimValueAuthorizationHandler(IAuthConfiguration authConfiguration)
    {
        _authIssuer = authConfiguration.AuthIssuer;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        ClaimValueAuthorizationRequirement requirement)
    {
        if (context.User.HasClaim(c =>
                (c.Issuer == _authIssuer || c.Issuer == LocalAuthIssuer) &&
                c.Type == requirement.ClaimType && requirement.ClaimValues.Contains(c.Value)))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}