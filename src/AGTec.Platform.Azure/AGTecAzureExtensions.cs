using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using AGTec.Platform;

namespace AGTec.Platform.Azure;

/// <summary>Registration helpers for the Azure platform adapters.</summary>
public static class AGTecAzureExtensions
{
    /// <summary>Key Vault secrets via <see cref="DefaultAzureCredential"/> (Workload Identity in AKS).</summary>
    public static IServiceCollection AddAzureKeyVaultSecrets(this IServiceCollection services, Uri vaultUri)
    {
        services.AddSingleton(new SecretClient(vaultUri, new DefaultAzureCredential()));
        services.AddSingleton<ISecretStore, AzureKeyVaultSecretStore>();
        return services;
    }

    /// <summary>
    /// Blob storage from a connection string (or endpoint + managed identity in production).
    /// Registers both the proxied <see cref="IObjectStore"/> and the SAS-based
    /// <see cref="IObjectUrlProvider"/> (direct client upload/download).
    /// </summary>
    public static IServiceCollection AddAzureBlobStorage(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(new BlobServiceClient(connectionString));
        services.AddSingleton<IObjectStore, AzureBlobObjectStore>();
        services.AddSingleton<IObjectUrlProvider, AzureBlobUrlProvider>();
        return services;
    }
}
