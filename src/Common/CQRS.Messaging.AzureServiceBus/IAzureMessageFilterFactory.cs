using Azure.Messaging.ServiceBus.Administration;

namespace AGTec.Common.CQRS.Messaging.AzureServiceBus;

public interface IAzureMessageFilterFactory
{
    CreateRuleOptions Create(IMessageFilter filter);
}