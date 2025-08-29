using AGTec.Common.CQRS.Exceptions;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AGTec.Common.CQRS.Messaging.AzureServiceBus;

public class AzureMessageHandler : IMessageHandler
{
    private readonly IMessageBusConfiguration _configuration;
    private readonly IAzureMessageFilterFactory _filterFactory;
    private readonly ILogger<AzureMessageHandler> _logger;
    private readonly IMessageSerializer _serializer;
    private readonly IServiceProvider _serviceProvider;

    private ServiceBusProcessor _processor;

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

    public async Task Handle(string destName, PublishType type, string subscriptionName = null,
        IEnumerable<IMessageFilter> filters = null)
    {
        var client = new ServiceBusClient(_configuration.ConnectionString);

        _processor = type == PublishType.Queue
            ? client.CreateProcessor(destName, new ServiceBusProcessorOptions())
            : client.CreateProcessor(destName, subscriptionName, new ServiceBusProcessorOptions());

        if (type == PublishType.Topic)
        {
            var adminClient = new ServiceBusAdministrationClient(_configuration.ConnectionString);

            var existingRules = adminClient.GetRulesAsync(destName, subscriptionName);
            await foreach (var rule in existingRules)
            {
                await adminClient.DeleteRuleAsync(destName, subscriptionName, rule.Name);
            }

            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    if (filter.IsValid())
                    {
                        var ruleOptions = _filterFactory.Create(filter);
                        await adminClient.CreateRuleAsync(destName, subscriptionName, ruleOptions);
                    }
                }
            }
        }

        _processor.ProcessMessageAsync += async args =>
        {
            if (!args.Message.ContentType.Equals(_serializer.ContentType))
                throw new SerializerContentTypeMismatch();

            _logger.LogInformation($"Processing message {args.Message.MessageId}.");

            var messageBody = _serializer.Deserialize(args.Message.Body.ToArray());

            using (var scope = _serviceProvider.CreateScope())
            {
                var processor = scope.ServiceProvider.GetService<IMessageProcessor>();
                await processor.Process(messageBody);
            }

            await args.CompleteMessageAsync(args.Message);
        };

        _processor.ProcessErrorAsync += args =>
        {
            var errorMessage =
                $"Error processing messages from {destName}/{subscriptionName}. ErrorSource: {args.ErrorSource}, EntityPath: {args.EntityPath}, Exception: {args.Exception}";
            _logger.LogError(args.Exception, errorMessage);
            throw new ErrorHandlingMessageException(errorMessage, args.Exception);
        };

        await _processor.StartProcessingAsync();
    }
}