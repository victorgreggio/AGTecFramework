using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Threading.Tasks;

namespace AGTec.Common.CQRS.Messaging.RabbitMQ;

public class RabbitMQProvisioner : IRabbitMQProvisioner
{
    private readonly IMessageBusConfiguration _configuration;
    private readonly ILogger<RabbitMQProvisioner> _logger;

    public RabbitMQProvisioner(
        IMessageBusConfiguration configuration,
        ILogger<RabbitMQProvisioner> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task EnsureQueueExistsAsync(string queueName)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentException("Queue name cannot be null or empty.", nameof(queueName));

        try
        {
            var factory = new ConnectionFactory { Uri = new Uri(_configuration.ConnectionString) };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation($"Queue '{queueName}' ensured to exist.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error ensuring queue '{queueName}' exists.");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task EnsureExchangeExistsAsync(string exchangeName)
    {
        if (string.IsNullOrWhiteSpace(exchangeName))
            throw new ArgumentException("Exchange name cannot be null or empty.", nameof(exchangeName));

        try
        {
            var factory = new ConnectionFactory { Uri = new Uri(_configuration.ConnectionString) };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(
                exchange: exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation($"Exchange '{exchangeName}' ensured to exist.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error ensuring exchange '{exchangeName}' exists.");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task EnsureBindingExistsAsync(string exchangeName, string queueName, string routingKey)
    {
        if (string.IsNullOrWhiteSpace(exchangeName))
            throw new ArgumentException("Exchange name cannot be null or empty.", nameof(exchangeName));
        
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentException("Queue name cannot be null or empty.", nameof(queueName));

        try
        {
            var factory = new ConnectionFactory { Uri = new Uri(_configuration.ConnectionString) };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueBind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKey ?? "#");

            _logger.LogInformation($"Binding between exchange '{exchangeName}' and queue '{queueName}' with routing key '{routingKey ?? "#"}' ensured to exist.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error ensuring binding between exchange '{exchangeName}' and queue '{queueName}' exists.");
            throw;
        }

        return Task.CompletedTask;
    }
}
