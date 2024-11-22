using System.Threading.Tasks;
using AGTec.Common.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AGTec.Common.Repository;

public interface IRepository<TEntity, TContext> : IReadOnlyRepository<TEntity, TContext>, ITrackingChangeRepository
    where TContext : DbContext where TEntity : class, IEntity
{
    IDbContextTransaction BeginTransaction();

    Task<TEntity> Insert(TEntity item, bool saveImmediately = true);

    Task<TEntity> Update(TEntity item, bool saveImmediately = true);

    Task Delete(TEntity item, bool saveImmediately = true);

    TEntity Attach(TEntity item);

    Task SaveChanges();
}