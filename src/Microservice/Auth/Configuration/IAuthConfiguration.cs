namespace AGTec.Microservice.Auth.Configuration;

public interface IAuthConfiguration
{
    string AuthorityIdentity { get; }
    string AuthIssuer { get; }
    string Audience { get; }
    bool IsValid();
}