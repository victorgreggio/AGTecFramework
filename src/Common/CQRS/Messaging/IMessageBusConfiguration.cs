namespace AGTec.Common.CQRS.Messaging;

public interface IMessageBusConfiguration
{
    string ConnectionString { get; }
    bool IsValid();
}