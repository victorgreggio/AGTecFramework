using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
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

        ISenderClient senderClient = type == PublishType.Queue
            ? new QueueClient(_configuration.ConnectionString, destName)
            : new TopicClient(_configuration.ConnectionString, destName);

        var messageGuid = Guid.NewGuid().ToString();

        await senderClient.SendAsync(new Microsoft.Azure.ServiceBus.Message(messagePayload)
        {
            CorrelationId = message.Id.ToString(),
            MessageId = messageGuid,
            ContentType = _serializer.ContentType,
            Label = message.Label
        });

        await senderClient.CloseAsync();

        _logger.LogInformation($"Message {messageGuid} published to {destName}.");
    }
}