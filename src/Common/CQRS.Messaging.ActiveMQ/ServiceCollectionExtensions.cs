using System;
using AGTec.Common.CQRS.Dispatchers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Common.CQRS.Messaging.ActiveMQ;

public static class ServiceCollectionExtensions
{
    public const string ActiveMQConnectionStringName = "ActiveMQ";
    
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
        var activeMQConnectionString = configuration.GetConnectionString(ActiveMQConnectionStringName);

        if (string.IsNullOrEmpty(activeMQConnectionString))
            throw new Exception(
                $"Connection string '{ActiveMQConnectionStringName}' not found.");

        var activeMQMessageBusConfiguration = new ActiveMQMessageBusConfiguration
        {
            ConnectionString = activeMQConnectionString
        };

        if (activeMQMessageBusConfiguration.IsValid() == false)
            throw new Exception($"Invalid ActiveMQ configuration.");

        services.AddSingleton<IMessageBusConfiguration>(activeMQMessageBusConfiguration);
        services.AddTransient<IActiveMQMessageFilterFactory, ActiveMQMessageFilterFactory>();
        services.AddTransient<IMessagePublisher, ActiveMQMessagePublisher>();
        services.AddTransient<IMessageHandler, ActiveMQMessageHandler>();

        return services;
    }
}