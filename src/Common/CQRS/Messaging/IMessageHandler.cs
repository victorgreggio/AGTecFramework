using System.Collections.Generic;

namespace AGTec.Common.CQRS.Messaging;

public interface IMessageHandler
{
    void Handle(string destName, PublishType type, string subscriptionName = null,
        IEnumerable<IMessageFilter> filters = null);
}