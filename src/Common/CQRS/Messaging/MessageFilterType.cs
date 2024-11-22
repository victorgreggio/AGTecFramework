using System.ComponentModel;

namespace AGTec.Common.CQRS.Messaging;

public enum MessageFilterType
{
    [Description("CorrelationId Filter")] CorrelationIdFilter = 1,

    [Description("Label Filter")] LabelFilter = 2,

    [Description("Query Filter")] QueryFilter = 3
}