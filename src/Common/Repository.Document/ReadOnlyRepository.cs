using System;
using System.Threading.Tasks;
using AGTec.Common.Base.Extensions;
using AGTec.Common.Document.Entities;
using MongoDB.Driver;

namespace AGTec.Common.Repository.Document;

public abstract class ReadOnlyRepository<T> : IReadOnlyRepository<T> where T : IDocumentEntity
{
    protected readonly string _collectionName = typeof(T).Name.ToSnakeCase();
    protected readonly IDocumentContext _context;

    protected ReadOnlyRepository(IDocumentContext context)
    {
        _context = context;
    }

    public virtual async Task<T> GetById(Guid id)
    {
        var filter = Builders<T>.Filter.Eq(feq => feq.Id, id);
        return await _context
            .Collection<T>(_collectionName)
            .Find(filter)
            .SingleAsync();
    }

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (disposing)
            _context.Dispose();
    }

    #endregion
}