using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AGTec.Tenancy;

/// <summary>Options for resolving the tenant from the request.</summary>
public sealed class AGTecTenancyOptions
{
    /// <summary>Route parameter carrying the tenant id (e.g. <c>/tenants/{tenantId}/...</c>).</summary>
    public string RouteParameterName { get; set; } = "tenantId";

    /// <summary>Optional route parameter carrying the workspace id.</summary>
    public string? WorkspaceRouteParameterName { get; set; }

    /// <summary>
    /// Token claim the route tenant is validated against when the caller is authenticated. A mismatch
    /// is rejected with 403. Set to <c>null</c> to skip validation (e.g. local dev).
    /// </summary>
    public string? ClaimType { get; set; } = "tenant";
}

/// <summary>
/// Resolves <see cref="ITenantContext"/> from the route and (when authenticated) validates it against
/// the configured claim. Register after <c>UseRouting</c> and <c>UseAuthentication</c>.
/// </summary>
internal sealed class TenantResolutionMiddleware(RequestDelegate next, IOptions<AGTecTenancyOptions> options)
{
    private readonly AGTecTenancyOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var routeTenant = context.Request.RouteValues.TryGetValue(_options.RouteParameterName, out var value)
            ? value?.ToString()
            : null;

        if (!string.IsNullOrEmpty(routeTenant))
        {
            if (!string.IsNullOrEmpty(_options.ClaimType) && context.User.Identity?.IsAuthenticated == true)
            {
                var claim = context.User.FindFirst(_options.ClaimType)?.Value;
                if (claim is not null && !string.Equals(claim, routeTenant, StringComparison.Ordinal))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
            }

            var workspace =
                _options.WorkspaceRouteParameterName is { } key
                && context.Request.RouteValues.TryGetValue(key, out var workspaceValue)
                    ? workspaceValue?.ToString()
                    : null;

            ((TenantContext)tenantContext).Set(routeTenant, workspace);
        }

        await next(context);
    }
}
