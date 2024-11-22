namespace AGTec.Common.Repository.Document.Configuration;

public interface IDocumentDBConfiguration
{
    string ConnectionString { get; }
    string Database { get; }
}