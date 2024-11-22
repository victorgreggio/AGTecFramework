using System;
using System.IO;
using ProtoBuf;

namespace AGTec.Common.CQRS.Messaging.ProtoBufSerializer;

public class ProtoBufPayloadSerializer : IPayloadSerializer
{
    public string Serialize(object payload)
    {
        var memoryStream = new MemoryStream();
        Serializer.Serialize(memoryStream, payload);
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    public T Deserialize<T>(string payload)
    {
        var memoryStream = new MemoryStream(Convert.FromBase64String(payload));
        return Serializer.Deserialize<T>(memoryStream);
    }
}