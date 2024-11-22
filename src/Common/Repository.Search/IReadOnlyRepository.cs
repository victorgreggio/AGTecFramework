using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AGTec.Common.Document.Entities;

namespace AGTec.Common.Repository.Search;

public interface IReadOnlyRepository<TEntity, TContext> : IDisposable where TContext : ISearchContext
    where TEntity : class, IDocumentEntity
{
    Task<TEntity> GetById(Guid id);

    Task<IEnumerable<TEntity>> Search<TValue>(Expression<Func<TEntity, TValue>> field, string value, int skip = 0,
        int top = 10000);
}