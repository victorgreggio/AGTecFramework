using System.Threading.Tasks;
using AGTec.Common.CQRS.Commands;

namespace AGTec.Common.CQRS.Dispatchers;

public interface ICommandDispatcher
{
    Task Execute<TCommand>(TCommand command) where TCommand : ICommand;
}