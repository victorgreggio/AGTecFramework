using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Ai;

/// <summary>Registration helpers for AGTec's local-default AI building blocks.</summary>
public static class AGTecAiExtensions
{
    /// <summary>
    /// Registers the no-model deterministic embedding generator as the standard
    /// <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>. A cloud generator (OpenAI/Azure)
    /// registered afterwards — selected by configuration — replaces it behind the same abstraction.
    /// </summary>
    public static IServiceCollection AddDeterministicEmbeddings(
        this IServiceCollection services,
        int dimensions = DeterministicEmbeddingGenerator.DefaultDimensions
    )
    {
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ => new DeterministicEmbeddingGenerator(
            dimensions
        ));
        return services;
    }

    /// <summary>
    /// Registers the no-model <see cref="StubChatClient"/> as the <see cref="IChatClient"/> — the
    /// local default. An OpenAI/Azure client registered afterwards replaces it behind the abstraction.
    /// </summary>
    public static IServiceCollection AddStubChatClient(this IServiceCollection services) =>
        services.AddSingleton<IChatClient>(new StubChatClient());
}
