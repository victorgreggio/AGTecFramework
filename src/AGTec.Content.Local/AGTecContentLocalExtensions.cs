using Microsoft.Extensions.DependencyInjection;
using AGTec.Content;

namespace AGTec.Content.Local;

/// <summary>Registration helpers for the local text-extraction adapter.</summary>
public static class AGTecContentLocalExtensions
{
    /// <summary>
    /// Registers <see cref="LocalTextExtractor"/> (plain text, PDF, Word) as the <see cref="ITextExtractor"/>.
    /// </summary>
    public static IServiceCollection AddLocalTextExtractor(this IServiceCollection services)
    {
        services.AddSingleton<ITextExtractor, LocalTextExtractor>();
        return services;
    }
}
