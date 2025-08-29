using System.Collections.Generic;
using System.Threading.Tasks;

namespace AGTec.Common.CQRS.Messaging;

public interface IMessageHandler
{
    Task Handle(string destName, PublishType type, string subscriptionName = null,
        IEnumerable<IMessageFilter> filters = null);
}