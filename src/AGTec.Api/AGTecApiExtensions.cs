using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

namespace AGTec.Api;

/// <summary>
/// API edge conventions: native OpenAPI (with the JWT bearer scheme) and Problem Details.
/// Standardizes on <c>Microsoft.AspNetCore.OpenApi</c> + Scalar — no Swashbuckle.
/// </summary>
public static class AGTecApiExtensions
{
    public static IServiceCollection AddAGTecApiDefaults(this IServiceCollection services)
    {
        // RFC 9457 Problem Details for all error responses.
        services.AddProblemDetails();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        return services;
    }

    /// <summary>Maps the OpenAPI document and the Scalar API reference UI (at <c>/scalar</c>).</summary>
    public static WebApplication MapAGTecOpenApi(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
        return app;
    }
}
