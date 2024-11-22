namespace AGTec.Common.CQRS.Messaging;

public class MessageFilter : IMessageFilter
{
    public MessageFilter(MessageFilterType type, string name, string expression)
    {
        Type = type;
        Name = name;
        Expression = expression;
    }

    public MessageFilterType Type { get; }
    public string Name { get; }
    public string Expression { get; }

    public bool IsValid()
    {
        return string.IsNullOrWhiteSpace(Name) == false
               && string.IsNullOrWhiteSpace(Expression) == false;
    }
}