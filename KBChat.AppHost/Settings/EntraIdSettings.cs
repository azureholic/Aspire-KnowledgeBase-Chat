namespace KBChat.AppHost.Settings;

internal class EntraIdSettings
{
    public const string EntraId = "EntraId";
    public string? ApiClientId { get; set; }
    public string? FrontEndClientId { get; set; }
    public string? Instance { get; set; }
    public string? BackendScope { get; set; }
    public string? Audience { get; set; }

    public void Validate()
    {
        if (string.IsNullOrEmpty(ApiClientId))
        {
            throw new ArgumentException("EntraId Settings: ApiClientId is required.");
        }
        if (string.IsNullOrEmpty(FrontEndClientId))
        {
            throw new ArgumentException("EntraId Settings: FrontEndClientId is required.");
        }
        if (string.IsNullOrEmpty(Instance))
        {
            throw new ArgumentException("EntraId Settings: Instance is required.");
        }
        if (string.IsNullOrEmpty(BackendScope))
        {
            throw new ArgumentException("EntraId Settings: BackendScope is required.");
        }
        if (string.IsNullOrEmpty(Audience))
        {
            throw new ArgumentException("EntraId Settings: Audience is required.");
        }
    }
}
