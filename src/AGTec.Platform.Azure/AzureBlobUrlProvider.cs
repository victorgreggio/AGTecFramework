using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using AGTec.Platform;

namespace AGTec.Platform.Azure;

/// <summary>
/// <see cref="IObjectUrlProvider"/> backed by Azure Blob SAS. Generates short-lived signed
/// URLs for direct client upload/download.
/// </summary>
/// <remarks>
/// Requires a <see cref="BlobServiceClient"/> that can sign SAS — i.e. created with a shared
/// key (connection string). With managed identity (no key), switch to a user-delegation SAS
/// via <c>GetUserDelegationKey</c>; this adapter surfaces that requirement explicitly.
/// </remarks>
public sealed class AzureBlobUrlProvider(BlobServiceClient client) : IObjectUrlProvider
{
    public async Task<Uri> CreateUploadUrlAsync(
        string container,
        string key,
        TimeSpan validFor,
        string? contentType = null,
        CancellationToken cancellationToken = default
    )
    {
        var containerClient = client.GetBlobContainerClient(container);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blob = containerClient.GetBlobClient(key);
        EnsureCanSign(blob);

        var builder = new BlobSasBuilder(
            BlobSasPermissions.Create | BlobSasPermissions.Write,
            DateTimeOffset.UtcNow.Add(validFor)
        )
        {
            BlobContainerName = container,
            BlobName = key,
            Resource = "b",
            ContentType = contentType,
        };

        return blob.GenerateSasUri(builder);
    }

    public Task<Uri> CreateDownloadUrlAsync(
        string container,
        string key,
        TimeSpan validFor,
        CancellationToken cancellationToken = default
    )
    {
        var blob = client.GetBlobContainerClient(container).GetBlobClient(key);
        EnsureCanSign(blob);

        var uri = blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(validFor));
        return Task.FromResult(uri);
    }

    private static void EnsureCanSign(BlobClient blob)
    {
        if (!blob.CanGenerateSasUri)
        {
            throw new InvalidOperationException(
                "The BlobServiceClient cannot generate a SAS. Create it with a shared key "
                    + "(connection string), or use a user-delegation SAS with managed identity."
            );
        }
    }
}
