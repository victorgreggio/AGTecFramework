using System.Threading.Tasks;
using AGTec.Common.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AGTec.Common.Repository;

public abstract class Repository<TEntity, TContext> :
    ReadOnlyRepository<TEntity, TContext>,
    IRepository<TEntity, TContext> where TContext : DbContext where TEntity : class, IEntity
{
    protected Repository(TContext context)
        : base(context)
    {
    }

    public IDbContextTransaction BeginTransaction()
    {
        return Context.Database.BeginTransaction();
    }

    public bool AutoDetectChanges
    {
        get => Context.ChangeTracker.AutoDetectChangesEnabled;
        set => Context.ChangeTracker.AutoDetectChangesEnabled = value;
    }

    public virtual async Task<TEntity> Insert(TEntity item, bool saveImmediately = true)
    {
        await EntitySet.AddAsync(item);

        if (saveImmediately) await Context.SaveChangesAsync();

        return item;
    }

    public virtual async Task<TEntity> Update(TEntity item, bool saveImmediately = true)
    {
        var entry = Context.Entry(item);

        if (entry != null)
        {
            entry.State = EntityState.Modified;
        }
        else
        {
            EntitySet.Attach(item);

            Context.Entry(item).State = EntityState.Modified;
        }

        if (saveImmediately) await Context.SaveChangesAsync();

        return item;
    }

    public virtual async Task Delete(TEntity item, bool saveImmediately = true)
    {
        var entry = Context.Entry(item);

        if (entry != null)
        {
            entry.State = EntityState.Deleted;
        }
        else
        {
            EntitySet.Attach(item);

            Context.Entry(item).State = EntityState.Deleted;
        }

        if (saveImmediately) await Context.SaveChangesAsync();
    }

    public TEntity Attach(TEntity item)
    {
        var attachedEntity = EntitySet.Attach(item);
        return attachedEntity.Entity;
    }

    public async Task SaveChanges()
    {
        await Context.SaveChangesAsync();
    }
}