using System.Threading.Tasks;

namespace AGTec.Common.CQRS.Messaging.AzureServiceBus;

public interface IAzureServiceBusProvisioner
{
    Task EnsureQueueExistsAsync(string queueName);
    Task EnsureTopicExistsAsync(string topicName);
    Task EnsureSubscriptionExistsAsync(string topicName, string subscriptionName);
    Task EnsureDefaultRuleExistsAsync(string topicName, string subscriptionName);
}
