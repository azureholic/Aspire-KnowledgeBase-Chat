namespace KBChat.AppHost.Settings;

internal class AzureOpenAISettings
{
    public const string AzureOpenAI = "AzureOpenAI";
    public string? CompletionDeploymentName { get; set; }
    public string? CompletionModelVersion { get; set; }
    public string? CompletionModelSku { get; set; }
    public int CompletionModelSkuCapacity { get; set; }
    public string? EmbeddingDeploymentName { get; set; }
    public string? EmbeddingModelVersion { get; set; }
    public string? EmbeddingModelSku { get; set; }
    public int EmbeddingModelSkuCapacity { get; set; }
    public int EmbeddingVectorDimensions { get; set; }
    public string? SearchIndex { get; set; }
    
    public void Validate()
    {
        if (string.IsNullOrEmpty(CompletionDeploymentName))
        {
            throw new ArgumentException("AzureOpenAI Settings: CompletionDeploymentName is required.");
        }
        if (string.IsNullOrEmpty(CompletionModelVersion))
        {
            throw new ArgumentException("AzureOpenAI Settings: CompletionModelVersion is required.");
        }
        if (string.IsNullOrEmpty(CompletionModelSku))
        {
            throw new ArgumentException("AzureOpenAI Settings: CompletionModelSku is required.");
        }
        if (CompletionModelSkuCapacity <= 0)
        {
            throw new ArgumentException("AzureOpenAI Settings: CompletionModelSkuCapacity must be greater than 0.");
        }
        if (string.IsNullOrEmpty(EmbeddingDeploymentName))
        {
            throw new ArgumentException("AzureOpenAI Settings: EmbeddingDeploymentName is required.");
        }
        if (string.IsNullOrEmpty(EmbeddingModelVersion))
        {
            throw new ArgumentException("AzureOpenAI Settings: EmbeddingModelVersion is required.");
        }
        if (string.IsNullOrEmpty(EmbeddingModelSku))
        {
            throw new ArgumentException("AzureOpenAI Settings: EmbeddingModelSku is required.");
        }
        if (EmbeddingModelSkuCapacity <= 0)
        {
            throw new ArgumentException("AzureOpenAI Settings: EmbeddingModelSkuCapacity must be greater than 0.");
        }
        if (EmbeddingVectorDimensions <= 0)
        {
            throw new ArgumentException("AzureOpenAI Settings: EmbeddingVectorDimensions must be greater than 0.");
        }
    }
}
