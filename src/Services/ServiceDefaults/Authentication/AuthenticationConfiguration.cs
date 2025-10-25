namespace AGTec.Services.ServiceDefaults.Authentication;

public class AuthenticationConfiguration : IAuthenticationConfiguration
{
    public string Authority { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string AuthIssuer { get; init; } = string.Empty;
    public bool RequireHttpsMetadata { get; init; } = true;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Authority) && !string.IsNullOrWhiteSpace(Audience);
    }
}
