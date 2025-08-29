namespace AGTec.Common.Repository.Document.Configuration;

public class DocumentDBConfiguration : IDocumentDBConfiguration
{
    public string DatabaseName { get; set; }

    public bool IsValid()
    {
        return string.IsNullOrWhiteSpace(DatabaseName) == false;
    }
}