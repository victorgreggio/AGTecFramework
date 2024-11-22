using System.Collections.Generic;

namespace AGTec.Common.CQRS.Queries;

public interface ISortedQuery
{
    IDictionary<string, SortOrder> SortFields { get; }
}