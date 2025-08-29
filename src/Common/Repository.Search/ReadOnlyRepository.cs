using AGTec.Common.Base.Extensions;
using AGTec.Common.Document.Entities;
using AGTec.Common.Repository.Search.Exceptions;
using Elastic.Clients.Elasticsearch;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AGTec.Common.Repository.Search;

public abstract class ReadOnlyRepository<TEntity> : IReadOnlyRepository<TEntity>
    where TEntity : class, IDocumentEntity
{
    protected readonly string CollectionName = typeof(TEntity).Name.ToSnakeCase();
    protected ElasticsearchClient Client;

    protected ReadOnlyRepository(ElasticsearchClient client)
    {
        Client = client;
    }

    public async Task<TEntity> GetById(Guid id)
    {
        var response = await Client.GetAsync<TEntity>(id, idx => idx.Index(CollectionName));
        if (response.IsValidResponse == false)
            throw new InvalidResponseException(response.DebugInformation);
        return response.Source;
    }

    public async Task<IEnumerable<TEntity>> Search<TValue>(Expression<Func<TEntity, TValue>> field, string value,
        int skip = 0, int top = 10000)
    {
        var response = await Client.SearchAsync<TEntity>(s =>
            s.Indices(CollectionName).From(skip).Size(top).Query(q =>
                q.Match(x => x
                    .Field(field)
                    .Query(value))));

        if (response.IsValidResponse == false)
            throw new InvalidResponseException(response.DebugInformation);

        return response.Documents;
    }

    #region IDisposable

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
    #endregion
}