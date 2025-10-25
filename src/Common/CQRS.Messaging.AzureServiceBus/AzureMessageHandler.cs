using AGTec.Common.CQRS.Exceptions;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AGTec.Common.CQRS.Messaging.AzureServiceBus;

public class AzureMessageHandler : IMessageHandler, IAsyncDisposable
{
    private readonly IMessageBusConfiguration _configuration;
    private readonly IAzureMessageFilterFactory _filterFactory;
    private readonly ILogger<AzureMessageHandler> _logger;
    private readonly IMessageSerializer _serializer;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAzureServiceBusProvisioner _provisioner;

    private ServiceBusClient _client;
    private ServiceBusProcessor _processor;

    public AzureMessageHandler(IServiceProvider serviceProvider,
        IMessageBusConfiguration configuration,
        IMessageSerializer serializer,
        IAzureMessageFilterFactory filterFactory,
        IAzureServiceBusProvisioner provisioner,
        ILogger<AzureMessageHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _serializer = serializer;
        _filterFactory = filterFactory;
        _provisioner = provisioner;
        _logger = logger;
    }

    public async Task Handle(string destName, PublishType type, string subscriptionName = null,
        IEnumerable<IMessageFilter> filters = null)
    {
        if (type == PublishType.Queue)
        {
            await _provisioner.EnsureQueueExistsAsync(destName);
        }
        else if (type == PublishType.Topic)
        {
            await _provisioner.EnsureSubscriptionExistsAsync(destName, subscriptionName);
        }

        if (ServiceBusConnectionHelper.IsEndpointUrl(_configuration.ConnectionString))
        {
            var fullyQualifiedNamespace = ServiceBusConnectionHelper.ExtractFullyQualifiedNamespace(_configuration.ConnectionString);
            _client = new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());
        }
        else
        {
            _client = new ServiceBusClient(_configuration.ConnectionString);
        }

        _processor = type == PublishType.Queue
            ? _client.CreateProcessor(destName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            })
            : _client.CreateProcessor(destName, subscriptionName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

        if (type == PublishType.Topic)
        {
            // Skip rule configuration for Service Bus Emulator as rules are pre-configured
            if (_configuration.ConnectionString?.Contains("UseDevelopmentEmulator=true", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation($"Using Service Bus Emulator. Skipping rule configuration for topic '{destName}', subscription '{subscriptionName}' - assuming rules are pre-configured.");
            }
            else
            {
                _logger.LogInformation($"Configuring subscription rules for topic '{destName}', subscription '{subscriptionName}'");
                
                ServiceBusAdministrationClient adminClient;
                if (ServiceBusConnectionHelper.IsEndpointUrl(_configuration.ConnectionString))
                {
                    var fullyQualifiedNamespace = ServiceBusConnectionHelper.ExtractFullyQualifiedNamespace(_configuration.ConnectionString);
                    adminClient = new ServiceBusAdministrationClient(fullyQualifiedNamespace, new DefaultAzureCredential());
                }
                else
                {
                    adminClient = new ServiceBusAdministrationClient(_configuration.ConnectionString);
                }

                var existingRules = adminClient.GetRulesAsync(destName, subscriptionName);
                var hasRules = false;
                await foreach (var rule in existingRules)
                {
                    hasRules = true;
                    _logger.LogInformation($"Deleting existing rule: {rule.Name}");
                    await adminClient.DeleteRuleAsync(destName, subscriptionName, rule.Name);
                }

                if (filters != null)
                {
                    foreach (var filter in filters)
                    {
                        if (filter.IsValid())
                        {
                            var ruleOptions = _filterFactory.Create(filter);
                            _logger.LogInformation($"Creating filter rule: {ruleOptions.Name}");
                            await adminClient.CreateRuleAsync(destName, subscriptionName, ruleOptions);
                        }
                    }
                }
                else if (hasRules)
                {
                    _logger.LogInformation("Creating default TrueRuleFilter to receive all messages");
                    var defaultRule = new CreateRuleOptions("$Default", new TrueRuleFilter());
                    await adminClient.CreateRuleAsync(destName, subscriptionName, defaultRule);
                }
                else
                {
                    _logger.LogInformation("No existing rules found - subscription will use default rule");
                }
            }
        }

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        _logger.LogInformation($"Starting processor for {destName}/{subscriptionName ?? "queue"}");
        await _processor.StartProcessingAsync();
        
        _logger.LogInformation($"Started processing messages from {destName}/{subscriptionName ?? "queue"}. Waiting for messages...");
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        _logger.LogInformation($"Received message {args.Message.MessageId} from {args.EntityPath}");
        
        try
        {
            if (!args.Message.ContentType.Equals(_serializer.ContentType))
            {
                _logger.LogError($"Content type mismatch. Expected: {_serializer.ContentType}, Received: {args.Message.ContentType}");
                await args.DeadLetterMessageAsync(args.Message, "ContentTypeMismatch", $"Expected {_serializer.ContentType}");
                return;
            }

            _logger.LogInformation($"Processing message {args.Message.MessageId}. Subject: {args.Message.Subject}, CorrelationId: {args.Message.CorrelationId}");

            var messageBody = _serializer.Deserialize(args.Message.Body.ToArray());

            using (var scope = _serviceProvider.CreateScope())
            {
                var processor = scope.ServiceProvider.GetService<IMessageProcessor>();
                if (processor == null)
                {
                    _logger.LogError("IMessageProcessor not found in service provider!");
                    throw new InvalidOperationException("IMessageProcessor is not registered in the DI container.");
                }
                
                await processor.Process(messageBody);
            }

            await args.CompleteMessageAsync(args.Message);
            
            _logger.LogInformation($"Message {args.Message.MessageId} processed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing message {args.Message.MessageId}.");
            throw;
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        var errorMessage =
            $"Error processing messages. ErrorSource: {args.ErrorSource}, EntityPath: {args.EntityPath}, Exception: {args.Exception}";
        _logger.LogError(args.Exception, errorMessage);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_processor != null)
        {
            await _processor.StopProcessingAsync();
            await _processor.DisposeAsync();
        }

        if (_client != null)
        {
            await _client.DisposeAsync();
        }
    }
}