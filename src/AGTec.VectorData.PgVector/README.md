# AGTec.VectorData.PgVector

A [pgvector](https://github.com/pgvector/pgvector)-backed `IVectorStore` for `AGTec.VectorData`. Drop-in
replacement for the in-memory default — same partition-scoped port, now durable in Postgres.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.VectorData.PgVector
```

Requires a PostgreSQL database with the `vector` extension available. The store creates the extension,
table (`agtec_vectors`), and partition index on first use.

## What it provides

- `PgVectorStore` — the `IVectorStore` implementation. Rows carry a `partition` and every query filters
  on it, preserving per-tenant isolation.
- DI helper (`AGTecPgVectorExtensions`): `AddPgVectorStore(connectionString, dimensions)` — registers an
  `NpgsqlDataSource` and the store as `IVectorStore`.

## Usage

```csharp
using Microsoft.Extensions.DependencyInjection;
using AGTec.Ai;              // DeterministicEmbeddingGenerator.DefaultDimensions
using AGTec.VectorData;
using AGTec.VectorData.PgVector;

services.AddPgVectorStore(
    connectionString: config.GetConnectionString("Postgres")!,
    dimensions: DeterministicEmbeddingGenerator.DefaultDimensions);   // IVectorStore

// consumers depend only on IVectorStore — identical to the in-memory store:
public sealed class Index(IVectorStore store)
{
    public Task SaveAsync(string tenant, VectorRecord record, CancellationToken ct) =>
        store.UpsertAsync(tenant, [record], ct);
}
```

## Configuration

- `connectionString` — a standard Npgsql connection string.
- `dimensions` — **must match the embedding generator** (e.g.
  `DeterministicEmbeddingGenerator.DefaultDimensions`, 1536). It is baked into the `embedding vector(N)`
  column when the table is created.

Embeddings are written as `::vector`-cast string literals, so no extra Npgsql type-mapping package is
required. Search uses the cosine-distance operator (`<=>`) and returns `1 - distance` as the score.

## See also

`AGTec.VectorData` (the `IVectorStore` port + in-memory default), `AGTec.Ai` (embeddings) — the other RAG
building blocks.
