using System;
using AGTec.Common.Document.Entities;
using AGTec.Common.Repository.Document.Configuration;
using MongoDB.Driver;

namespace AGTec.Common.Repository.Document;

public class DocumentContext(IMongoClient client, IDocumentDBConfiguration configuration) : IDocumentContext
{
    public IMongoCollection<T> Collection<T>(string collectionName) where T : IDocumentEntity
        => client.GetDatabase(configuration.DatabaseName).GetCollection<T>(collectionName);

    #region IDisposable

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #endregion
}
