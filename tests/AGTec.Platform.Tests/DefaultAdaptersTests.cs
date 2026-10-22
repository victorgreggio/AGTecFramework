using System.Text;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using AGTec.Platform;

namespace AGTec.Platform.Tests;

public class ConfigurationSecretStoreTests
{
    [Fact]
    public async Task Reads_secret_from_configuration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Secrets:ApiKey"] = "s3cr3t" })
            .Build();

        var store = new ConfigurationSecretStore(configuration);

        (await store.GetSecretAsync("ApiKey")).Should().Be("s3cr3t");
    }

    [Fact]
    public async Task Returns_null_for_a_missing_secret()
    {
        var store = new ConfigurationSecretStore(new ConfigurationBuilder().Build());

        (await store.GetSecretAsync("Missing")).Should().BeNull();
    }
}

public class InMemoryObjectStoreTests
{
    [Fact]
    public async Task Put_then_Get_round_trips_content()
    {
        var store = new InMemoryObjectStore();
        using var input = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

        await store.PutAsync("c", "k", input, "text/plain");

        (await store.ExistsAsync("c", "k")).Should().BeTrue();
        await using var output = await store.GetAsync("c", "k");
        output.Should().NotBeNull();
        using var reader = new StreamReader(output!);
        (await reader.ReadToEndAsync()).Should().Be("hello");
    }

    [Fact]
    public async Task Delete_removes_the_object()
    {
        var store = new InMemoryObjectStore();
        using var input = new MemoryStream([1, 2, 3]);
        await store.PutAsync("c", "k", input, "application/octet-stream");

        await store.DeleteAsync("c", "k");

        (await store.ExistsAsync("c", "k")).Should().BeFalse();
        (await store.GetAsync("c", "k")).Should().BeNull();
    }
}
