using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Common.CQRS.Messaging.ProtoBufSerializer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProtoBufMessagingSerializer(this IServiceCollection services)
    {
        services.AddTransient<IMessageSerializer, ProtoBufMessageSerializer>();
        services.AddTransient<IPayloadSerializer, ProtoBufPayloadSerializer>();
        return services;
    }
}