using System;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.AMQP;
using Microsoft.Extensions.Logging;

namespace AGTec.Common.CQRS.Messaging.ActiveMQ;

public sealed class ActiveMQMessagePublisher : IMessagePublisher
{
    private readonly IMessageBusConfiguration _configuration;
    private readonly IConnectionFactory _factory;
    private readonly ILogger<ActiveMQMessagePublisher> _logger;
    private readonly IMessageSerializer _serializer;

    public ActiveMQMessagePublisher(IMessageBusConfiguration configuration,
        IMessageSerializer serializer,
        ILogger<ActiveMQMessagePublisher> logger)
    {
        _configuration = configuration;
        _serializer = serializer;
        _factory = new ConnectionFactory(_configuration.ConnectionString);
        _logger = logger;
    }

    public Task Publish(string destName, PublishType type, IMessage message)
    {
        var messagePayload = _serializer.Serialize(message);
        var messageGuid = Guid.NewGuid().ToString();

        using (var connection = _factory.CreateConnection())
        using (var session = connection.CreateSession())
        {
            var destination = type == PublishType.Queue
                ? session.GetQueue(destName) as IDestination
                : session.GetTopic(destName);

            using (var senderClient = session.CreateProducer(destination))
            {
                connection.Start();

                var activeMQMessage = session.CreateBytesMessage(messagePayload);
                activeMQMessage.NMSCorrelationID = message.Id.ToString();
                activeMQMessage.Properties["MessageId"] = messageGuid;
                activeMQMessage.Properties["ContentType"] = _serializer.ContentType;
                activeMQMessage.Properties["Label"] = message.Label;

                senderClient.Send(activeMQMessage);
                connection.Close();
            }
        }

        _logger.LogInformation($"Message {messageGuid} published to {destName}.");

        return Task.CompletedTask;
    }
}