using System.Linq;
using AGTec.Common.Domain.Entities;

namespace AGTec.Common.Repository.Extensions;

public interface IOrderByClause<T> where T : IEntity
{
    IOrderedQueryable<T> ApplySort(IQueryable<T> query, bool firstSort);
}