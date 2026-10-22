using System.Text;
using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using AGTec.Content.Local;

namespace AGTec.Content.Tests;

public class LocalTextExtractorTests
{
    private readonly LocalTextExtractor _extractor = new();

    [Fact]
    public async Task Extracts_plain_text_as_utf8()
    {
        var bytes = Encoding.UTF8.GetBytes("hello from a markdown file");

        var text = await _extractor.ExtractAsync(bytes, "text/markdown", "notes.md");

        text.Should().Be("hello from a markdown file");
    }

    [Fact]
    public async Task Extracts_text_from_a_docx()
    {
        var bytes = BuildDocx("the capital plan requests twelve million dollars");

        var text = await _extractor.ExtractAsync(
            bytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "evidence.docx"
        );

        text.Should().Contain("twelve million dollars");
    }

    private static byte[] BuildDocx(string paragraphText)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text(paragraphText)))));
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }
}
