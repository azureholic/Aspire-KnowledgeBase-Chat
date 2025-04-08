namespace KBChat.AppHost.Settings;

internal class StorageAccountSettings
{
    public const string StorageAccount = "StorageAccount";
    public string?  BlobEndpoint { get; set; }
    public string? ResourceId { get; set; }
    public string? ContainerName { get; set; }

    public void Validate()
    {
        if (string.IsNullOrEmpty(BlobEndpoint))
        {
            throw new ArgumentException("StorageAccount Settings: BlobEndpoint is required.");
        }
        if (string.IsNullOrEmpty(ResourceId))
        {
            throw new ArgumentException("StorageAccount Settings: ResourceId is required.");
        }
        if (string.IsNullOrEmpty(ContainerName))
        {
            throw new ArgumentException("StorageAccount Settings: ContainerName is required.");
        }
    }
}
