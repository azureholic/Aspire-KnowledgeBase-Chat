namespace KBChat.AppHost.Settings;

internal class ConnectionStringsSetting
{
    
    public const string ConnectionStrings = "ConnectionStrings";
    public string? Search { get; set; }
    public string? OpenAI { get; set; }
    public string? Cosmos { get; set; }
    public void Validate()
    {
        if (string.IsNullOrEmpty(Search))
        {
            throw new ArgumentException("ConnectionStrings Settings: Search is required.");
        }
        if (string.IsNullOrEmpty(OpenAI))
        {
            throw new ArgumentException("ConnectionStrings Settings: AzureOpenAI is required.");
        }
        if (string.IsNullOrEmpty(Cosmos))
        {
            throw new ArgumentException("ConnectionStrings Settings: Cosmos is required.");
        }
    }
}
