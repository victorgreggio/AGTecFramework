using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AGTec.FeatureManagement.Azure;

/// <summary>
/// Wires Azure App Configuration as the feature-flag source. App Configuration's flags land in the
/// standard <c>FeatureManagement</c> configuration section, so <c>AddAGTecFeatureManagement()</c> consumes
/// them unchanged — this adapter only adds the configuration source and the runtime refresh.
/// </summary>
public static class AGTecAzureAppConfigurationExtensions
{
    /// <summary>
    /// Adds Azure App Configuration as a configuration source with feature flags and dynamic refresh.
    /// Authenticates with a connection string (<c>ConnectionStrings:appconfig</c>) when present, otherwise
    /// with <c>DefaultAzureCredential</c> against <c>AGTec:FeatureFlags:AppConfig:Endpoint</c> (managed
    /// identity in AKS, developer credentials locally). Call before <c>AddAGTecFeatureManagement()</c>.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="refreshInterval">How often to poll for flag changes. Defaults to 30 seconds.</param>
    public static IHostApplicationBuilder AddAGTecAzureAppConfiguration(
        this IHostApplicationBuilder builder,
        TimeSpan? refreshInterval = null
    )
    {
        var connectionString = builder.Configuration.GetConnectionString("appconfig");
        var endpoint = builder.Configuration["AGTec:FeatureFlags:AppConfig:Endpoint"];
        var interval = refreshInterval ?? TimeSpan.FromSeconds(30);

        builder.Configuration.AddAzureAppConfiguration(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.Connect(connectionString);
            }
            else
            {
                var uri = new Uri(
                    endpoint
                        ?? throw new InvalidOperationException(
                            "AGTec:FeatureFlags:AppConfig:Endpoint or ConnectionStrings:appconfig is required for Azure App Configuration."
                        )
                );
                options.Connect(uri, new DefaultAzureCredential());
            }

            options.UseFeatureFlags(featureFlags => featureFlags.SetRefreshInterval(interval));
        });

        builder.Services.AddAzureAppConfiguration();
        return builder;
    }

    /// <summary>
    /// Adds the App Configuration refresh middleware so flag changes are picked up at runtime. Place it
    /// early in the pipeline (before the endpoints).
    /// </summary>
    public static IApplicationBuilder UseAGTecAzureAppConfiguration(this IApplicationBuilder app) =>
        app.UseAzureAppConfiguration();
}
