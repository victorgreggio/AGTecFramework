using System;
using AGTec.Common.CQRS.Dispatchers;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Common.CQRS.Messaging.RabbitMQ;

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

        // Adds RabbitMQ as MessageBroker.
        services.AddRabbitMQMessaging(connectionString);

        return services;
    }

    private static IServiceCollection AddRabbitMQMessaging(this IServiceCollection services,
        string connectionString)
    {
        var rabbitMQConfiguration = new RabbitMQConfiguration { ConnectionString = connectionString };

        if (rabbitMQConfiguration.IsValid() == false)
            throw new Exception($"Invalid RabbitMQ configuration.");

        services.AddSingleton<IMessageBusConfiguration>(rabbitMQConfiguration);
        services.AddTransient<IRabbitMQFilterFactory, RabbitMQFilterFactory>();
        services.AddTransient<IMessagePublisher, RabbitMQMessagePublisher>();
        services.AddTransient<IMessageHandler, RabbitMQMessageHandler>();
        services.AddTransient<IRabbitMQProvisioner, RabbitMQProvisioner>();

        return services;
    }
}
