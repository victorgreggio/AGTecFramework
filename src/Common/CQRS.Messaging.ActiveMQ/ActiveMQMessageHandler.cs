using System;
using System.Collections.Generic;
using System.IO;
using AGTec.Common.CQRS.Exceptions;
using Apache.NMS;
using Apache.NMS.AMQP;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AGTec.Common.CQRS.Messaging.ActiveMQ;

public sealed class ActiveMQMessageHandler : IMessageHandler
{
    private readonly IMessageBusConfiguration _configuration;
    private readonly IConnectionFactory _factory;
    private readonly IActiveMQMessageFilterFactory _filterFactory;
    private readonly ILogger<ActiveMQMessageHandler> _logger;
    private readonly IMessageSerializer _serializer;
    private readonly IServiceProvider _serviceProvider;

    private IConnection _connection;
    private IMessageConsumer _receiverClient;
    private ISession _session;


    public ActiveMQMessageHandler(IServiceProvider serviceProvider,
        IMessageBusConfiguration configuration,
        IMessageSerializer serializer,
        IActiveMQMessageFilterFactory filterFactory,
        ILogger<ActiveMQMessageHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _serializer = serializer;
        _filterFactory = filterFactory;
        _factory = new ConnectionFactory(_configuration.ConnectionString);
        _logger = logger;
    }

    public void Handle(string destName, PublishType type, string subscriptionName = null,
        IEnumerable<IMessageFilter> filters = null)
    {
        _connection = _factory.CreateConnection();
        _session = _connection.CreateSession();
        _receiverClient = type == PublishType.Queue
            ? _session.CreateConsumer(_session.GetQueue(destName))
            : _session.CreateDurableConsumer(_session.GetTopic(destName), subscriptionName,
                _filterFactory.Create(filters), false);

        _connection.Start();
        _receiverClient.Listener += msg =>
        {
            var message = msg as IBytesMessage;

            if (message == null)
                throw new SerializerContentTypeMismatch();

            if (message.Properties["ContentType"].Equals(_serializer.ContentType) == false)
                throw new SerializerContentTypeMismatch();

            _logger.LogInformation($"Processing message {message.Properties["MessageId"]}.");

            var payload = new byte[message.BodyLength];
            if (message.BodyLength != message.ReadBytes(payload))
                throw new IOException("Is was not possible to read all the message's payload");

            var messageBody = _serializer.Deserialize(payload);

            // Needs its own DI Scope
            using (var scope = _serviceProvider.CreateScope())
            {
                var processor = scope.ServiceProvider.GetService<IMessageProcessor>();
                processor.Process(messageBody).Wait();
            }
        };
    }
}