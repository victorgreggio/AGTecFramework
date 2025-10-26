using AGTec.Common.CQRS.Messaging.Exceptions;
using System.Collections.Generic;

namespace AGTec.Common.CQRS.Messaging.RabbitMQ;

internal class RabbitMQFilterFactory : IRabbitMQFilterFactory
{
    public IDictionary<string, object> CreateHeaders(IMessageFilter filter)
    {
        var headers = new Dictionary<string, object>();

        switch (filter.Type)
        {
            case MessageFilterType.CorrelationIdFilter:
                headers["x-match"] = "all";
                headers["correlationId"] = filter.Expression;
                break;

            case MessageFilterType.LabelFilter:
                headers["x-match"] = "all";
                headers["label"] = filter.Expression;
                break;

            case MessageFilterType.QueryFilter:
                throw new InvalidMessageFilterException(
                    $"QueryFilter is not directly supported by RabbitMQ. Use CorrelationIdFilter or LabelFilter instead, or implement custom filtering logic.");

            default:
                throw new InvalidMessageFilterException(
                    $"Filter {filter.Type} is invalid for {GetType().AssemblyQualifiedName} implementation.");
        }

        return headers;
    }
}
