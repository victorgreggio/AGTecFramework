using Microsoft.Extensions.Configuration;
using AGTec.Reference.Application;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.RabbitMQ;

namespace AGTec.Reference.Infrastructure;

/// <summary>
/// Which side of the conversation a host plays. A host may be both (e.g. an
/// integration test that publishes and projects in one process).
/// </summary>
[Flags]
public enum MessagingRole
{
    None = 0,
    Publisher = 1,
    Listener = 2,
}

/// <summary>
/// Selects the messaging transport <b>from configuration</b> — the heart of the
/// Azure-first-but-portable stance (§8). The same binary runs on RabbitMQ locally or
/// Azure Service Bus in the cloud; switching is a config change + restart, never a
/// recompile. The routing (one logical "agtec.forecasts" stream) is identical across
/// transports; only the transport primitive differs (exchange/queue vs topic/subscription).
/// </summary>
internal static class ForecastMessaging
{
    private const string Stream = "agtec.forecasts";
    private const string WorkerSubscription = "agtec.forecasts.worker";

    public static void Configure(WolverineOptions options, IConfiguration configuration, MessagingRole roles)
    {
        // AGTec:Messaging:Provider = Local (default) | RabbitMq | AzureServiceBus
        var provider = configuration["AGTec:Messaging:Provider"] ?? "Local";
        var connectionString = configuration.GetConnectionString("messaging");

        switch (provider.ToLowerInvariant())
        {
            case "rabbitmq":
                ConfigureRabbitMq(options, connectionString, roles);
                break;

            case "azureservicebus":
                ConfigureAzureServiceBus(options, connectionString, roles);
                break;

            default:
                // No broker: a durable, Postgres-backed local queue — still the outbox/inbox.
                options.Policies.UseDurableLocalQueues();
                break;
        }
    }

    private static void ConfigureRabbitMq(WolverineOptions options, string? connectionString, MessagingRole roles)
    {
        var uri = new Uri(connectionString ?? "amqp://guest:guest@localhost:5672");
        var transport = options.UseRabbitMq(uri).AutoProvision();

        if (roles.HasFlag(MessagingRole.Publisher))
        {
            options.PublishMessage<ForecastRegisteredIntegrationEvent>().ToRabbitExchange(Stream);
        }

        if (roles.HasFlag(MessagingRole.Listener))
        {
            transport.BindExchange(Stream).ToQueue(WorkerSubscription);
            options.ListenToRabbitQueue(WorkerSubscription);
        }
    }

    private static void ConfigureAzureServiceBus(
        WolverineOptions options,
        string? connectionString,
        MessagingRole roles
    )
    {
        options.UseAzureServiceBus(connectionString!).AutoProvision();

        if (roles.HasFlag(MessagingRole.Publisher))
        {
            options.PublishMessage<ForecastRegisteredIntegrationEvent>().ToAzureServiceBusTopic(Stream);
        }

        if (roles.HasFlag(MessagingRole.Listener))
        {
            options.ListenToAzureServiceBusSubscription(WorkerSubscription).FromTopic(Stream);
        }
    }
}
