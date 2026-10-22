using AGTec.Application;

namespace AGTec.Reference.Application;

/// <summary>
/// Search read store port — a full-text index over forecasts, kept separate from the
/// write model. Implemented by an Elasticsearch adapter in production (an in-memory one
/// for single-process dev/test); the provider is selected by configuration.
/// </summary>
public interface IForecastSearchStore
{
    Task IndexAsync(ForecastDto forecast, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ForecastDto>> SearchAsync(string term, CancellationToken cancellationToken = default);
}

/// <summary>Read: free-text search over forecasts.</summary>
public sealed record SearchForecasts(string Term) : IQuery<IReadOnlyList<ForecastDto>>;

public static class SearchForecastsHandler
{
    public static Task<IReadOnlyList<ForecastDto>> Handle(
        SearchForecasts query,
        IForecastSearchStore search,
        CancellationToken cancellationToken
    ) => search.SearchAsync(query.Term, cancellationToken);
}
