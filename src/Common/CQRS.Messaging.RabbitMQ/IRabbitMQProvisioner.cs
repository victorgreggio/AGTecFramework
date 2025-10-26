using System.Threading.Tasks;

namespace AGTec.Common.CQRS.Messaging.RabbitMQ;

public interface IRabbitMQProvisioner
{
    Task EnsureQueueExistsAsync(string queueName);
    Task EnsureExchangeExistsAsync(string exchangeName);
    Task EnsureBindingExistsAsync(string exchangeName, string queueName, string routingKey);
}
