using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Ai.OpenAI;

/// <summary>
/// Registration helpers that bind an OpenAI or Azure OpenAI chat model as the standard
/// <see cref="IChatClient"/>. These replace AGTec's local <c>StubChatClient</c> behind the same
/// abstraction, selected by configuration — no recompile to switch providers. They delegate to
/// <see cref="AGTecOpenAiChatClient"/>; use that directly when you build clients yourself (e.g. per use case).
/// </summary>
public static class AGTecOpenAiChatExtensions
{
    /// <summary>
    /// Registers an OpenAI (api.openai.com or compatible) chat model as the <see cref="IChatClient"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="model">The model id (e.g. <c>gpt-4o-mini</c>).</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="endpoint">Optional base endpoint for OpenAI-compatible gateways.</param>
    public static IServiceCollection AddOpenAiChatClient(
        this IServiceCollection services,
        string model,
        string apiKey,
        Uri? endpoint = null
    ) => services.AddSingleton(AGTecOpenAiChatClient.CreateOpenAi(model, apiKey, endpoint));

    /// <summary>
    /// Registers an Azure OpenAI deployment as the <see cref="IChatClient"/>. When
    /// <paramref name="apiKey"/> is omitted, authentication uses <c>DefaultAzureCredential</c>
    /// (managed identity in AKS, developer credentials locally).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="endpoint">The Azure OpenAI resource endpoint.</param>
    /// <param name="deployment">The deployment name.</param>
    /// <param name="apiKey">Optional API key; when null, uses managed identity / DefaultAzureCredential.</param>
    public static IServiceCollection AddAzureOpenAiChatClient(
        this IServiceCollection services,
        Uri endpoint,
        string deployment,
        string? apiKey = null
    ) => services.AddSingleton(AGTecOpenAiChatClient.CreateAzureOpenAi(endpoint, deployment, apiKey));
}
