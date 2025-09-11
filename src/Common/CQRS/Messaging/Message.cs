using System;
using AGTec.Common.Base.ValueObjects;

namespace AGTec.Common.CQRS.Messaging;

public sealed class Message : ValueObject, IMessage
{
    public Message(string id,
        string label,
        string version,
        string type,
        string payload)
    {
        Id = id;
        Label = label;
        Version = version;
        Type = type;
        Payload = payload;
    }

    public string Id { get; }
    public string Label { get; }
    public string Version { get; }
    public string Type { get; }
    public string Payload { get; }
}