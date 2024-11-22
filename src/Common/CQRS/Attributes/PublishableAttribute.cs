using System;
using AGTec.Common.CQRS.Messaging;

namespace AGTec.Common.CQRS.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class PublishableAttribute : Attribute
{
    public PublishableAttribute(string label, string version, string destName, PublishType type = PublishType.Queue)
    {
        Label = label;
        Version = version;
        DestName = destName;
        Type = type;
    }

    public string Label { get; }
    public string Version { get; }
    public string DestName { get; }
    public PublishType Type { get; }
}