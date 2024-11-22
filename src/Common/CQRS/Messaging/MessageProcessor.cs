using System;
using System.Threading.Tasks;
using AGTec.Common.CQRS.CommandHandlers;
using AGTec.Common.CQRS.Commands;
using AGTec.Common.CQRS.EventHandlers;
using AGTec.Common.CQRS.Events;
using AGTec.Common.CQRS.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Common.CQRS.Messaging;

public class MessageProcessor : IMessageProcessor
{
    private readonly IPayloadSerializer _serializer;
    private readonly IServiceProvider _serviceProvider;

    public MessageProcessor(IPayloadSerializer serializer,
        IServiceProvider serviceProvider)
    {
        _serializer = serializer;
        _serviceProvider = serviceProvider;
    }

    public Task Process(IMessage message)
    {
        var payloadType = Type.GetType(message.Type);

        var methodInfo = typeof(IPayloadSerializer).GetMethod("Deserialize");
        var genericMethod = methodInfo?.MakeGenericMethod(payloadType);
        var payload = genericMethod?.Invoke(_serializer, new object[] { message.Payload });

        if (payload is ICommand)
        {
            methodInfo = GetType().GetMethod("ExecuteCommand");
            genericMethod = methodInfo?.MakeGenericMethod(payloadType);
            return genericMethod?.Invoke(this, new[] { payload }) as Task;
        }

        if (payload is IEvent)
        {
            methodInfo = GetType().GetMethod("ProcessEvent");
            genericMethod = methodInfo?.MakeGenericMethod(payloadType);
            return genericMethod?.Invoke(this, new[] { payload }) as Task;
        }

        throw new MessageTypeNotSupportedException($"Message type {message.Type} is not supported.");
    }

    public Task ExecuteCommand<TCommand>(TCommand command) where TCommand : ICommand
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var handler = _serviceProvider.GetService<ICommandHandler<TCommand>>();

        if (handler == null)
        {
            var commandType = typeof(TCommand).FullName;
            throw new CommandHandlerNotFoundException($"Handler not found for command => {commandType}");
        }

        return handler.Execute(command);
    }

    public Task ProcessEvent<TEvent>(TEvent evt) where TEvent : IEvent
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));

        var handler = _serviceProvider.GetService<IEventHandler<TEvent>>();

        if (handler == null)
        {
            var eventType = typeof(TEvent).FullName;
            throw new EventHandlerNotFoundException($"Handler not found for command => {eventType}");
        }

        return handler.Process(evt);
    }
}