using Microsoft.AspNetCore.Authorization;

namespace AGTec.Security;

/// <summary>
/// Requires that the caller's token grant at least one of the named OAuth scopes.
/// </summary>
public sealed class ScopeRequirement(params string[] scopes) : IAuthorizationRequirement
{
    public IReadOnlyCollection<string> Scopes { get; } = scopes;
}

/// <summary>
/// Satisfies a <see cref="ScopeRequirement"/> by inspecting the standard scope claims
/// (<c>scope</c> / <c>scp</c>), which may be space-delimited or repeated.
/// </summary>
public sealed class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRequirement requirement)
    {
        var granted = context
            .User.FindAll("scope")
            .Concat(context.User.FindAll("scp"))
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet(StringComparer.Ordinal);

        if (requirement.Scopes.Any(granted.Contains))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
