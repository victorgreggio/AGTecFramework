using System.Collections.Concurrent;
using System.Globalization;
using AGTec.Reference.Application;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace AGTec.Reference.Infrastructure;

/// <summary>Document shape stored in the search index.</summary>
public sealed class ForecastSearchDocument
{
    public Guid Id { get; set; }
    public string Date { get; set; } = string.Empty;
    public int Celsius { get; set; }
    public int Fahrenheit { get; set; }
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// In-memory search store — the default for single-process dev/test. Not shared across
/// processes, so production uses the Elasticsearch adapter instead (selected by config).
/// </summary>
public sealed class InMemoryForecastSearchStore : IForecastSearchStore
{
    private readonly ConcurrentDictionary<Guid, ForecastDto> _docs = new();

    public Task IndexAsync(ForecastDto forecast, CancellationToken cancellationToken = default)
    {
        _docs[forecast.Id] = forecast;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ForecastDto>> SearchAsync(string term, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ForecastDto> results = _docs
            .Values.Where(d => d.Summary.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult(results);
    }
}

/// <summary>Elasticsearch / OpenSearch adapter for the search read model.</summary>
public sealed class ElasticsearchForecastSearchStore(ElasticsearchClient client) : IForecastSearchStore
{
    public const string IndexName = "forecasts";

    public async Task IndexAsync(ForecastDto forecast, CancellationToken cancellationToken = default)
    {
        var document = new ForecastSearchDocument
        {
            Id = forecast.Id,
            Date = forecast.Date.ToString("O", CultureInfo.InvariantCulture),
            Celsius = forecast.Celsius,
            Fahrenheit = forecast.Fahrenheit,
            Summary = forecast.Summary,
        };

        // Refresh so the document is immediately searchable (dev/test behavior).
        var response = await client.IndexAsync(
            document,
            request => request.Index(IndexName).Id(forecast.Id.ToString()).Refresh(Refresh.True),
            cancellationToken
        );

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException($"Failed to index forecast {forecast.Id}: {response.DebugInformation}");
        }
    }

    public async Task<IReadOnlyList<ForecastDto>> SearchAsync(
        string term,
        CancellationToken cancellationToken = default
    )
    {
        var response = await client.SearchAsync<ForecastSearchDocument>(
            search => search.Indices(IndexName).Query(q => q.Match(m => m.Field(f => f.Summary).Query(term))),
            cancellationToken
        );

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException($"Search failed: {response.DebugInformation}");
        }

        return response
            .Documents.Select(d => new ForecastDto(
                d.Id,
                DateOnly.Parse(d.Date, CultureInfo.InvariantCulture),
                d.Celsius,
                d.Fahrenheit,
                d.Summary
            ))
            .ToList();
    }

    /// <summary>Builds a client from a connection string, supporting optional basic auth in the URI.</summary>
    public static ElasticsearchClient CreateClient(string connectionString)
    {
        var uri = new Uri(connectionString);
        var settings = new ElasticsearchClientSettings(new Uri($"{uri.Scheme}://{uri.Host}:{uri.Port}")).DefaultIndex(
            IndexName
        );

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':', 2);
            settings = settings.Authentication(
                new BasicAuthentication(parts[0], parts.Length > 1 ? parts[1] : string.Empty)
            );
        }

        return new ElasticsearchClient(settings);
    }
}
