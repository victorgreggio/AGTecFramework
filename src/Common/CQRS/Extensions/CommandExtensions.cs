using System;
using System.Linq;
using System.Reflection;
using AGTec.Common.Base.Extensions;
using AGTec.Common.CQRS.Attributes;
using AGTec.Common.CQRS.Commands;

namespace AGTec.Common.CQRS.Extensions;

public static class CommandExtensions
{
    public static bool IsPublishable(this ICommand command)
    {
        var commandType = command.GetType();

        var publishableAttributes = commandType.GetCustomAttributes()
            .Where(attr => attr.GetType() == typeof(PublishableAttribute));

        return publishableAttributes.Any();
    }

    public static PublishableAttribute GetPublishableAttribute(this ICommand command)
    {
        var commandType = command.GetType();

        var publishableAttributes = commandType.GetCustomAttributes()
            .Where(attr => attr.GetType() == typeof(PublishableAttribute)).ToArray();

        if (publishableAttributes.Any() && publishableAttributes.HasOnlyOneElement())
            return publishableAttributes.FirstOrDefault() as PublishableAttribute;

        throw new Exception($"There is none or more than one PublishableCommandAttribute for {command.GetType()}");
    }
}