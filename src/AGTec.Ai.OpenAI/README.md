# AGTec.Ai.OpenAI

Binds a real OpenAI or Azure OpenAI chat model behind the standard
[Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/) `IChatClient`. Drop-in replacement for
AGTec.Ai's local `StubChatClient` — selected by configuration, no recompile to switch providers.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.Ai.OpenAI
```

## What it provides

- `AddOpenAiChatClient(model, apiKey, endpoint?)` — registers an OpenAI (api.openai.com or an
  OpenAI-compatible gateway via `endpoint`) chat model as the `IChatClient`.
- `AddAzureOpenAiChatClient(endpoint, deployment, apiKey?)` — registers an Azure OpenAI deployment as the
  `IChatClient`. When `apiKey` is omitted, authentication uses `DefaultAzureCredential` (managed identity
  in AKS, developer credentials locally).

Both helpers live on `AGTecOpenAiChatExtensions` and register `IChatClient` as a singleton. They delegate to
`AGTecOpenAiChatClient.CreateOpenAi(...)` / `CreateAzureOpenAi(...)` — call those directly when you build clients
yourself rather than registering one, e.g. a resolver that picks a different model per use case:

```csharp
IChatClient client = AGTecOpenAiChatClient.CreateAzureOpenAi(endpoint, deployment /*, apiKey */);
```

## Usage

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using AGTec.Ai.OpenAI;

// OpenAI:
services.AddOpenAiChatClient(model: "gpt-4o-mini", apiKey: config["OpenAI:ApiKey"]!);

// Azure OpenAI with managed identity (no key):
services.AddAzureOpenAiChatClient(
    endpoint: new Uri("https://my-resource.openai.azure.com"),
    deployment: "gpt-4o-mini");

// consumers depend only on the standard IChatClient:
public sealed class Assistant(IChatClient chat)
{
    public async Task<string> AskAsync(string prompt, CancellationToken ct)
    {
        var response = await chat.GetResponseAsync(prompt, cancellationToken: ct);
        return response.Text;
    }
}
```

## Configuration

No bound options object; pass values to the helper directly. Supply secrets through your configuration
pipeline (user secrets, environment variables, key vault):

- `model` / `deployment` — e.g. `gpt-4o-mini`.
- `apiKey` — required for OpenAI; optional for Azure (falls back to `DefaultAzureCredential`).
- `endpoint` — the Azure resource endpoint, or an optional base URL for OpenAI-compatible gateways.

## See also

`AGTec.Ai` (local defaults + the `IEmbeddingGenerator`/`IChatClient` surface), `AGTec.VectorData`,
`AGTec.Content` — the other RAG building blocks.
