using System.Collections.Generic;
using System.Linq;
using AGTec.Common.Base.Extensions;
using AGTec.Common.CQRS.Messaging.Exceptions;

namespace AGTec.Common.CQRS.Messaging.ActiveMQ;

internal class ActiveMQMessageFilterFactory : IActiveMQMessageFilterFactory
{
    public string Create(IEnumerable<IMessageFilter> filters)
    {
        if (filters == null || filters.Any() == false)
            return null;

        var result = string.Empty;
        filters.ForEach(filter =>
        {
            if (string.IsNullOrWhiteSpace(result) == false)
                result += " AND ";

            switch (filter.Type)
            {
                case MessageFilterType.CorrelationIdFilter:
                    result += "NMSCorrelationID = " + filter.Expression;
                    break;

                case MessageFilterType.LabelFilter:
                    result += "Label = " + filter.Expression;
                    break;

                case MessageFilterType.QueryFilter:
                    result += filter.Expression;
                    break;

                default:
                    throw new InvalidMessageFilterException(
                        $"Filter {filter.Type} is invalid for {GetType().AssemblyQualifiedName} implementation.");
            }
        });

        return result;
    }
}