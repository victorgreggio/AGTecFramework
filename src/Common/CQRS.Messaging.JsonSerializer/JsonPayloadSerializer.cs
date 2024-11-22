using Newtonsoft.Json;

namespace AGTec.Common.CQRS.Messaging.JsonSerializer;

public sealed class JsonPayloadSerializer : IPayloadSerializer
{
    public string Serialize(object payload)
    {
        return JsonConvert.SerializeObject(payload, new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        });
    }

    public T Deserialize<T>(string payload)
    {
        return JsonConvert.DeserializeObject<T>(payload);
    }
}