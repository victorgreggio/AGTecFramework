namespace AGTec.Common.CQRS.Messaging.JsonSerializer;

public sealed class JsonPayloadSerializer : IPayloadSerializer
{
    public string Serialize(object payload) => System.Text.Json.JsonSerializer.Serialize(payload);
    public T Deserialize<T>(string payload) => System.Text.Json.JsonSerializer.Deserialize<T>(payload);    
}