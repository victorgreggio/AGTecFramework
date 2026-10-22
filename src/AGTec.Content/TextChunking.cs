using System.Text;

namespace AGTec.Content;

/// <summary>A chunk of extracted text with its ordinal index and optional source page.</summary>
public sealed record TextChunk(int Index, string Content, int? Page);

/// <summary>Options for <see cref="ParagraphTextChunker"/>.</summary>
public sealed class TextChunkerOptions
{
    /// <summary>Target maximum characters per chunk. A paragraph is never split, so a single long
    /// paragraph may exceed this. Defaults to 1200.</summary>
    public int TargetCharacters { get; set; } = 1200;
}

/// <summary>
/// Splits text into chunks suitable for embedding + retrieval. Adapters that need richer,
/// token-aware chunking implement this port behind the same seam.
/// </summary>
public interface ITextChunker
{
    IReadOnlyList<TextChunk> Chunk(string text);
}

/// <summary>
/// A paragraph-aware chunker: accumulates paragraphs up to a target character budget, never splitting
/// a paragraph. Adequate for most RAG ingestion; swap the <see cref="ITextChunker"/> registration for
/// a token-aware implementation when needed.
/// </summary>
public sealed class ParagraphTextChunker(TextChunkerOptions? options = null) : ITextChunker
{
    private readonly int _targetChars = options?.TargetCharacters ?? new TextChunkerOptions().TargetCharacters;

    public IReadOnlyList<TextChunk> Chunk(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var paragraphs = text.Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var chunks = new List<TextChunk>();
        var current = new StringBuilder();

        void Flush()
        {
            if (current.Length > 0)
            {
                chunks.Add(new TextChunk(chunks.Count, current.ToString().Trim(), null));
                current.Clear();
            }
        }

        foreach (var paragraph in paragraphs)
        {
            if (current.Length > 0 && current.Length + paragraph.Length > _targetChars)
            {
                Flush();
            }

            current.AppendLine(paragraph);
        }

        Flush();
        return chunks;
    }
}
