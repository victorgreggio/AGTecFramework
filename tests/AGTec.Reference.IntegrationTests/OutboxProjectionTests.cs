using AwesomeAssertions;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AGTec.Reference.Application;
using AGTec.Reference.Infrastructure;
using AGTec.Reference.Worker;
using Testcontainers.PostgreSql;
using Wolverine;
using Wolverine.Tracking;

namespace AGTec.Reference.IntegrationTests;

/// <summary>
/// Proves the core thesis end to end against a real PostgreSQL: registering a forecast
/// stores the write record AND — through the Wolverine/Marten transactional outbox —
/// delivers the integration event to the projector, which builds the read model. The
/// write and the event are committed in one transaction, so the event cannot be lost.
/// </summary>
public sealed class OutboxProjectionTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private IHost _host = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:forecasts"] = _postgres.GetConnectionString();
        builder.Configuration["AGTec:Messaging:Provider"] = "Local"; // explicit: no broker in this test
        builder.Configuration["AGTec:Search:Provider"] = "InMemory"; // explicit: no Elasticsearch in this test

        // "Local" provider → a durable, Postgres-backed local
        // queue: still the outbox/inbox, just in-process and deterministic.
        builder.AddReferencePlatform(
            MessagingRole.Publisher | MessagingRole.Listener,
            // Discover the real projector handler from the Worker assembly.
            opts => opts.Discovery.IncludeAssembly(typeof(ForecastProjectionHandler).Assembly)
        );

        _host = builder.Build();
        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Registering_a_forecast_persists_the_write_and_projects_the_read_model()
    {
        // Act: send the command and wait until it AND its cascaded integration event are done.
        await _host
            .TrackActivity()
            .Timeout(TimeSpan.FromSeconds(30))
            .SendMessageAndWaitAsync(new RegisterForecast(new DateOnly(2026, 10, 20), 21, "Mild and clear"));

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.QuerySession();

        // Write side: the aggregate record was stored.
        var records = await session.Query<ForecastRecord>().ToListAsync();
        records.Should().ContainSingle();
        records[0].Summary.Should().Be("Mild and clear");

        // Read side: the projector built the read model from the outbox-delivered event.
        var summaries = await session.Query<ForecastSummary>().ToListAsync();
        summaries.Should().ContainSingle();
        summaries[0].Fahrenheit.Should().Be(69);

        // Same aggregate id flows through write → event → read model.
        summaries[0].Id.Should().Be(records[0].Id);

        // Search read model: the projector also indexed the forecast (in-memory provider here).
        var search = _host.Services.GetRequiredService<IForecastSearchStore>();
        var hits = await search.SearchAsync("Mild");
        hits.Should().ContainSingle().Which.Id.Should().Be(records[0].Id);
    }
}
