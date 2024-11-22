using System;

namespace AGTec.Common.CQRS.Messaging;

public interface IMessage
{
    Guid Id { get; }
    string Label { get; }
    string Version { get; }
    string Type { get; }
    string Payload { get; }
}