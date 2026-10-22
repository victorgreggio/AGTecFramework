namespace AGTec.Content;

/// <summary>
/// Extracts plain text from a document's raw bytes. The local default (<c>AGTec.Content.Local</c>)
/// handles plain text, PDF, and Word with no cloud dependency; a cloud adapter (e.g. Azure Content
/// Understanding) plugs in behind this same port, selected by configuration — no recompile to switch.
/// </summary>
public interface ITextExtractor
{
    /// <summary>
    /// Extracts the document's text content.
    /// </summary>
    /// <param name="content">The raw document bytes.</param>
    /// <param name="contentType">The MIME type (e.g. <c>application/pdf</c>); may be empty.</param>
    /// <param name="fileName">The original file name, used to classify by extension.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The extracted plain text.</returns>
    Task<string> ExtractAsync(
        byte[] content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default
    );
}
