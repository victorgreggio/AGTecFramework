namespace AGTec.Services.ServiceDefaults.Authentication;

public interface IAuthenticationConfiguration
{
    public string Authority { get; }
    public string Audience { get; }
    public string AuthIssuer { get; }
    public bool RequireHttpsMetadata { get; }
    public bool IsValid();
}