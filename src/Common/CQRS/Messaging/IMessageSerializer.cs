namespace AGTec.Common.CQRS.Messaging;

public interface IMessageSerializer
{
    string ContentType { get; }
    byte[] Serialize(IMessage message);
    IMessage Deserialize(byte[] messageBody);
}