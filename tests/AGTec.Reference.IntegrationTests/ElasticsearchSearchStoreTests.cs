using AwesomeAssertions;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using AGTec.Reference.Application;
using AGTec.Reference.Infrastructure;
using Testcontainers.Elasticsearch;

namespace AGTec.Reference.IntegrationTests;

/// <summary>
/// Proves the Elasticsearch search adapter against a real Elasticsearch node: indexed
/// forecasts are found by free-text search over their summary. The in-memory provider is
/// a drop-in alternative behind the same <see cref="IForecastSearchStore"/> port (config-selected).
/// </summary>
public sealed class ElasticsearchSearchStoreTests : IAsyncLifetime
{
    private readonly ElasticsearchContainer _elasticsearch = new ElasticsearchBuilder(
        "docker.elastic.co/elasticsearch/elasticsearch:8.15.3"
    )
        .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
        .Build();

    private IForecastSearchStore _store = null!;

    public async Task InitializeAsync()
    {
        await _elasticsearch.StartAsync();

        // The secured test node hands back https://elastic:<password>@host:port.
        var uri = new Uri(_elasticsearch.GetConnectionString());
        var credentials = uri.UserInfo.Split(':', 2);

        var settings = new ElasticsearchClientSettings(new Uri($"{uri.Scheme}://{uri.Host}:{uri.Port}"))
            .Authentication(new BasicAuthentication(credentials[0], credentials[1]))
            .ServerCertificateValidationCallback((_, _, _, _) => true) // dev: trust the self-signed cert
            .DefaultIndex(ElasticsearchForecastSearchStore.IndexName);

        _store = new ElasticsearchForecastSearchStore(new ElasticsearchClient(settings));
    }

    public Task DisposeAsync() => _elasticsearch.DisposeAsync().AsTask();

    [Fact]
    public async Task Indexed_forecasts_are_found_by_summary_text()
    {
        var expectedId = Guid.CreateVersion7();
        await _store.IndexAsync(
            new ForecastDto(expectedId, new DateOnly(2026, 10, 20), 21, 69, "Mild and clear skies")
        );
        await _store.IndexAsync(
            new ForecastDto(Guid.CreateVersion7(), new DateOnly(2026, 10, 21), 3, 37, "Freezing fog")
        );

        var results = await _store.SearchAsync("clear");

        results.Should().ContainSingle().Which.Id.Should().Be(expectedId);
    }
}
