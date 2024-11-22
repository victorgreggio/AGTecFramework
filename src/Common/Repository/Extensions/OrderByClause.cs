using System;
using System.Linq;
using System.Linq.Expressions;
using AGTec.Common.Domain.Entities;

namespace AGTec.Common.Repository.Extensions;

public enum SortDirection
{
    Ascending,
    Decending
}

public class OrderByClause<T, TProperty> : IOrderByClause<T> where T : IEntity, new()
{
    private OrderByClause()
    {
    }

    public OrderByClause(
        Expression<Func<T, TProperty>> orderBy,
        SortDirection sortDirection = SortDirection.Ascending)
    {
        OrderBy = orderBy;
        SortDirection = sortDirection;
    }

    private Expression<Func<T, TProperty>> OrderBy { get; }
    private SortDirection SortDirection { get; }

    public IOrderedQueryable<T> ApplySort(IQueryable<T> query, bool firstSort)
    {
        if (SortDirection == SortDirection.Ascending)
            return firstSort ? query.OrderBy(OrderBy) : ((IOrderedQueryable<T>)query).ThenBy(OrderBy);
        return firstSort ? query.OrderByDescending(OrderBy) : ((IOrderedQueryable<T>)query).ThenByDescending(OrderBy);
    }
}