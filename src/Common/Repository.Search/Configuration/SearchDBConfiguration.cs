using System.Collections.Generic;
using System.Linq;

namespace AGTec.Common.Repository.Search.Configuration;

public class SearchDbConfiguration : ISearchDbConfiguration
{
    public const string ConfigSectionName = "SearchDBConfiguration";

    public string CertificateFingerprint { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public IEnumerable<string> Hosts { get; set; }

    public bool IsValid()
    {
        return string.IsNullOrWhiteSpace(CertificateFingerprint) == false
               && string.IsNullOrWhiteSpace(Username) == false
               && string.IsNullOrWhiteSpace(Password) == false
               && Hosts != null
               && Hosts.Any()
               && Hosts.All(host => string.IsNullOrWhiteSpace(host) == false);
    }
}