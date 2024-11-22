using System;
using System.Threading.Tasks;
using AGTec.Common.CQRS.Events;

namespace AGTec.Common.CQRS.EventHandlers;

public interface IEventHandler<in TEvent> : IDisposable where TEvent : IEvent
{
    Task Process(TEvent evt);
}