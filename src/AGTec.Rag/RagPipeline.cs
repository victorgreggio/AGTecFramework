using AGTec.Content;
using AGTec.VectorData;
using Microsoft.Extensions.AI;

namespace AGTec.Rag;

public sealed record RagDocument(
    string Id,
    string FileName,
    string ContentType,
    ReadOnlyMemory<byte> Content,
    IReadOnlyDictionary<string, string>? Metadata = null
);

public sealed record RagSource(
    string Id,
    string Content,
    double Score,
    IReadOnlyDictionary<string, string> Metadata
);

public interface IRagPipeline
{
    Task IngestAsync(string partition, RagDocument document, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RagSource>> RetrieveAsync(
        string partition,
        string query,
        int topK,
        IReadOnlyDictionary<string, string>? filter = null,
        CancellationToken cancellationToken = default
    );
}

/// <summary>Provider-neutral RAG orchestration with source metadata preserved for attribution.</summary>
public sealed class RagPipeline(
    ITextExtractor extractor,
    ITextChunker chunker,
    IEmbeddingGenerator<string, Embedding<float>> embeddings,
    IVectorStore vectors
) : IRagPipeline
{
    public async Task IngestAsync(string partition, RagDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partition);
        ArgumentException.ThrowIfNullOrWhiteSpace(document.Id);

        var text = await extractor.ExtractAsync(
            document.Content.ToArray(),
            document.ContentType,
            document.FileName,
            cancellationToken
        );
        var chunks = chunker.Chunk(text);
        if (chunks.Count == 0)
        {
            return;
        }

        var generated = await embeddings.GenerateAsync(
            chunks.Select(chunk => chunk.Content),
            cancellationToken: cancellationToken
        );
        var records = chunks
            .Select((chunk, index) => new VectorRecord(
                $"{document.Id}:{chunk.Index}",
                generated[index].Vector,
                chunk.Content,
                BuildMetadata(document, chunk)
            ))
            .ToArray();

        await vectors.UpsertAsync(partition, records, cancellationToken);
    }

    public async Task<IReadOnlyList<RagSource>> RetrieveAsync(
        string partition,
        string query,
        int topK,
        IReadOnlyDictionary<string, string>? filter = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partition);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);

        var embedding = await embeddings.GenerateAsync([query], cancellationToken: cancellationToken);
        var matches = await vectors.SearchAsync(partition, embedding[0].Vector, topK, filter, cancellationToken);
        return matches.Select(match => new RagSource(match.Id, match.Content, match.Score, match.Metadata)).ToArray();
    }

    private static Dictionary<string, string> BuildMetadata(RagDocument document, TextChunk chunk)
    {
        var metadata = new Dictionary<string, string>(document.Metadata ?? new Dictionary<string, string>())
        {
            ["documentId"] = document.Id,
            ["fileName"] = document.FileName,
            ["chunkIndex"] = chunk.Index.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        return metadata;
    }
}
