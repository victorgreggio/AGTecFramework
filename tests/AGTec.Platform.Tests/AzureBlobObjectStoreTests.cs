using System.Text;
using AwesomeAssertions;
using Azure.Storage.Blobs;
using AGTec.Platform;
using AGTec.Platform.Azure;
using Testcontainers.Azurite;

namespace AGTec.Platform.Tests;

/// <summary>
/// Proves the Azure Blob adapter against Azurite (the official Azure Storage emulator),
/// so the real Azure code path is exercised without an Azure account. The in-memory store
/// is a drop-in alternative behind the same <see cref="IObjectStore"/> port.
/// </summary>
public sealed class AzureBlobObjectStoreTests : IAsyncLifetime
{
    private readonly AzuriteContainer _azurite = new AzuriteBuilder(
        "mcr.microsoft.com/azure-storage/azurite:3.33.0"
    ).Build();

    private IObjectStore _store = null!;

    public async Task InitializeAsync()
    {
        await _azurite.StartAsync();

        // Pin a storage API version Azurite supports (the newest client default is ahead of it).
        var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2023_11_03);
        _store = new AzureBlobObjectStore(new BlobServiceClient(_azurite.GetConnectionString(), options));
    }

    public Task DisposeAsync() => _azurite.DisposeAsync().AsTask();

    [Fact]
    public async Task Uploaded_blob_can_be_downloaded()
    {
        using var input = new MemoryStream(Encoding.UTF8.GetBytes("blob content"));
        await _store.PutAsync("notes", "key1", input, "text/plain");

        (await _store.ExistsAsync("notes", "key1")).Should().BeTrue();

        await using var output = await _store.GetAsync("notes", "key1");
        output.Should().NotBeNull();
        using var reader = new StreamReader(output!);
        (await reader.ReadToEndAsync()).Should().Be("blob content");
    }

    [Fact]
    public async Task Missing_blob_returns_null()
    {
        (await _store.GetAsync("notes", "absent")).Should().BeNull();
    }
}
