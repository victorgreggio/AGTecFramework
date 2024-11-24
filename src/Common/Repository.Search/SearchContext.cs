using System;
using AGTec.Common.Repository.Search.Configuration;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace AGTec.Common.Repository.Search;

public class SearchContext : ISearchContext
{
    public SearchContext(ISearchDbConfiguration configuration)
    {
        var pool = new StaticNodePool(new[] { new Uri(configuration.Host) });
        var settings = new ElasticsearchClientSettings(pool)
            .EnableDebugMode()
            .Authentication(new BasicAuthentication(configuration.Username, configuration.Password));

        Client = new ElasticsearchClient(settings);
    }

    public ElasticsearchClient Client { get; }

    #region IDisposable

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #endregion
}
