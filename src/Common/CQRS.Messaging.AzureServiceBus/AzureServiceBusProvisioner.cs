using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AGTec.Common.CQRS.Messaging.AzureServiceBus;

public class AzureServiceBusProvisioner : IAzureServiceBusProvisioner
{
    private readonly IMessageBusConfiguration _configuration;
    private readonly ILogger<AzureServiceBusProvisioner> _logger;

    public AzureServiceBusProvisioner(
        IMessageBusConfiguration configuration,
        ILogger<AzureServiceBusProvisioner> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EnsureQueueExistsAsync(string queueName)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentException("Queue name cannot be null or empty.", nameof(queueName));

        var adminClient = CreateAdministrationClient();

        try
        {
            var queueExists = await adminClient.QueueExistsAsync(queueName);

            if (queueExists)
            {
                _logger.LogInformation($"Queue '{queueName}' already exists.");
                return;
            }

            _logger.LogInformation($"Queue '{queueName}' does not exist. Creating...");
            await adminClient.CreateQueueAsync(queueName);
            _logger.LogInformation($"Queue '{queueName}' created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error ensuring queue '{queueName}' exists.");
            throw;
        }
    }

    public async Task EnsureTopicExistsAsync(string topicName)
    {
        if (string.IsNullOrWhiteSpace(topicName))
            throw new ArgumentException("Topic name cannot be null or empty.", nameof(topicName));

        var adminClient = CreateAdministrationClient();

        try
        {
            var topicExists = await adminClient.TopicExistsAsync(topicName);

            if (topicExists)
            {
                _logger.LogInformation($"Topic '{topicName}' already exists.");
                return;
            }

            _logger.LogInformation($"Topic '{topicName}' does not exist. Creating...");
            await adminClient.CreateTopicAsync(topicName);
            _logger.LogInformation($"Topic '{topicName}' created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error ensuring topic '{topicName}' exists.");
            throw;
        }
    }

    public async Task EnsureSubscriptionExistsAsync(string topicName, string subscriptionName)
    {
        if (string.IsNullOrWhiteSpace(topicName))
            throw new ArgumentException("Topic name cannot be null or empty.", nameof(topicName));

        if (string.IsNullOrWhiteSpace(subscriptionName))
            throw new ArgumentException("Subscription name cannot be null or empty.", nameof(subscriptionName));

        var adminClient = CreateAdministrationClient();

        try
        {
            await EnsureTopicExistsAsync(topicName);

            var subscriptionExists = await adminClient.SubscriptionExistsAsync(topicName, subscriptionName);

            if (subscriptionExists)
            {
                _logger.LogInformation($"Subscription '{subscriptionName}' on topic '{topicName}' already exists.");
                
                // Check if subscription has any rules
                var rules = adminClient.GetRulesAsync(topicName, subscriptionName);
                var hasRules = false;
                await foreach (var rule in rules)
                {
                    hasRules = true;
                    _logger.LogInformation($"Existing rule found: {rule.Name}");
                    break;
                }
                
                if (!hasRules)
                {
                    _logger.LogWarning($"Subscription '{subscriptionName}' has no rules! Creating default TrueRuleFilter.");
                    var defaultRule = new CreateRuleOptions("$Default", new TrueRuleFilter());
                    await adminClient.CreateRuleAsync(topicName, subscriptionName, defaultRule);
                    _logger.LogInformation("Default rule created successfully.");
                }
                
                return;
            }

            _logger.LogInformation($"Subscription '{subscriptionName}' on topic '{topicName}' does not exist. Creating...");
            
            var subscriptionOptions = new CreateSubscriptionOptions(topicName, subscriptionName);
            var ruleOptions = new CreateRuleOptions("$Default", new TrueRuleFilter());
            
            await adminClient.CreateSubscriptionAsync(subscriptionOptions, ruleOptions);
            _logger.LogInformation($"Subscription '{subscriptionName}' on topic '{topicName}' created successfully with default rule.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error ensuring subscription '{subscriptionName}' on topic '{topicName}' exists.");
            throw;
        }
    }

    public async Task EnsureDefaultRuleExistsAsync(string topicName, string subscriptionName)
    {
        if (string.IsNullOrWhiteSpace(topicName))
            throw new ArgumentException("Topic name cannot be null or empty.", nameof(topicName));

        if (string.IsNullOrWhiteSpace(subscriptionName))
            throw new ArgumentException("Subscription name cannot be null or empty.", nameof(subscriptionName));

        var adminClient = CreateAdministrationClient();

        try
        {
            var rules = adminClient.GetRulesAsync(topicName, subscriptionName);
            var hasRules = false;
            
            await foreach (var rule in rules)
            {
                hasRules = true;
                _logger.LogInformation($"Rule found on subscription '{subscriptionName}': {rule.Name}");
            }

            if (!hasRules)
            {
                _logger.LogWarning($"Subscription '{subscriptionName}' on topic '{topicName}' has NO rules! Messages will be dropped.");
                _logger.LogInformation("Creating default TrueRuleFilter to receive all messages...");
                
                var defaultRule = new CreateRuleOptions("$Default", new TrueRuleFilter());
                await adminClient.CreateRuleAsync(topicName, subscriptionName, defaultRule);
                
                _logger.LogInformation("Default rule created successfully. Subscription will now receive messages.");
            }
            else
            {
                _logger.LogInformation($"Subscription '{subscriptionName}' on topic '{topicName}' has rules configured.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error ensuring default rule exists for subscription '{subscriptionName}' on topic '{topicName}'.");
            throw;
        }
    }

    private ServiceBusAdministrationClient CreateAdministrationClient()
    {
        if (ServiceBusConnectionHelper.IsEndpointUrl(_configuration.ConnectionString))
        {
            var fullyQualifiedNamespace = ServiceBusConnectionHelper.ExtractFullyQualifiedNamespace(_configuration.ConnectionString);
            return new ServiceBusAdministrationClient(fullyQualifiedNamespace, new DefaultAzureCredential());
        }
        else
        {
            return new ServiceBusAdministrationClient(_configuration.ConnectionString);
        }
    }
}
