using AwesomeAssertions;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AGTec.Reference.Application;
using AGTec.Reference.Infrastructure;
using AGTec.Reference.Worker;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Wolverine;

namespace AGTec.Reference.IntegrationTests;

/// <summary>
/// Proves the §8 portability claim: with <c>AGTec:Messaging:Provider = RabbitMq</c> — a pure
/// configuration choice, no code change from the "Local" test — the integration event
/// round-trips through a real RabbitMQ broker (exchange → bound queue) to the projector,
/// which builds the read model. Swapping to Azure Service Bus is the same config key.
/// </summary>
public sealed class RabbitMqTransportTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder("rabbitmq:4.0").Build();

    private IHost _host = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbit.StartAsync());

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:forecasts"] = _postgres.GetConnectionString();
        builder.Configuration["ConnectionStrings:messaging"] = _rabbit.GetConnectionString();
        builder.Configuration["AGTec:Messaging:Provider"] = "RabbitMq"; // <-- the only switch

        builder.AddReferencePlatform(
            MessagingRole.Publisher | MessagingRole.Listener,
            opts => opts.Discovery.IncludeAssembly(typeof(ForecastProjectionHandler).Assembly)
        );

        _host = builder.Build();
        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _rabbit.DisposeAsync().AsTask());
    }

    [Fact]
    public async Task Event_round_trips_through_rabbitmq_and_projects_the_read_model()
    {
        var bus = _host.Services.GetRequiredService<IMessageBus>();

        var id = await bus.InvokeAsync<Guid>(new RegisterForecast(new DateOnly(2026, 10, 20), 21, "Mild and clear"));

        var store = _host.Services.GetRequiredService<IDocumentStore>();
        var summary = await WaitForSummaryAsync(store, id, TimeSpan.FromSeconds(30));

        summary.Should().NotBeNull("the event should be delivered via RabbitMQ and projected");
        summary!.Fahrenheit.Should().Be(69);
    }

    private static async Task<ForecastSummary?> WaitForSummaryAsync(IDocumentStore store, Guid id, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            await using var session = store.QuerySession();
            var summary = await session.LoadAsync<ForecastSummary>(id);
            if (summary is not null)
            {
                return summary;
            }

            await Task.Delay(250);
        }

        return null;
    }
}
