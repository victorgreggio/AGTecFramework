using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace AGTec.Common.Repository.Extensions;

public class IncludableJoin<TEntity, TPreviousProperty> : IIncludableJoin<TEntity, TPreviousProperty>
{
    private readonly IIncludableQueryable<TEntity, TPreviousProperty> _query;

    public IncludableJoin(IIncludableQueryable<TEntity, TPreviousProperty> query)
    {
        _query = query;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<TEntity> GetEnumerator()
    {
        return _query.GetEnumerator();
    }

    public Expression Expression => _query.Expression;
    public Type ElementType => _query.ElementType;
    public IQueryProvider Provider => _query.Provider;

    internal IIncludableQueryable<TEntity, TPreviousProperty> GetQuery()
    {
        return _query;
    }
}

public static class IncludableJoinExtensions
{
    public static IIncludableJoin<TEntity, TProperty> Join<TEntity, TProperty>(
        this IQueryable<TEntity> query,
        Expression<Func<TEntity, TProperty>> propToExpand)
        where TEntity : class
    {
        return new IncludableJoin<TEntity, TProperty>(query.Include(propToExpand));
    }

    public static IIncludableJoin<TEntity, TProperty> ThenJoin<TEntity, TPreviousProperty, TProperty>(
        this IIncludableJoin<TEntity, TPreviousProperty> query,
        Expression<Func<TPreviousProperty, TProperty>> propToExpand)
        where TEntity : class
    {
        var queryable = ((IncludableJoin<TEntity, TPreviousProperty>)query).GetQuery();
        return new IncludableJoin<TEntity, TProperty>(queryable.ThenInclude(propToExpand));
    }

    public static IIncludableJoin<TEntity, TProperty> ThenJoin<TEntity, TPreviousProperty, TProperty>(
        this IIncludableJoin<TEntity, IEnumerable<TPreviousProperty>> query,
        Expression<Func<TPreviousProperty, TProperty>> propToExpand)
        where TEntity : class
    {
        var queryable = ((IncludableJoin<TEntity, IEnumerable<TPreviousProperty>>)query).GetQuery();
        var include = queryable.ThenInclude(propToExpand);
        return new IncludableJoin<TEntity, TProperty>(include);
    }
}