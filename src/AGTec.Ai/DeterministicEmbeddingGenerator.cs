using Microsoft.Extensions.AI;

namespace AGTec.Ai;

/// <summary>
/// A deterministic, dependency-free embedding generator (signed feature hashing / bag-of-words),
/// implementing the standard <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>. Cosine
/// similarity reflects token overlap — enough to demonstrate retrieval end to end locally with
/// no model or cloud dependency. An OpenAI/Azure generator replaces it behind the same abstraction,
/// selected by configuration. It is stable across processes (unlike <c>string.GetHashCode</c>).
/// </summary>
public sealed class DeterministicEmbeddingGenerator(int dimensions = DeterministicEmbeddingGenerator.DefaultDimensions)
    : IEmbeddingGenerator<string, Embedding<float>>
{
    public const int DefaultDimensions = 1536;

    public int Dimensions { get; } = dimensions;

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var embeddings = new GeneratedEmbeddings<Embedding<float>>(
            values.Select(value => new Embedding<float>(Embed(value)))
        );
        return Task.FromResult(embeddings);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() { }

    private float[] Embed(string text)
    {
        var vector = new float[Dimensions];
        foreach (var token in Tokenize(text))
        {
            var hash = Fnv1A(token);
            var index = (int)(hash % (uint)Dimensions);
            var sign = (hash & 0x8000_0000) == 0 ? 1f : -1f;
            vector[index] += sign;
        }

        // L2-normalise so cosine similarity is a clean dot product.
        var norm = MathF.Sqrt(vector.Sum(v => v * v));
        if (norm > 0f)
        {
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] /= norm;
            }
        }

        return vector;
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var start = -1;
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsLetterOrDigit(text[i]))
            {
                start = start < 0 ? i : start;
            }
            else if (start >= 0)
            {
                yield return text[start..i].ToLowerInvariant();
                start = -1;
            }
        }

        if (start >= 0)
        {
            yield return text[start..].ToLowerInvariant();
        }
    }

    private static uint Fnv1A(string token)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        var hash = offset;
        foreach (var c in token)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash;
    }
}
