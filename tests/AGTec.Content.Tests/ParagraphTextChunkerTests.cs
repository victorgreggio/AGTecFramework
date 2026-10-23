using AGTec.Content;
using AwesomeAssertions;

namespace AGTec.Content.Tests;

public class ParagraphTextChunkerTests
{
    [Fact]
    public void Returns_empty_for_blank_text()
    {
        var chunker = new ParagraphTextChunker();

        chunker.Chunk("   ").Should().BeEmpty();
    }

    [Fact]
    public void Keeps_small_text_in_a_single_chunk()
    {
        var chunker = new ParagraphTextChunker();

        var chunks = chunker.Chunk("First paragraph.\n\nSecond paragraph.");

        chunks.Should().HaveCount(1);
        chunks[0].Index.Should().Be(0);
        chunks[0].Content.Should().Contain("First paragraph.").And.Contain("Second paragraph.");
    }

    [Fact]
    public void Splits_when_the_character_budget_is_exceeded()
    {
        var chunker = new ParagraphTextChunker(new TextChunkerOptions { TargetCharacters = 20 });
        var text = string.Join("\n\n", "Paragraph one here.", "Paragraph two here.", "Paragraph three here.");

        var chunks = chunker.Chunk(text);

        chunks.Should().HaveCountGreaterThan(1);
        chunks.Select(c => c.Index).Should().BeInAscendingOrder().And.StartWith(0);
    }
}
