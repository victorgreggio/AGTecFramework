using System.Threading.Tasks;
using AGTec.Common.CQRS.Queries;

namespace AGTec.Common.CQRS.Dispatchers;

public interface IQueryDispatcher
{
    Task<TResult> Execute<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>;
}