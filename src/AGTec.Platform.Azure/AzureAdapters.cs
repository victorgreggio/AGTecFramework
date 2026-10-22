using Azure;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AGTec.Platform;

namespace AGTec.Platform.Azure;

/// <summary>Azure Key Vault adapter for <see cref="ISecretStore"/>.</summary>
public sealed class AzureKeyVaultSecretStore(SecretClient client) : ISecretStore
{
    public async Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.GetSecretAsync(name, cancellationToken: cancellationToken);
            return response.Value.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}

/// <summary>Azure Blob Storage adapter for <see cref="IObjectStore"/>.</summary>
public sealed class AzureBlobObjectStore(BlobServiceClient client) : IObjectStore
{
    public async Task PutAsync(
        string container,
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        var containerClient = client.GetBlobContainerClient(container);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        await containerClient
            .GetBlobClient(key)
            .UploadAsync(
                content,
                new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
                cancellationToken
            );
    }

    public async Task<Stream?> GetAsync(string container, string key, CancellationToken cancellationToken = default)
    {
        var blob = client.GetBlobContainerClient(container).GetBlobClient(key);

        if (!await blob.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return download.Value.Content;
    }

    public async Task<bool> ExistsAsync(string container, string key, CancellationToken cancellationToken = default) =>
        await client.GetBlobContainerClient(container).GetBlobClient(key).ExistsAsync(cancellationToken);

    public async Task DeleteAsync(string container, string key, CancellationToken cancellationToken = default) =>
        await client
            .GetBlobContainerClient(container)
            .GetBlobClient(key)
            .DeleteIfExistsAsync(cancellationToken: cancellationToken);
}
