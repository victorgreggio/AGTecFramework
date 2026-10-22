using System.Text;

namespace AGTec.Content;

/// <summary>
/// A dependency-free <see cref="ITextExtractor"/> that decodes the bytes as UTF-8 text. Suitable for
/// plain text and markdown. For PDF and Word, use the <c>AGTec.Content.Local</c> adapter.
/// </summary>
public sealed class PlainTextExtractor : ITextExtractor
{
    public Task<string> ExtractAsync(
        byte[] content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(Encoding.UTF8.GetString(content));
}
