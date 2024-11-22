using System;
using System.Threading.Tasks;
using AGTec.Common.CQRS.Commands;

namespace AGTec.Common.CQRS.CommandHandlers;

public interface ICommandHandler<in TCommand> : IDisposable where TCommand : ICommand
{
    Task Execute(TCommand command);
}