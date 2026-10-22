using System.Collections.Concurrent;

namespace AGTec.VectorData;

/// <summary>In-memory vector store (cosine similarity) for single-process dev/test.</summary>
public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, VectorRecord>> _partitions = new();

    public Task UpsertAsync(
        string partition,
        IReadOnlyList<VectorRecord> records,
        CancellationToken cancellationToken = default
    )
    {
        var store = _partitions.GetOrAdd(partition, _ => new ConcurrentDictionary<string, VectorRecord>());
        foreach (var record in records)
        {
            store[record.Id] = record;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorMatch>> SearchAsync(
        string partition,
        ReadOnlyMemory<float> query,
        int topK,
        IReadOnlyDictionary<string, string>? filter = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!_partitions.TryGetValue(partition, out var store))
        {
            return Task.FromResult<IReadOnlyList<VectorMatch>>([]);
        }

        var matches = store
            .Values.Where(record => Matches(record, filter))
            .Select(record => new VectorMatch(
                record.Id,
                record.Content,
                Cosine(query.Span, record.Embedding.Span),
                record.Metadata ?? EmptyMetadata
            ))
            .OrderByDescending(match => match.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<VectorMatch>>(matches);
    }

    public Task<int> CountAsync(string partition, CancellationToken cancellationToken = default) =>
        Task.FromResult(_partitions.TryGetValue(partition, out var store) ? store.Count : 0);

    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata = new Dictionary<string, string>();

    private static bool Matches(VectorRecord record, IReadOnlyDictionary<string, string>? filter)
    {
        if (filter is null || filter.Count == 0)
        {
            return true;
        }

        return filter.All(entry =>
            record.Metadata is not null && record.Metadata.TryGetValue(entry.Key, out var value) && value == entry.Value
        );
    }

    private static double Cosine(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length)
        {
            return 0d;
        }

        double dot = 0d,
            normA = 0d,
            normB = 0d;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return normA <= 0d || normB <= 0d ? 0d : dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
