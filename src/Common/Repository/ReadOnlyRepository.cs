using System;
using System.Linq;
using System.Linq.Expressions;
using AGTec.Common.Domain.Entities;
using AGTec.Common.Repository.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AGTec.Common.Repository;

public abstract class ReadOnlyRepository<TEntity, TContext> : IReadOnlyRepository<TEntity, TContext>
    where TContext : DbContext where TEntity : class, IEntity
{
    protected DbContext Context;
    protected DbSet<TEntity> EntitySet;

    protected ReadOnlyRepository(TContext context)
    {
        Context = context;
        EntitySet = context.Set<TEntity>();
    }

    public TEntity SelectById(Guid id)
    {
        return EntitySet.Find(id);
    }

    public IQueryable<TEntity> Select(Expression<Func<TEntity, bool>> where = null,
        IOrderByClause<TEntity>[] orderBy = null, int skip = 0, int top = 0)
    {
        IQueryable<TEntity> query = EntitySet;

        if (where != null)
            query = query.Where(where);

        if (orderBy != null)
        {
            var isFirstSort = true;

            orderBy.ToList().ForEach(one =>
            {
                query = one.ApplySort(query, isFirstSort);
                isFirstSort = false;
            });
        }

        if (skip > 0)
            query = query.Skip(skip);

        if (top > 0)
            query = query.Take(top);

        return query;
    }

    public virtual IIncludableJoin<TEntity, TProperty> Join<TProperty>(
        Expression<Func<TEntity, TProperty>> navigationProperty)
    {
        return EntitySet.Join(navigationProperty);
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