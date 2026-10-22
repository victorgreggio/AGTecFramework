using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Content;

/// <summary>Registration helpers for AGTec's content building blocks.</summary>
public static class AGTecContentExtensions
{
    /// <summary>
    /// Registers the paragraph-aware <see cref="ITextChunker"/>. Replace the registration with a
    /// token-aware implementation when needed.
    /// </summary>
    public static IServiceCollection AddParagraphChunker(
        this IServiceCollection services,
        Action<TextChunkerOptions>? configure = null
    )
    {
        var options = new TextChunkerOptions();
        configure?.Invoke(options);
        services.AddSingleton<ITextChunker>(new ParagraphTextChunker(options));
        return services;
    }

    /// <summary>
    /// Registers the dependency-free <see cref="PlainTextExtractor"/> as the <see cref="ITextExtractor"/>.
    /// For PDF and Word support, use <c>AddLocalTextExtractor()</c> from <c>AGTec.Content.Local</c> instead.
    /// </summary>
    public static IServiceCollection AddPlainTextExtractor(this IServiceCollection services)
    {
        services.AddSingleton<ITextExtractor, PlainTextExtractor>();
        return services;
    }
}
