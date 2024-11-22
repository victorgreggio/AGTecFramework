using System.Threading.Tasks;
using AGTec.Common.Document.Entities;

namespace AGTec.Common.Repository.Document;

public interface IRepository<T> : IReadOnlyRepository<T> where T : IDocumentEntity
{
    Task Insert(T document);
    Task<bool> Update(T document);
    Task<bool> Delete(T document);
}