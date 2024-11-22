namespace AGTec.Common.Repository.Document.Configuration;

public class DocumentDBConfiguration : IDocumentDBConfiguration
{
    public const string ConfigSectionName = "DocumentDBConfiguration";

    public string ConnectionString { get; set; }

    public string Database { get; set; }

    public bool IsValid()
    {
        return string.IsNullOrWhiteSpace(ConnectionString) == false
               && string.IsNullOrWhiteSpace(Database) == false;
    }
}