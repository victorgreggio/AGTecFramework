using AGTec.Common.Document.Attributes;
using AGTec.Common.Document.Entities;

namespace AGTec.Common.Repository.Search.Extensions;

internal static class SearchEntityExtesions
{
    public static void SetSchemaVersion(this IDocumentEntity entity)
    {
        var memberInfo = entity.GetType();
        var attributes = memberInfo.GetCustomAttributes(true);

        var version = 1; // DefaultVersion

        foreach (var attribute in attributes)
            if (attribute is SchemaVersionAttribute schemaVersionAttribute)
            {
                version = schemaVersionAttribute.Version;
                break;
            }

        entity.SchemaVersion = version;
    }
}