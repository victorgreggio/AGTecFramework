using System;
using System.Threading;
using System.Threading.Tasks;
using AGTec.Common.Document.Entities;
using AGTec.Common.Repository.Search.Exceptions;
using AGTec.Common.Repository.Search.Extensions;

namespace AGTec.Common.Repository.Search;

public abstract class Repository<TEntity, TContext> :
    ReadOnlyRepository<TEntity, TContext>,
    IRepository<TEntity, TContext> where TContext : ISearchContext where TEntity : class, IDocumentEntity
{
    protected Repository(TContext context)
        : base(context)
    {
    }

    public async Task Insert(TEntity document)
    {
        var response = await Context.Client.IndexAsync(document: document, index: CollectionName);
        if (response.IsValidResponse == false)
            throw new InvalidResponseException(response.DebugInformation);
    }

    public async Task<bool> Update(TEntity document)
    {
        var entity = await GetById(document.Id);

        if (entity == null)
            return false;

        document.Created = entity.Created;
        document.LastUpdated = DateTime.UtcNow;

        document.SetSchemaVersion();

        var response = await Context
            .Client.UpdateAsync<TEntity, TEntity>(CollectionName,
                document.Id,
                u => u.Doc(document));

        return response.IsValidResponse;
    }

    public async Task<bool> Delete(TEntity document)
    {
        var response = await Context.Client.DeleteAsync(CollectionName, document.Id);
        return response.IsValidResponse;
    }
}