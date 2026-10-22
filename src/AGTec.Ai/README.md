# AGTec.Ai

Local, no-model defaults for AI building blocks, exposed through the **standard
[Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/) abstractions** — a deterministic
`IEmbeddingGenerator<string, Embedding<float>>` and a stub `IChatClient`. Consumers code against the
standard interfaces; a cloud provider swaps in by configuration.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.Ai
```

## What it provides

- `DeterministicEmbeddingGenerator` — a dependency-free `IEmbeddingGenerator<string, Embedding<float>>`
  (signed feature hashing + L2 normalisation). Cosine similarity reflects token overlap, so retrieval
  runs end to end locally with no model or cloud. Stable across processes. Default `Dimensions` = 1536.
- `StubChatClient` — a no-model `IChatClient` that echoes the last user message with a `[stub]` marker,
  so agent/chat code runs end to end with no model or cloud.
- DI helpers (`AGTecAiExtensions`): `AddDeterministicEmbeddings(dimensions = 1536)` registers the
  `IEmbeddingGenerator`; `AddStubChatClient()` registers the `IChatClient`.
- **Chat-client telemetry** (`AGTecAiTelemetryExtensions`): OpenTelemetry for any `IChatClient` — spans
  carry the model, token usage, latency, and finish reason; prompt/response **content** is recorded only
  when opted in. `AGTec.ServiceDefaults` registers the `AGTec.Ai` trace source, so spans are captured with
  no extra wiring.

## Chat-client telemetry

```csharp
services.AddAGTecAiTelemetry(configuration);   // binds AGTecAiTelemetryOptions from AGTec:Ai:CaptureContent

// Either decorate the registered client…
services.AddStubChatClient();
services.AddChatClientTelemetry();

// …or instrument a client you build yourself (e.g. a per-intent resolver):
IChatClient Resolve(AGTecAiTelemetryOptions telemetry) =>
    BuildClient().WithTelemetry(telemetry /*, sourceName: "MyApp.Agents" */);
```

`AGTecAiTelemetryOptions.CaptureContent` (config key **`AGTec:Ai:CaptureContent`**) governs whether the
prompt/response message content is written to spans. It **defaults to `false`** because that content can be
sensitive (user input, retrieved documents, generated text) — turn it on locally to inspect full reasoning
in a trace viewer, and keep it off when deployed. Model/token/latency spans are emitted regardless.

## Usage

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using AGTec.Ai;

services.AddDeterministicEmbeddings();   // IEmbeddingGenerator<string, Embedding<float>>
services.AddStubChatClient();            // IChatClient

// consumers depend only on the standard Microsoft.Extensions.AI interfaces:
public sealed class Retriever(IEmbeddingGenerator<string, Embedding<float>> embedder)
{
    public async Task<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken ct)
    {
        var result = await embedder.GenerateAsync([text], cancellationToken: ct);
        return result[0].Vector;
    }
}
```

## Swapping in OpenAI / Azure embeddings and chat

Because the surface is the standard Microsoft.Extensions.AI abstractions, a cloud provider replaces the
local defaults with **no change in consumers** — register it in configuration instead of the stubs. For
chat, `AGTec.Ai.OpenAI` provides `AddOpenAiChatClient(...)` / `AddAzureOpenAiChatClient(...)` that bind a
real model behind the same `IChatClient`. Register a cloud `IEmbeddingGenerator<string, Embedding<float>>`
the same way to replace `DeterministicEmbeddingGenerator`.

## See also

`AGTec.Ai.OpenAI` (real chat models), `AGTec.VectorData` (vector store), `AGTec.Content` (ingestion) — the
other RAG building blocks.
