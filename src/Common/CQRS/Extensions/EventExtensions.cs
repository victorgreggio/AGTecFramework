using System;
using System.Linq;
using System.Reflection;
using AGTec.Common.Base.Extensions;
using AGTec.Common.CQRS.Attributes;
using AGTec.Common.CQRS.Events;

namespace AGTec.Common.CQRS.Extensions;

public static class EventExtensions
{
    public static PublishableAttribute GetPublishableAttributes(this IEvent evt)
    {
        var eventType = evt.GetType();

        var publishableAttributes = eventType.GetCustomAttributes()
            .Where(attr => attr.GetType() == typeof(PublishableAttribute)).ToArray();

        if (publishableAttributes.Any() && publishableAttributes.HasOnlyOneElement())
            return publishableAttributes.FirstOrDefault() as PublishableAttribute;

        throw new Exception($"There is none or more than one PublishableAttribute for {evt.GetType()}");
    }
}