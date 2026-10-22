using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace AGTec.Security.MultiTenant;

/// <summary>
/// Authenticates a bearer token against the IdP of the tenant the request targets. The tenant is taken from
/// the <c>/{routePrefix}/{tenantId}/...</c> route (or the master tenant for routes without a tenant, such as
/// system-management endpoints). Because each tenant has its own issuer, the IdP that validates the token is
/// the tenant boundary — a token signed by another tenant's IdP fails here. The tenant registry and the OIDC
/// metadata cache are resolved from request services, so the handler carries no per-tenant state.
/// </summary>
public sealed partial class MultiTenantJwtBearerHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to load OIDC metadata for tenant {Tenant}")]
    private static partial void LogMetadataFailure(ILogger logger, string tenant, Exception exception);

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = header["Bearer ".Length..].Trim();
        var services = Context.RequestServices;
        var registry = services.GetRequiredService<ITenantOidcRegistry>();
        var cache = services.GetRequiredService<OidcConfigurationCache>();
        var opts = services.GetRequiredService<IOptions<AGTecMultiTenantAuthOptions>>().Value;

        var tenantId = ResolveTenantId(opts) ?? opts.MasterTenantId;

        var registration = await registry.FindAsync(tenantId, Context.RequestAborted);
        if (registration is null)
        {
            return AuthenticateResult.Fail($"No identity provider is configured for tenant '{tenantId}'.");
        }

        OpenIdConnectConfiguration oidc;
        try
        {
            oidc = await cache.GetAsync(
                registration.ResolveMetadataAddress(),
                registration.RequireHttpsMetadata,
                Context.RequestAborted);
        }
        catch (Exception ex)
        {
            LogMetadataFailure(Logger, tenantId, ex);
            return AuthenticateResult.Fail("The tenant's identity provider metadata is unavailable.");
        }

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = oidc.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = oidc.SigningKeys,
            ValidateLifetime = true,
            ValidateAudience = registration.ValidateAudience,
            ValidAudience = registration.Audience,
            NameClaimType = opts.NameClaimType,
            RoleClaimType = opts.RoleClaimType,
        };

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);
        if (!result.IsValid)
        {
            return AuthenticateResult.Fail(result.Exception?.Message ?? "The bearer token is invalid.");
        }

        var identity = result.ClaimsIdentity;
        identity.AddClaim(new Claim(opts.TenantClaimType, tenantId));
        Context.Items[opts.TenantClaimType] = tenantId;

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    private string? ResolveTenantId(AGTecMultiTenantAuthOptions opts)
    {
        if (Request.RouteValues.TryGetValue(opts.RouteParameterName, out var value)
            && value is string routeTenant
            && !string.IsNullOrWhiteSpace(routeTenant))
        {
            return routeTenant;
        }

        var segments = Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments is { Length: >= 2 }
            && string.Equals(segments[0], opts.RoutePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return segments[1];
        }

        return null;
    }
}
