using AwesomeAssertions;
using AGTec.VectorData;
using AGTec.VectorData.PgVector;
using Npgsql;
using Testcontainers.PostgreSql;

namespace AGTec.VectorData.Tests;

/// <summary>
/// Proves the pgvector adapter against a real PostgreSQL + vector extension: nearest-neighbour
/// search, partition (tenant) isolation, and metadata filtering — the same IVectorStore contract
/// as the in-memory default.
/// </summary>
public sealed class PgVectorStoreTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg17").Build();

    private NpgsqlDataSource _dataSource = null!;
    private PgVectorStore _store = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _dataSource = NpgsqlDataSource.Create(_postgres.GetConnectionString());
        _store = new PgVectorStore(_dataSource, dimensions: 3);
    }

    public async Task DisposeAsync()
    {
        _store.Dispose();
        await _dataSource.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Search_is_nearest_partition_scoped_and_filterable()
    {
        await _store.UpsertAsync(
            "t1:w1",
            [
                new VectorRecord(
                    "a",
                    new float[] { 1, 0, 0 },
                    "apple",
                    new Dictionary<string, string> { ["cat"] = "fruit" }
                ),
                new VectorRecord(
                    "b",
                    new float[] { 0, 1, 0 },
                    "banana",
                    new Dictionary<string, string> { ["cat"] = "fruit" }
                ),
                new VectorRecord(
                    "c",
                    new float[] { 0, 0, 1 },
                    "carrot",
                    new Dictionary<string, string> { ["cat"] = "veg" }
                ),
            ]
        );
        await _store.UpsertAsync("t2:w1", [new VectorRecord("x", new float[] { 1, 0, 0 }, "other tenant", null)]);

        // Nearest to [0.9, 0.1, 0] is "a".
        var hits = await _store.SearchAsync("t1:w1", new float[] { 0.9f, 0.1f, 0f }, topK: 2);
        hits[0].Id.Should().Be("a");
        hits[0].Score.Should().BeGreaterThan(0.9);

        // Partition isolation: the other tenant's "x" never appears.
        hits.Should().NotContain(h => h.Id == "x");

        // Metadata filter narrows to fruit.
        var fruit = await _store.SearchAsync(
            "t1:w1",
            new float[] { 0, 0, 1 },
            topK: 10,
            filter: new Dictionary<string, string> { ["cat"] = "fruit" }
        );
        fruit.Should().OnlyContain(h => h.Metadata["cat"] == "fruit");

        (await _store.CountAsync("t1:w1")).Should().Be(3);
    }
}
