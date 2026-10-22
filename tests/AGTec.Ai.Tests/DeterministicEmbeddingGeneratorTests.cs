using AwesomeAssertions;
using Microsoft.Extensions.AI;
using AGTec.Ai;

namespace AGTec.Ai.Tests;

public class DeterministicEmbeddingGeneratorTests
{
    private readonly DeterministicEmbeddingGenerator _generator = new();

    [Fact]
    public async Task Embeddings_are_deterministic_and_l2_normalised()
    {
        var first = await EmbedAsync("regulatory filing deadline");
        var second = await EmbedAsync("regulatory filing deadline");

        first.Should().Equal(second); // same input → identical vector (stable across calls)
        first.Length.Should().Be(DeterministicEmbeddingGenerator.DefaultDimensions);
        Magnitude(first).Should().BeApproximately(1.0, 1e-4);
    }

    [Fact]
    public async Task Similar_text_scores_higher_than_unrelated_text()
    {
        var query = await EmbedAsync("regulatory filing deadline");
        var similar = await EmbedAsync("the deadline for the regulatory filing");
        var unrelated = await EmbedAsync("banana smoothie recipe");

        Cosine(query, similar).Should().BeGreaterThan(Cosine(query, unrelated));
    }

    [Fact]
    public async Task Batch_matches_individual_embedding()
    {
        var batch = await _generator.GenerateAsync(["alpha", "beta"]);
        var alpha = await EmbedAsync("alpha");

        batch[0].Vector.ToArray().Should().Equal(alpha);
    }

    private async Task<float[]> EmbedAsync(string text)
    {
        var result = await _generator.GenerateAsync([text]);
        return result[0].Vector.ToArray();
    }

    private static double Magnitude(float[] vector) => Math.Sqrt(vector.Sum(x => (double)x * x));

    private static double Cosine(float[] a, float[] b)
    {
        double dot = 0d;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
        }

        return dot; // both vectors are L2-normalised, so the dot product is the cosine
    }
}
