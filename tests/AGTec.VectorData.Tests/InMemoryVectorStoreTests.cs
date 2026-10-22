using AwesomeAssertions;
using AGTec.VectorData;

namespace AGTec.VectorData.Tests;

public class InMemoryVectorStoreTests
{
    private static readonly IReadOnlyList<VectorRecord> Records =
    [
        new("a", new float[] { 1, 0, 0 }, "apple", new Dictionary<string, string> { ["cat"] = "fruit" }),
        new("b", new float[] { 0, 1, 0 }, "banana", new Dictionary<string, string> { ["cat"] = "fruit" }),
        new("c", new float[] { 0, 0, 1 }, "carrot", new Dictionary<string, string> { ["cat"] = "veg" }),
    ];

    [Fact]
    public async Task Search_returns_nearest_by_cosine()
    {
        var store = new InMemoryVectorStore();
        await store.UpsertAsync("t1", Records);

        var hits = await store.SearchAsync("t1", new float[] { 0.9f, 0.1f, 0f }, topK: 2);

        hits.Should().HaveCount(2);
        hits[0].Id.Should().Be("a");
    }

    [Fact]
    public async Task Partitions_are_isolated()
    {
        var store = new InMemoryVectorStore();
        await store.UpsertAsync("tenant-1", Records);
        await store.UpsertAsync("tenant-2", [new VectorRecord("z", new float[] { 1, 0, 0 }, "other", null)]);

        var hits = await store.SearchAsync("tenant-1", new float[] { 1, 0, 0 }, topK: 10);

        hits.Should().NotContain(h => h.Id == "z");
        (await store.CountAsync("tenant-1")).Should().Be(3);
    }

    [Fact]
    public async Task Metadata_filter_narrows_results()
    {
        var store = new InMemoryVectorStore();
        await store.UpsertAsync("t1", Records);

        var hits = await store.SearchAsync(
            "t1",
            new float[] { 0, 1, 0 },
            topK: 10,
            filter: new Dictionary<string, string> { ["cat"] = "fruit" }
        );

        hits.Should().OnlyContain(h => h.Metadata["cat"] == "fruit");
        hits.Should().NotContain(h => h.Id == "c");
    }
}
