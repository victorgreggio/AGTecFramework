using System.Text;
using AGTec.Platform;
using AwesomeAssertions;

namespace AGTec.Platform.Tests;

public sealed class FileSystemObjectStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "agtec-fs-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task Put_then_Get_round_trips_content()
    {
        var store = new FileSystemObjectStore(_root);
        using var input = new MemoryStream(Encoding.UTF8.GetBytes("hello disk"));

        await store.PutAsync("notes", "a/b.txt", input, "text/plain");

        (await store.ExistsAsync("notes", "a/b.txt")).Should().BeTrue();
        await using var output = await store.GetAsync("notes", "a/b.txt");
        output.Should().NotBeNull();
        using var reader = new StreamReader(output!);
        (await reader.ReadToEndAsync()).Should().Be("hello disk");
    }

    [Fact]
    public async Task Delete_removes_the_object()
    {
        var store = new FileSystemObjectStore(_root);
        using var input = new MemoryStream([1, 2, 3]);
        await store.PutAsync("c", "k", input, "application/octet-stream");

        await store.DeleteAsync("c", "k");

        (await store.ExistsAsync("c", "k")).Should().BeFalse();
        (await store.GetAsync("c", "k")).Should().BeNull();
    }

    [Fact]
    public async Task Rejects_keys_that_escape_the_root()
    {
        var store = new FileSystemObjectStore(_root);
        using var input = new MemoryStream([0]);

        var act = async () => await store.PutAsync("c", "../../escape.txt", input, "text/plain");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
