using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AGTec.Security.MultiTenant;

/// <summary>
/// Wiring for per-tenant OIDC authentication. Each tenant brings its own identity provider; a bearer token is
/// validated against the IdP of the tenant the request targets (the validating issuer is the tenant boundary).
/// The host supplies an <see cref="ITenantOidcRegistry"/> (AGTec stays persistence-agnostic) and builds its own
/// authorization policies with <see cref="AGTecSecurityExtensions.RequireScope{TBuilder}"/> / standard ASP.NET
/// Core policy APIs — this extension only registers the authentication scheme and its supporting services.
/// </summary>
public static class AGTecMultiTenantSecurityExtensions
{
    /// <summary>
    /// Registers the multi-tenant OIDC authentication scheme (<see cref="MultiTenantJwtBearerHandler"/>), the
    /// shared <see cref="OidcConfigurationCache"/>, an <see cref="System.Net.Http.IHttpClientFactory"/>, and the
    /// scope authorization handler. Register your <see cref="ITenantOidcRegistry"/> implementation separately.
    /// </summary>
    public static IHostApplicationBuilder AddAGTecMultiTenantJwtBearer(
        this IHostApplicationBuilder builder,
        Action<AGTecMultiTenantAuthOptions>? configure = null
    )
    {
        var options = new AGTecMultiTenantAuthOptions();
        configure?.Invoke(options);

        builder.Services.AddSingleton(Options.Create(options));
        builder.Services.AddSingleton<OidcConfigurationCache>();
        builder.Services.AddHttpClient();

        builder
            .Services.AddAuthentication(options.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, MultiTenantJwtBearerHandler>(
                options.SchemeName,
                configureOptions: null
            );

        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();

        return builder;
    }
}
