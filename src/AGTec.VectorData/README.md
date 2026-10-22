# AGTec.VectorData

A provider-neutral, partition-scoped vector store (`IVectorStore`) for retrieval, with an in-memory
default. Every operation is scoped to a `partition` (e.g. a tenant/workspace) so one tenant can never
read another's vectors — an isolation property that carries across providers.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.VectorData
# for Postgres/pgvector:
dotnet add package AGTec.VectorData.PgVector
```

## What it provides

- `IVectorStore` — partition-scoped port with three operations: `UpsertAsync`, `SearchAsync`
  (nearest `topK` by cosine similarity, optional metadata `filter`), and `CountAsync`.
- `VectorRecord(Id, Embedding, Content, Metadata?)` — a record to index.
- `VectorMatch(Id, Content, Score, Metadata)` — a search hit with its similarity score (1.0 = identical).
- `InMemoryVectorStore` — single-process cosine-similarity store for dev/test.
- DI helper (`AGTecVectorDataExtensions`): `AddInMemoryVectorStore()`.

## Usage

```csharp
using Microsoft.Extensions.DependencyInjection;
using AGTec.VectorData;

services.AddInMemoryVectorStore();   // IVectorStore

public sealed class Index(IVectorStore store)
{
    public Task SaveAsync(string tenant, string id, ReadOnlyMemory<float> embedding, string content, CancellationToken ct) =>
        store.UpsertAsync(tenant, [new VectorRecord(id, embedding, content)], ct);

    public Task<IReadOnlyList<VectorMatch>> SearchAsync(string tenant, ReadOnlyMemory<float> query, CancellationToken ct) =>
        store.SearchAsync(tenant, query, topK: 5, cancellationToken: ct);
}
```

## Swapping in pgvector

`IVectorStore` is the provider-neutral port. Register a backing adapter — `AGTec.VectorData.PgVector`'s
`AddPgVectorStore(connectionString, dimensions)` (Postgres + pgvector), or Azure AI Search — in place of
`AddInMemoryVectorStore()`, with no code change in consumers. The partition isolation holds across every
provider.

## See also

`AGTec.VectorData.PgVector` (Postgres adapter), `AGTec.Ai` (embeddings), `AGTec.Content` (ingestion) — the
other RAG building blocks.
