using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading.Tasks;

namespace AGTec.Common.CQRS.Messaging.RabbitMQ;

public sealed class RabbitMQMessagePublisher : IMessagePublisher
{
    private readonly IMessageBusConfiguration _configuration;
    private readonly ILogger<RabbitMQMessagePublisher> _logger;
    private readonly IMessageSerializer _serializer;
    private readonly IRabbitMQProvisioner _provisioner;

    public RabbitMQMessagePublisher(IMessageBusConfiguration configuration,
        IMessageSerializer serializer,
        IRabbitMQProvisioner provisioner,
        ILogger<RabbitMQMessagePublisher> logger)
    {
        _configuration = configuration;
        _serializer = serializer;
        _provisioner = provisioner;
        _logger = logger;
    }

    public async Task Publish(string destName, PublishType type, IMessage message)
    {
        _logger.LogInformation($"Publishing message to {type}: {destName}. Message Type: {message.Type}, Label: {message.Label}");
        
        if (type == PublishType.Queue)
        {
            await _provisioner.EnsureQueueExistsAsync(destName);
        }
        else if (type == PublishType.Topic)
        {
            await _provisioner.EnsureExchangeExistsAsync(destName);
        }

        var messagePayload = _serializer.Serialize(message);
        _logger.LogInformation($"Serialized message payload size: {messagePayload.Length} bytes. ContentType: {_serializer.ContentType}");

        var factory = new ConnectionFactory { Uri = new Uri(_configuration.ConnectionString) };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = _serializer.ContentType;
        properties.CorrelationId = message.Id.ToString();
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Type = message.Label;

        string exchangeName = type == PublishType.Topic ? destName : string.Empty;
        string routingKey = type == PublishType.Queue ? destName : message.Label ?? string.Empty;

        _logger.LogInformation($"Sending message {properties.MessageId} with CorrelationId: {message.Id}, Type: {message.Label}");
        
        channel.BasicPublish(
            exchange: exchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: messagePayload);

        _logger.LogInformation($"Message {properties.MessageId} published successfully to {destName}.");
    }
}
