using System;
using AGTec.Common.Document.Entities;
using MongoDB.Driver;

namespace AGTec.Common.Repository.Document;

public interface IDocumentContext : IDisposable
{
    IMongoCollection<T> Collection<T>(string collectionName) where T : IDocumentEntity;
}