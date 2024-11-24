namespace AGTec.Common.Repository.Search.Configuration;

public class SearchDbConfiguration : ISearchDbConfiguration
{
    public const string ConfigSectionName = "SearchDBConfiguration";
    public string Username { get; set; }
    public string Password { get; set; }
    public string Host { get; set; }

    public bool IsValid()
    {
        return string.IsNullOrWhiteSpace(Username) == false
               && string.IsNullOrWhiteSpace(Password) == false
               && string.IsNullOrWhiteSpace(Host) == false;
    }
}
