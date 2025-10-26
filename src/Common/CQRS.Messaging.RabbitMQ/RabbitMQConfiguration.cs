namespace AGTec.Common.CQRS.Messaging.RabbitMQ;

public class RabbitMQConfiguration : IMessageBusConfiguration
{
    public string ConnectionString { get; init; }

    public bool IsValid()
    {
        return string.IsNullOrWhiteSpace(ConnectionString) == false;
    }
}
