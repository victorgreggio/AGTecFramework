using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace AGTec.Common.CQRS.Messaging.AzureServiceBus;

public sealed class AzureMessagePublisher : IMessagePublisher
{
    private readonly IMessageBusConfiguration _configuration;
    private readonly ILogger<AzureMessagePublisher> _logger;
    private readonly IMessageSerializer _serializer;

    public AzureMessagePublisher(IMessageBusConfiguration configuration,
        IMessageSerializer serializer,
        ILogger<AzureMessagePublisher> logger)
    {
        _configuration = configuration;
        _serializer = serializer;
        _logger = logger;
    }

    public async Task Publish(string destName, PublishType type, IMessage message)
    {
        var messagePayload = _serializer.Serialize(message);

        await using var client = new ServiceBusClient(_configuration.ConnectionString);
        var sender = client.CreateSender(destName);

        var messageGuid = Guid.NewGuid().ToString();

        var serviceBusMessage = new ServiceBusMessage(messagePayload)
        {
            CorrelationId = message.Id.ToString(),
            MessageId = messageGuid,
            ContentType = _serializer.ContentType,
            Subject = message.Label
        };

        await sender.SendMessageAsync(serviceBusMessage);

        _logger.LogInformation($"Message {messageGuid} published to {destName}.");
    }
}