namespace KBChat.AppHost.Settings;

internal class CosmosDbSettings
{
    public const string CosmosDb = "CosmosDb";
    public string? DatabaseName { get; set; }
    public string? ContainerName { get; set; }

    public void Validate()
    {
        if (string.IsNullOrEmpty(DatabaseName))
        {
            throw new ArgumentException("CosmosDb Settings: DatabaseName is required.");
        }
        if (string.IsNullOrEmpty(ContainerName))
        {
            throw new ArgumentException("CosmosDb Settings: ContainerName is required.");
        }
    }
}
