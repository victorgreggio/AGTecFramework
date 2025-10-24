using System;

namespace AGTec.Common.CQRS.Messaging.AzureServiceBus;

internal static class ServiceBusConnectionHelper
{
    public static bool IsEndpointUrl(string connectionString)
    {
        return connectionString.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               connectionString.StartsWith("sb://", StringComparison.OrdinalIgnoreCase);
    }

    public static string ExtractFullyQualifiedNamespace(string endpointUrl)
    {
        return endpointUrl
            .Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("sb://", "", StringComparison.OrdinalIgnoreCase)
            .Replace(":443/", "")
            .Replace("/", "")
            .TrimEnd();
    }
}
