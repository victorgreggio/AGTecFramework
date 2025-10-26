using System.Collections.Generic;

namespace AGTec.Common.CQRS.Messaging.RabbitMQ;

public interface IRabbitMQFilterFactory
{
    IDictionary<string, object> CreateHeaders(IMessageFilter filter);
}
