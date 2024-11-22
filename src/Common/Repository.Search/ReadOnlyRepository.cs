using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AGTec.Common.Base.Extensions;
using AGTec.Common.Document.Entities;
using AGTec.Common.Repository.Search.Exceptions;

namespace AGTec.Common.Repository.Search;

public abstract class ReadOnlyRepository<TEntity, TContext> : IReadOnlyRepository<TEntity, TContext>
    where TContext : ISearchContext where TEntity : class, IDocumentEntity
{
    protected readonly string CollectionName = typeof(TEntity).Name.ToSnakeCase();
    protected ISearchContext Context;

    protected ReadOnlyRepository(TContext context)
    {
        Context = context;
    }

    public async Task<TEntity> GetById(Guid id)
    {
        var response = await Context.Client.GetAsync<TEntity>(id, idx => idx.Index(CollectionName));
        if (response.IsValidResponse == false)
            throw new InvalidResponseException(response.DebugInformation);
        return response.Source;
    }

    public async Task<IEnumerable<TEntity>> Search<TValue>(Expression<Func<TEntity, TValue>> field, string value,
        int skip = 0, int top = 10000)
    {
        var response = await Context.Client.SearchAsync<TEntity>(s =>
            s.Index(CollectionName).From(skip).Size(top).Query(q =>
                q.Match(x => x
                    .Field(field)
                    .Query(value))));

        if (response.IsValidResponse == false)
            throw new InvalidResponseException(response.DebugInformation);

        return response.Documents;
    }

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (disposing)
            Context.Dispose();
    }

    #endregion
}