using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using OpenAI;

namespace AGTec.Ai.OpenAI;

/// <summary>
/// Factories that build an OpenAI or Azure OpenAI chat model as the standard <see cref="IChatClient"/>
/// <em>without</em> registering it in DI. Use these when the app constructs clients itself — e.g. a resolver
/// that selects a different model per use case. The <see cref="AGTecOpenAiChatExtensions"/> DI helpers delegate
/// to these, so the construction (and the managed-identity default for Azure) lives in one place.
/// </summary>
public static class AGTecOpenAiChatClient
{
    /// <summary>
    /// Builds an OpenAI (api.openai.com or compatible) chat client. <paramref name="endpoint"/> targets an
    /// OpenAI-compatible gateway (e.g. a local model such as Ollama); omit it for api.openai.com.
    /// </summary>
    public static IChatClient CreateOpenAi(string model, string apiKey, Uri? endpoint = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        var options = endpoint is null ? null : new OpenAIClientOptions { Endpoint = endpoint };
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        return client.GetChatClient(model).AsIChatClient();
    }

    /// <summary>
    /// Builds an Azure OpenAI chat client for a deployment. When <paramref name="apiKey"/> is <c>null</c>,
    /// authentication uses <see cref="DefaultAzureCredential"/> (managed identity in Azure, developer
    /// credentials locally).
    /// </summary>
    public static IChatClient CreateAzureOpenAi(Uri endpoint, string deployment, string? apiKey = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(deployment);

        var client = apiKey is null
            ? new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
            : new AzureOpenAIClient(endpoint, new ApiKeyCredential(apiKey));
        return client.GetChatClient(deployment).AsIChatClient();
    }
}
