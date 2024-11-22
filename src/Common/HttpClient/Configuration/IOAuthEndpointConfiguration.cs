namespace AGTec.Common.HttpClient.Configuration;

public interface IOAuthEndpointConfiguration : ISecuredEndpointConfiguration
{
    string Client { get; }
    string Secret { get; }
    string Scope { get; }
    string AuthorityIdentity { get; }
}