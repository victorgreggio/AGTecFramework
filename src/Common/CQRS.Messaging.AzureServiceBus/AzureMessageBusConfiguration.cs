namespace AGTec.Common.CQRS.Messaging.AzureServiceBus;

public class AzureMessageBusConfiguration : IMessageBusConfiguration
{
    public string ConnectionString { get; init; }

    public bool IsValid()
    {
        return string.IsNullOrWhiteSpace(ConnectionString) == false;
    }
}