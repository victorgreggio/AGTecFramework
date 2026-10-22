namespace AGTec.VectorData;

/// <summary>A record to index: an id, its embedding, the source text, and string metadata.</summary>
public sealed record VectorRecord(
    string Id,
    ReadOnlyMemory<float> Embedding,
    string Content,
    IReadOnlyDictionary<string, string>? Metadata = null
);

/// <summary>A search hit with its similarity score (1.0 = identical).</summary>
public sealed record VectorMatch(string Id, string Content, double Score, IReadOnlyDictionary<string, string> Metadata);

/// <summary>
/// Provider-neutral vector store for retrieval. Every operation is scoped to a
/// <c>partition</c> (e.g. a tenant/workspace) so one tenant can never read another's
/// vectors — the isolation property carries across providers. In-memory by default; pgvector or
/// Azure AI Search behind the same port, selected by configuration.
/// </summary>
public interface IVectorStore
{
    Task UpsertAsync(
        string partition,
        IReadOnlyList<VectorRecord> records,
        CancellationToken cancellationToken = default
    );

    /// <summary>Nearest <paramref name="topK"/> by cosine similarity, optionally filtered to records whose metadata matches every entry in <paramref name="filter"/>.</summary>
    Task<IReadOnlyList<VectorMatch>> SearchAsync(
        string partition,
        ReadOnlyMemory<float> query,
        int topK,
        IReadOnlyDictionary<string, string>? filter = null,
        CancellationToken cancellationToken = default
    );

    Task<int> CountAsync(string partition, CancellationToken cancellationToken = default);
}
