using System;
using AGTec.Common.CQRS.Dispatchers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Common.CQRS.Messaging.AzureServiceBus;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCQRSWithMessaging(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCQRS(configuration);
        services.AddTransient<IEventDispatcher, EventDispatcher>();
        services.AddMessaging(configuration);
        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<IMessageProcessor, MessageProcessor>();

        // Adds AzureServiceBus as MessageBroker.
        services.AddAzureServiceBusMessaging(configuration);

        return services;
    }

    private static IServiceCollection AddAzureServiceBusMessaging(this IServiceCollection services,
        IConfiguration configuration)
    {
        var azureMessageBusConfiguration = configuration.GetSection(AzureMessageBusConfiguration.ConfigSectionName)
            .Get<AzureMessageBusConfiguration>();

        if (azureMessageBusConfiguration.IsValid() == false)
            throw new Exception($"Configuration section '{AzureMessageBusConfiguration.ConfigSectionName}' not found.");

        services.AddSingleton<IMessageBusConfiguration>(azureMessageBusConfiguration);
        services.AddTransient<IAzureMessageFilterFactory, AzureMessageFilterFactory>();
        services.AddTransient<IMessagePublisher, AzureMessagePublisher>();
        services.AddTransient<IMessageHandler, AzureMessageHandler>();

        return services;
    }
}