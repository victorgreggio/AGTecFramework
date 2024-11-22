using System.IO;
using ProtoBuf;

namespace AGTec.Common.CQRS.Messaging.ProtoBufSerializer;

public class ProtoBufMessageSerializer : IMessageSerializer
{
    public string ContentType => "application/x-protobuf";

    public byte[] Serialize(IMessage message)
    {
        var memoryStream = new MemoryStream();
        Serializer.Serialize(memoryStream, message);

        return memoryStream.ToArray();
    }

    public IMessage Deserialize(byte[] messageBody)
    {
        var memoryStream = new MemoryStream(messageBody);
        return Serializer.Deserialize<Message>(memoryStream);
    }
}