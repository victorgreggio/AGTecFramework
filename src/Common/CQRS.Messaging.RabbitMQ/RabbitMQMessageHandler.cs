using AGTec.Common.CQRS.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AGTec.Common.CQRS.Messaging.RabbitMQ;

public class RabbitMQMessageHandler : IMessageHandler, IAsyncDisposable
{
    private readonly IMessageBusConfiguration _configuration;
    private readonly IRabbitMQFilterFactory _filterFactory;
    private readonly ILogger<RabbitMQMessageHandler> _logger;
    private readonly IMessageSerializer _serializer;
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMQProvisioner _provisioner;

    private IConnection _connection;
    private IModel _channel;
    private AsyncEventingBasicConsumer _consumer;
    private string _consumerTag;

    public RabbitMQMessageHandler(IServiceProvider serviceProvider,
        IMessageBusConfiguration configuration,
        IMessageSerializer serializer,
        IRabbitMQFilterFactory filterFactory,
        IRabbitMQProvisioner provisioner,
        ILogger<RabbitMQMessageHandler> logger)
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
        string queueName;

        if (type == PublishType.Queue)
        {
            await _provisioner.EnsureQueueExistsAsync(destName);
            queueName = destName;
        }
        else if (type == PublishType.Topic)
        {
            await _provisioner.EnsureExchangeExistsAsync(destName);
            
            queueName = string.IsNullOrWhiteSpace(subscriptionName) 
                ? $"{destName}.subscription" 
                : subscriptionName;
            
            await _provisioner.EnsureQueueExistsAsync(queueName);

            var routingKey = "#";
            if (filters != null && filters.Any())
            {
                var labelFilter = filters.FirstOrDefault(f => f.Type == MessageFilterType.LabelFilter);
                if (labelFilter != null && labelFilter.IsValid())
                {
                    routingKey = labelFilter.Expression;
                }
            }

            await _provisioner.EnsureBindingExistsAsync(destName, queueName, routingKey);
            
            _logger.LogInformation($"Configured subscription '{queueName}' for topic '{destName}' with routing key '{routingKey}'");
        }
        else
        {
            throw new ArgumentException($"Unsupported PublishType: {type}");
        }

        var factory = new ConnectionFactory { Uri = new Uri(_configuration.ConnectionString) };
        factory.DispatchConsumersAsync = true;

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _consumer = new AsyncEventingBasicConsumer(_channel);
        _consumer.Received += async (sender, args) => await ProcessMessageAsync(args);

        _consumerTag = _channel.BasicConsume(
            queue: queueName,
            autoAck: false,
            consumer: _consumer);

        _logger.LogInformation($"Started processing messages from queue '{queueName}'. Waiting for messages...");
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs args)
    {
        _logger.LogInformation($"Received message {args.BasicProperties?.MessageId} from {args.RoutingKey}");
        
        try
        {
            if (args.BasicProperties?.ContentType != null && 
                !args.BasicProperties.ContentType.Equals(_serializer.ContentType))
            {
                _logger.LogError($"Content type mismatch. Expected: {_serializer.ContentType}, Received: {args.BasicProperties.ContentType}");
                _channel.BasicNack(args.DeliveryTag, false, false);
                return;
            }

            _logger.LogInformation($"Processing message {args.BasicProperties?.MessageId}. Type: {args.BasicProperties?.Type}, CorrelationId: {args.BasicProperties?.CorrelationId}");

            var messageBody = _serializer.Deserialize(args.Body.ToArray());

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

            _channel.BasicAck(args.DeliveryTag, false);
            
            _logger.LogInformation($"Message {args.BasicProperties?.MessageId} processed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing message {args.BasicProperties?.MessageId}.");
            _channel.BasicNack(args.DeliveryTag, false, true);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_consumerTag != null && _channel != null)
        {
            _channel.BasicCancel(_consumerTag);
        }

        _channel?.Close();
        _channel?.Dispose();

        _connection?.Close();
        _connection?.Dispose();

        await Task.CompletedTask;
    }
}
