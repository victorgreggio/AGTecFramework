using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AGTec.Security;

/// <summary>
/// Authentication/authorization conventions. OIDC/JWT bearer is configured from
/// <c>Jwt:Authority</c> / <c>Jwt:Audience</c>, keeping us identity-provider portable
/// (Entra ID by default; any OIDC IdP otherwise). Endpoints opt in to OAuth scopes
/// with <see cref="RequireScope"/>.
/// </summary>
public static class AGTecSecurityExtensions
{
    public static IHostApplicationBuilder AddAGTecJwtBearer(
        this IHostApplicationBuilder builder,
        Action<JwtBearerOptions>? configure = null
    )
    {
        var authority = builder.Configuration["Jwt:Authority"];
        var audience = builder.Configuration["Jwt:Audience"];

        builder
            .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                if (!string.IsNullOrWhiteSpace(authority))
                {
                    options.Authority = authority;
                }

                if (!string.IsNullOrWhiteSpace(audience))
                {
                    options.Audience = audience;
                }

                // Validate audience only when one is configured.
                options.TokenValidationParameters.ValidateAudience = !string.IsNullOrWhiteSpace(audience);

                configure?.Invoke(options);
            });

        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();

        return builder;
    }

    /// <summary>Requires an authenticated caller holding at least one of the given OAuth scopes.</summary>
    public static TBuilder RequireScope<TBuilder>(this TBuilder builder, params string[] scopes)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new ScopeRequirement(scopes));
        });
}
