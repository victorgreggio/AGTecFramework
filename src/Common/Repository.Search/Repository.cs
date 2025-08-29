using AGTec.Common.Document.Entities;
using AGTec.Common.Repository.Search.Exceptions;
using AGTec.Common.Repository.Search.Extensions;
using Elastic.Clients.Elasticsearch;
using System;
using System.Threading.Tasks;

namespace AGTec.Common.Repository.Search;

public abstract class Repository<TEntity> :
    ReadOnlyRepository<TEntity>,
    IRepository<TEntity> where TEntity : class, IDocumentEntity
{
    protected Repository(ElasticsearchClient client)
        : base(client)
    {
    }

    public async Task Insert(TEntity document)
    {
        var response = await Client.IndexAsync(document, CollectionName, id: document.Id);
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

        var response = await Client.UpdateAsync<TEntity, TEntity>(CollectionName,
                document.Id,
                u => u.Doc(document));

        return response.IsValidResponse;
    }

    public async Task<bool> Delete(TEntity document)
    {
        var response = await Client.DeleteAsync(CollectionName, document.Id);
        return response.IsValidResponse;
    }
}