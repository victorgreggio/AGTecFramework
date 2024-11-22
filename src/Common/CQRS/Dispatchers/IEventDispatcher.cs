using System.Threading.Tasks;
using AGTec.Common.CQRS.Events;

namespace AGTec.Common.CQRS.Dispatchers;

public interface IEventDispatcher
{
    Task Raise<TEvent>(TEvent evt) where TEvent : IEvent;
}