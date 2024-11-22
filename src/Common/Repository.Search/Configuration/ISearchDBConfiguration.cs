using System.Collections.Generic;

namespace AGTec.Common.Repository.Search.Configuration;

public interface ISearchDbConfiguration
{
    string CertificateFingerprint { get; }
    string Username { get; }
    string Password { get; }
    IEnumerable<string> Hosts { get; }
}