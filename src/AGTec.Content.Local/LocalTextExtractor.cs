using System.Text;
using AGTec.Content;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace AGTec.Content.Local;

/// <summary>
/// Local <see cref="ITextExtractor"/> with no cloud dependency: plain text/markdown, PDF (via PdfPig),
/// and Word (.docx via OpenXml). A cloud adapter (e.g. Azure Content Understanding) can replace it
/// behind the same port, selected by configuration.
/// </summary>
public sealed class LocalTextExtractor : ITextExtractor
{
    public Task<string> ExtractAsync(
        byte[] content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default
    )
    {
        var text = Classify(contentType, fileName) switch
        {
            DocKind.Pdf => ExtractPdf(content),
            DocKind.Docx => ExtractDocx(content),
            _ => Encoding.UTF8.GetString(content),
        };

        return Task.FromResult(text);
    }

    private enum DocKind
    {
        Text,
        Pdf,
        Docx,
    }

    private static DocKind Classify(string contentType, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension == ".pdf" || contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
        {
            return DocKind.Pdf;
        }

        if (extension == ".docx" || contentType.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase))
        {
            return DocKind.Docx;
        }

        return DocKind.Text;
    }

    private static string ExtractPdf(byte[] content)
    {
        using var pdf = PdfDocument.Open(content);
        var builder = new StringBuilder();
        foreach (var page in pdf.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return builder.ToString();
    }

    private static string ExtractDocx(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var document = WordprocessingDocument.Open(stream, isEditable: false);
        return document.MainDocumentPart?.Document.Body?.InnerText ?? string.Empty;
    }
}
