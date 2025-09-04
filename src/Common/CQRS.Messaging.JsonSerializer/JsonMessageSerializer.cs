using System.Text;

namespace AGTec.Common.CQRS.Messaging.JsonSerializer;

public class JsonMessageSerializer : IMessageSerializer
{
    public string ContentType => "application/json";
    public byte[] Serialize(IMessage message) => Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message));
    public IMessage Deserialize(byte[] messageBody) => System.Text.Json.JsonSerializer.Deserialize<Message>(Encoding.UTF8.GetString(messageBody));
}