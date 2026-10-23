using AGTec.Platform;
using Dapr.Client;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Platform.Dapr;

/// <summary>Registration helpers for the Dapr platform adapters.</summary>
public static class AGTecDaprExtensions
{
    /// <summary>
    /// Registers <see cref="ISecretStore"/> backed by the named Dapr secret store component.
    /// The Dapr sidecar must be running (injected in AKS, or run via the Aspire AppHost locally).
    /// </summary>
    public static IServiceCollection AddDaprSecrets(this IServiceCollection services, string storeName)
    {
        // DaprClientBuilder reads DAPR_GRPC_PORT / DAPR_HTTP_PORT to reach the local sidecar.
        services.AddSingleton(_ => new DaprClientBuilder().Build());
        services.AddSingleton<ISecretStore>(sp => new DaprSecretStore(sp.GetRequiredService<DaprClient>(), storeName));
        return services;
    }
}
