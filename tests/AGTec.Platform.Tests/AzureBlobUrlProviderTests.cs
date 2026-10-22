using System.Globalization;
using System.Linq;
using AwesomeAssertions;
using Azure.Storage.Blobs;
using AGTec.Platform;
using AGTec.Platform.Azure;
using Testcontainers.Azurite;

namespace AGTec.Platform.Tests;

/// <summary>
/// Proves the Azure Blob SAS adapter generates correctly-scoped, signed URLs for direct
/// client upload/download. We assert the SAS structure (signature, permissions, resource,
/// expiry) rather than round-tripping through Azurite, because Azurite's supported SAS
/// version trails the current SDK — a real Azure endpoint accepts these URLs.
/// </summary>
public sealed class AzureBlobUrlProviderTests : IAsyncLifetime
{
    private readonly AzuriteContainer _azurite = new AzuriteBuilder(
        "mcr.microsoft.com/azure-storage/azurite:3.33.0"
    ).Build();

    private IObjectUrlProvider _urls = null!;

    public async Task InitializeAsync()
    {
        await _azurite.StartAsync();

        var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2023_11_03);
        _urls = new AzureBlobUrlProvider(new BlobServiceClient(_azurite.GetConnectionString(), options));
    }

    public Task DisposeAsync() => _azurite.DisposeAsync().AsTask();

    [Fact]
    public async Task Upload_url_is_signed_and_scoped_to_create_write()
    {
        var url = await _urls.CreateUploadUrlAsync("docs", "file.txt", TimeSpan.FromMinutes(5), "text/plain");

        url.AbsolutePath.Should().Contain("docs/file.txt");
        Param(url, "sig").Should().NotBeNullOrEmpty();
        Param(url, "sr").Should().Be("b"); // blob resource
        var permissions = Param(url, "sp");
        permissions.Should().Contain("w").And.Contain("c");
        Expiry(url).Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Download_url_is_read_only()
    {
        var url = await _urls.CreateDownloadUrlAsync("docs", "file.txt", TimeSpan.FromMinutes(5));

        Param(url, "sig").Should().NotBeNullOrEmpty();
        Param(url, "sp").Should().Be("r"); // read only
        Expiry(url).Should().BeAfter(DateTimeOffset.UtcNow);
    }

    private static string? Param(Uri uri, string key) =>
        uri
            .Query.TrimStart('?')
            .Split('&')
            .Select(pair => pair.Split('=', 2))
            .Where(kv => kv.Length == 2 && kv[0] == key)
            .Select(kv => Uri.UnescapeDataString(kv[1]))
            .FirstOrDefault();

    private static DateTimeOffset Expiry(Uri uri) =>
        DateTimeOffset.Parse(Param(uri, "se")!, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
}
