using System;
using AGTec.Common.CQRS.Dispatchers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Common.CQRS.Messaging.ActiveMQ;

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

        // Adds ActiveMQ as MessageBroker.
        services.AddActiveMQMessaging(configuration);

        return services;
    }

    private static IServiceCollection AddActiveMQMessaging(this IServiceCollection services,
        IConfiguration configuration)
    {
        var activeMQMessageBusConfiguration = configuration
            .GetSection(ActiveMQMessageBusConfiguration.ConfigSectionName).Get<ActiveMQMessageBusConfiguration>();

        if (activeMQMessageBusConfiguration.IsValid() == false)
            throw new Exception(
                $"Configuration section '{ActiveMQMessageBusConfiguration.ConfigSectionName}' not found.");

        services.AddSingleton<IMessageBusConfiguration>(activeMQMessageBusConfiguration);
        services.AddTransient<IActiveMQMessageFilterFactory, ActiveMQMessageFilterFactory>();
        services.AddTransient<IMessagePublisher, ActiveMQMessagePublisher>();
        services.AddTransient<IMessageHandler, ActiveMQMessageHandler>();

        return services;
    }
}