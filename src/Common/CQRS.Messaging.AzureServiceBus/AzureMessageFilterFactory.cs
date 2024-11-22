using AGTec.Common.CQRS.Messaging.Exceptions;
using Microsoft.Azure.ServiceBus;

namespace AGTec.Common.CQRS.Messaging.AzureServiceBus;

internal class AzureMessageFilterFactory : IAzureMessageFilterFactory
{
    public RuleDescription Create(IMessageFilter filter)
    {
        switch (filter.Type)
        {
            case MessageFilterType.CorrelationIdFilter:
                return new RuleDescription(filter.Name,
                    new CorrelationFilter(filter.Expression));

            case MessageFilterType.LabelFilter:
                return new RuleDescription(filter.Name,
                    new CorrelationFilter { Label = filter.Expression });

            case MessageFilterType.QueryFilter:
                return new RuleDescription(filter.Name,
                    new SqlFilter(filter.Expression));

            default:
                throw new InvalidMessageFilterException(
                    $"Filter {filter.Type} is invalid for {GetType().AssemblyQualifiedName} implementation.");
        }
    }
}