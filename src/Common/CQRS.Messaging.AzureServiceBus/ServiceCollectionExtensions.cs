using System;
using AGTec.Common.CQRS.Dispatchers;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Common.CQRS.Messaging.AzureServiceBus;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCQRSWithMessaging(this IServiceCollection services,
        string connectionString)
    {
        services.AddCQRS();
        services.AddTransient<IEventDispatcher, EventDispatcher>();
        services.AddMessaging(connectionString);
        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services,
        string connectionString)
    {
        services.AddTransient<IMessageProcessor, MessageProcessor>();

        // Adds AzureServiceBus as MessageBroker.
        services.AddAzureServiceBusMessaging(connectionString);

        return services;
    }

    private static IServiceCollection AddAzureServiceBusMessaging(this IServiceCollection services,
        string connectionString)
    {
        var azureMessageBusConfiguration = new AzureMessageBusConfiguration { ConnectionString = connectionString };

        if (azureMessageBusConfiguration.IsValid() == false)
            throw new Exception($"Invalid AzureServiceBus configuration.");

        services.AddSingleton<IMessageBusConfiguration>(azureMessageBusConfiguration);
        services.AddTransient<IAzureMessageFilterFactory, AzureMessageFilterFactory>();
        services.AddTransient<IMessagePublisher, AzureMessagePublisher>();
        services.AddTransient<IMessageHandler, AzureMessageHandler>();
        services.AddTransient<IAzureServiceBusProvisioner, AzureServiceBusProvisioner>();

        return services;
    }
}