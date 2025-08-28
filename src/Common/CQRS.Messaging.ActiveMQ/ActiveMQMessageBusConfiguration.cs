namespace AGTec.Common.CQRS.Messaging.ActiveMQ;

public class ActiveMQMessageBusConfiguration : IMessageBusConfiguration
{
    public string ConnectionString { get; set; }

    public bool IsValid()
    {
        return string.IsNullOrWhiteSpace(ConnectionString) == false;
    }
}