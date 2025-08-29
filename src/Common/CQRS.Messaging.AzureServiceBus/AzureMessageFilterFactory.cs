using AGTec.Common.CQRS.Messaging.Exceptions;
using Azure.Messaging.ServiceBus.Administration;

namespace AGTec.Common.CQRS.Messaging.AzureServiceBus;

internal class AzureMessageFilterFactory : IAzureMessageFilterFactory
{
    public CreateRuleOptions Create(IMessageFilter filter)
    {
        switch (filter.Type)
        {
            case MessageFilterType.CorrelationIdFilter:
                return new CreateRuleOptions
                {
                    Name = filter.Name,
                    Filter = new CorrelationRuleFilter { CorrelationId = filter.Expression }
                };

            case MessageFilterType.LabelFilter:
                return new CreateRuleOptions
                {
                    Name = filter.Name,
                    Filter = new CorrelationRuleFilter { Subject = filter.Expression }
                };

            case MessageFilterType.QueryFilter:
                return new CreateRuleOptions
                {
                    Name = filter.Name,
                    Filter = new SqlRuleFilter(filter.Expression)
                };

            default:
                throw new InvalidMessageFilterException(
                    $"Filter {filter.Type} is invalid for {GetType().AssemblyQualifiedName} implementation.");
        }
    }
}