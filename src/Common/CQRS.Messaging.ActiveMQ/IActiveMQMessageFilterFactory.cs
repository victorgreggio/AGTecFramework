using System.Collections.Generic;

namespace AGTec.Common.CQRS.Messaging.ActiveMQ;

public interface IActiveMQMessageFilterFactory
{
    string Create(IEnumerable<IMessageFilter> filters);
}