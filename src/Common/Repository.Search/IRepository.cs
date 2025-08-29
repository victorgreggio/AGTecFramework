using System.Threading.Tasks;
using AGTec.Common.Document.Entities;

namespace AGTec.Common.Repository.Search;

public interface IRepository<TEntity> : IReadOnlyRepository<TEntity>
    where TEntity : class, IDocumentEntity
{
    Task Insert(TEntity document);
    Task<bool> Update(TEntity document);
    Task<bool> Delete(TEntity document);
}