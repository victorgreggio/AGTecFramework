using System.Text;
using Newtonsoft.Json;

namespace AGTec.Common.CQRS.Messaging.JsonSerializer;

public class JsonMessageSerializer : IMessageSerializer
{
    public string ContentType => "application/json";

    public byte[] Serialize(IMessage message)
    {
        var payload = JsonConvert.SerializeObject(message, new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        });

        return Encoding.UTF8.GetBytes(payload);
    }

    public IMessage Deserialize(byte[] messageBody)
    {
        var payload = Encoding.UTF8.GetString(messageBody);
        return JsonConvert.DeserializeObject<Message>(payload);
    }
}