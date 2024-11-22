using System.Threading.Tasks;

namespace AGTec.Common.CQRS.Messaging;

public interface IMessageProcessor
{
    Task Process(IMessage message);
}