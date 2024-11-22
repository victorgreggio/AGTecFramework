using System;
using System.Threading.Tasks;
using AGTec.Common.Base.Accessors;
using AGTec.Common.CQRS.CommandHandlers;
using AGTec.Common.CQRS.Commands;
using AGTec.Common.CQRS.Exceptions;
using AGTec.Common.CQRS.Extensions;
using AGTec.Common.CQRS.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Common.CQRS.Dispatchers;

public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task Execute<TCommand>(TCommand command) where TCommand : ICommand
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        if (command.IsPublishable())
        {
            var publishableAttr = command.GetPublishableAttribute();

            var messageId = Guid.Empty.Equals(CorrelationIdAccessor.CorrelationId)
                ? Guid.NewGuid()
                : CorrelationIdAccessor.CorrelationId;

            var publisher = _serviceProvider.GetService<IMessagePublisher>();
            if (publisher == null) throw new MessagePublisherNotFoundException();

            var serializer = _serviceProvider.GetService<IPayloadSerializer>();
            if (serializer == null) throw new PayloadSerializerNotFound();

            var message = new Message(messageId, publishableAttr.Label, publishableAttr.Version,
                command.GetType().AssemblyQualifiedName, serializer.Serialize(command));

            return publisher.Publish(publishableAttr.DestName, publishableAttr.Type, message);
        }

        var handler = _serviceProvider.GetService<ICommandHandler<TCommand>>();

        if (handler == null)
        {
            var commandType = typeof(TCommand).FullName;
            throw new CommandHandlerNotFoundException($"Handler not found for command => {commandType}");
        }

        return handler.Execute(command);
    }
}