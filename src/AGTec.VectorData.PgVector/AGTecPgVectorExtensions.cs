using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AGTec.VectorData.PgVector;

/// <summary>Registration helper for the pgvector-backed vector store.</summary>
public static class AGTecPgVectorExtensions
{
    /// <summary>
    /// Registers <see cref="IVectorStore"/> backed by pgvector. <paramref name="dimensions"/> must
    /// match the embedding generator (e.g. <c>DeterministicEmbeddingGenerator.DefaultDimensions</c>).
    /// </summary>
    public static IServiceCollection AddPgVectorStore(
        this IServiceCollection services,
        string connectionString,
        int dimensions
    )
    {
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<IVectorStore>(provider => new PgVectorStore(
            provider.GetRequiredService<NpgsqlDataSource>(),
            dimensions
        ));
        return services;
    }
}
