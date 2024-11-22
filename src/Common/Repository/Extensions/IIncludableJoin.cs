using System.Linq;

namespace AGTec.Common.Repository.Extensions;

public interface IIncludableJoin<out TEntity, out TProperty> : IQueryable<TEntity>
{
}