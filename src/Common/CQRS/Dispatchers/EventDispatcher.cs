using System;
using System.Threading.Tasks;
using AGTec.Common.Base.Accessors;
using AGTec.Common.CQRS.Events;
using AGTec.Common.CQRS.Extensions;
using AGTec.Common.CQRS.Messaging;

namespace AGTec.Common.CQRS.Dispatchers;

public class EventDispatcher : IEventDispatcher
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly IPayloadSerializer _serializer;

    public EventDispatcher(IMessagePublisher messagePublisher,
        IPayloadSerializer serializer)
    {
        _messagePublisher = messagePublisher;
        _serializer = serializer;
    }

    public Task Raise<TEvent>(TEvent evt) where TEvent : IEvent
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));

        var publishableAttr = evt.GetPublishableAttributes();

        var messageId = Guid.Empty.Equals(CorrelationIdAccessor.CorrelationId)
            ? Guid.NewGuid()
            : CorrelationIdAccessor.CorrelationId;

        var payload = _serializer.Serialize(evt);

        var message = new Message(messageId, publishableAttr.Label, publishableAttr.Version,
            evt.GetType().AssemblyQualifiedName, payload);

        return _messagePublisher.Publish(publishableAttr.DestName, publishableAttr.Type, message);
    }
}