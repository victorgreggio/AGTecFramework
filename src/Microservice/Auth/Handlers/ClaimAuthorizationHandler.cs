using System.Linq;
using System.Threading.Tasks;
using AGTec.Microservice.Auth.Configuration;
using AGTec.Microservice.Auth.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace AGTec.Microservice.Auth.Handlers;

internal class ClaimAuthorizationHandler : AuthorizationHandler<ClaimAuthorizationRequirement>
{
    private const string LocalAuthIssuer = "LOCAL AUTHORITY";
    private readonly string _authIssuer;

    public ClaimAuthorizationHandler(IAuthConfiguration authConfiguration)
    {
        _authIssuer = authConfiguration.AuthIssuer;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        ClaimAuthorizationRequirement requirement)
    {
        if (context.User.HasClaim(c =>
                (c.Issuer == _authIssuer || c.Issuer == LocalAuthIssuer) &&
                requirement.ClaimTypes.Contains(c.Type)))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}