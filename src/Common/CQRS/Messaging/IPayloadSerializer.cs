namespace AGTec.Common.CQRS.Messaging;

public interface IPayloadSerializer
{
    string Serialize(object payload);
    T Deserialize<T>(string payload);
}