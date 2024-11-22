namespace AGTec.Common.CQRS.Messaging;

public interface IMessageFilter
{
    MessageFilterType Type { get; }
    string Name { get; }
    string Expression { get; }

    bool IsValid();
}