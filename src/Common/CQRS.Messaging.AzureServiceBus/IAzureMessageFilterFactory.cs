using Microsoft.Azure.ServiceBus;

namespace AGTec.Common.CQRS.Messaging.AzureServiceBus;

public interface IAzureMessageFilterFactory
{
    RuleDescription Create(IMessageFilter filter);
}