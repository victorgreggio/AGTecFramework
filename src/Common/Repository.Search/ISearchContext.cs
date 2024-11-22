using System;
using Elastic.Clients.Elasticsearch;

namespace AGTec.Common.Repository.Search;

public interface ISearchContext : IDisposable
{
    ElasticsearchClient Client { get; }
}