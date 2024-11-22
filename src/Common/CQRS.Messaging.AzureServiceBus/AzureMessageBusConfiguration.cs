namespace AGTec.Common.CQRS.Messaging.AzureServiceBus;

public class AzureMessageBusConfiguration : IMessageBusConfiguration
{
    public const string ConfigSectionName = "AzureServiceBus";

    public string ConnectionString { get; set; }

    public bool IsValid()
    {
        return string.IsNullOrWhiteSpace(ConnectionString) == false;
    }
}