using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AGTec.Common.CQRS.Messaging.AzureServiceBus;

public sealed class AzureMessagePublisher : IMessagePublisher
{
    private readonly IMessageBusConfiguration _configuration;
    private readonly ILogger<AzureMessagePublisher> _logger;
    private readonly IMessageSerializer _serializer;
    private readonly IAzureServiceBusProvisioner _provisioner;

    public AzureMessagePublisher(IMessageBusConfiguration configuration,
        IMessageSerializer serializer,
        IAzureServiceBusProvisioner provisioner,
        ILogger<AzureMessagePublisher> logger)
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
            await _provisioner.EnsureTopicExistsAsync(destName);
        }

        var messagePayload = _serializer.Serialize(message);
        _logger.LogInformation($"Serialized message payload size: {messagePayload.Length} bytes. ContentType: {_serializer.ContentType}");

        ServiceBusClient client;
        if (ServiceBusConnectionHelper.IsEndpointUrl(_configuration.ConnectionString))
        {
            var fullyQualifiedNamespace = ServiceBusConnectionHelper.ExtractFullyQualifiedNamespace(_configuration.ConnectionString);
            client = new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());
        }
        else
        {
            client = new ServiceBusClient(_configuration.ConnectionString);
        }

        await using (client)
        {
            var sender = client.CreateSender(destName);

            var messageGuid = Guid.NewGuid().ToString();

            var serviceBusMessage = new ServiceBusMessage(messagePayload)
            {
                CorrelationId = message.Id.ToString(),
                MessageId = messageGuid,
                ContentType = _serializer.ContentType,
                Subject = message.Label
            };

            _logger.LogInformation($"Sending message {messageGuid} with CorrelationId: {message.Id}, Subject: {message.Label}");
            await sender.SendMessageAsync(serviceBusMessage);

            _logger.LogInformation($"Message {messageGuid} published successfully to {destName}.");
        }
    }
}