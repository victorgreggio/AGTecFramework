using Microsoft.Extensions.DependencyInjection;

namespace AGTec.VectorData;

/// <summary>Registration helper for the in-memory vector store (the dev/test default).</summary>
public static class AGTecVectorDataExtensions
{
    public static IServiceCollection AddInMemoryVectorStore(this IServiceCollection services) =>
        services.AddSingleton<IVectorStore, InMemoryVectorStore>();
}
