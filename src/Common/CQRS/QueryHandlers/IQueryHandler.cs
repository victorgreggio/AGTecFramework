using System;
using System.Threading.Tasks;
using AGTec.Common.CQRS.Queries;

namespace AGTec.Common.CQRS.QueryHandlers;

public interface IQueryHandler<in TQuery, TResult> : IDisposable where TQuery : IQuery<TResult>
{
    Task<TResult> Execute(TQuery query);
}