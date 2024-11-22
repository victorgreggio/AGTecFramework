using System;
using System.Threading.Tasks;
using AGTec.Common.CQRS.Exceptions;
using AGTec.Common.CQRS.Queries;
using AGTec.Common.CQRS.QueryHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Common.CQRS.Dispatchers;

public class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public QueryDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResult> Execute<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        var handler = _serviceProvider.GetService<IQueryHandler<TQuery, TResult>>();

        if (handler == null)
        {
            var queryType = typeof(TQuery).FullName;
            throw new QueryHandlerNotFoundException($"Handler not found for query => {queryType}");
        }

        return await handler.Execute(query);
    }
}