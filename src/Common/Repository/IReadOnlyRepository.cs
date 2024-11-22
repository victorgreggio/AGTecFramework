using System;
using System.Linq;
using System.Linq.Expressions;
using AGTec.Common.Domain.Entities;
using AGTec.Common.Repository.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AGTec.Common.Repository;

public interface IReadOnlyRepository<TEntity, TContext> : IDisposable
    where TContext : DbContext where TEntity : class, IEntity
{
    IQueryable<TEntity> Select(Expression<Func<TEntity, bool>> where = null, IOrderByClause<TEntity>[] orderBy = null,
        int skip = 0, int top = 0);

    TEntity SelectById(Guid id);

    IIncludableJoin<TEntity, TProperty> Join<TProperty>(Expression<Func<TEntity, TProperty>> navigationProperty);
}