namespace AGTec.Services.ServiceDefaults.Auth.Configuration;

public class AuthConfiguration : IAuthConfiguration
{
    public const string ConfigSectionName = "AuthConfiguration";

    public string AuthorityIdentity { get; set; }
    public string AuthIssuer { get; set; }
    public string Audience { get; set; }

    public bool IsValid()
    {
        return string.IsNullOrWhiteSpace(AuthorityIdentity) == false
               && string.IsNullOrWhiteSpace(AuthIssuer) == false
               && string.IsNullOrWhiteSpace(Audience) == false;
    }
}