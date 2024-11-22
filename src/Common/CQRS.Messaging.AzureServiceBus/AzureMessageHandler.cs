using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AGTec.Common.Base.Extensions;
using AGTec.Common.CQRS.Exceptions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AGTec.Common.CQRS.Messaging.AzureServiceBus;

public class AzureMessageHandler : IMessageHandler
{
    private readonly IMessageBusConfiguration _configuration;
    private readonly IAzureMessageFilterFactory _filterFactory;
    private readonly ILogger<AzureMessageHandler> _logger;
    private readonly IMessageSerializer _serializer;
    private readonly IServiceProvider _serviceProvider;

    private IReceiverClient _receiverClient;

    public AzureMessageHandler(IServiceProvider serviceProvider,
        IMessageBusConfiguration configuration,
        IMessageSerializer serializer,
        IAzureMessageFilterFactory filterFactory,
        ILogger<AzureMessageHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _serializer = serializer;
        _filterFactory = filterFactory;
        _logger = logger;
    }

    public void Handle(string destName, PublishType type, string subscriptionName = null,
        IEnumerable<IMessageFilter> filters = null)
    {
        _receiverClient = type == PublishType.Queue
            ? new QueueClient(_configuration.ConnectionString, destName)
            : new SubscriptionClient(_configuration.ConnectionString, destName, subscriptionName);

        if (type == PublishType.Topic)
        {
            var subscriptionClient = _receiverClient as ISubscriptionClient;
            var existingFilters = subscriptionClient.GetRulesAsync().Result;

            existingFilters?.ForEach(filter => subscriptionClient.RemoveRuleAsync(filter.Name).Wait());

            filters?.ForEach(filter =>
            {
                if (filter.IsValid())
                    subscriptionClient.AddRuleAsync(_filterFactory.Create(filter)).Wait();
            });
        }

        _receiverClient.RegisterMessageHandler(
            (message, cancellationToken) =>
            {
                if (message.ContentType.Equals(_serializer.ContentType) == false)
                    throw new SerializerContentTypeMismatch();

                _logger.LogInformation($"Processing message {message.MessageId}.");

                var messageBody = _serializer.Deserialize(message.Body);

                // Needs its own DI Scope
                using (var scope = _serviceProvider.CreateScope())
                {
                    var processor = scope.ServiceProvider.GetService<IMessageProcessor>();
                    processor.Process(messageBody).Wait(cancellationToken);
                }

                return Task.CompletedTask;
            },
            exceptionEvent =>
            {
                var exceptionReceivedContext = $"Action: {exceptionEvent.ExceptionReceivedContext.Action} -" +
                                               $"ClientId: {exceptionEvent.ExceptionReceivedContext.ClientId} -" +
                                               $"Endpoint: {exceptionEvent.ExceptionReceivedContext.Endpoint} -" +
                                               $"EntityPath: {exceptionEvent.ExceptionReceivedContext.EntityPath}.";

                var errorMessage =
                    $"Error processing messages from {destName}/{subscriptionName}. {exceptionReceivedContext}";

                _logger.LogError(exceptionEvent.Exception, errorMessage);
                throw new ErrorHandlingMessageException(errorMessage, exceptionEvent.Exception);
            });
    }
}