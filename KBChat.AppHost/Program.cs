using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Resources;
using Azure.Provisioning.Search;
using Azure.Provisioning.Storage;
using KBChat.AppHost.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;

#pragma warning disable ASPIRECOSMOSDB001 
var builder = DistributedApplication.CreateBuilder(args);

//EntraID
var entraIdSettings = new EntraIdSettings();
builder.Configuration.GetSection(EntraIdSettings.EntraId).Bind(entraIdSettings);
entraIdSettings.Validate();

//cosmos
var cosmosDbSettings = new CosmosDbSettings();
builder.Configuration.GetSection(CosmosDbSettings.CosmosDb).Bind(cosmosDbSettings);
cosmosDbSettings.Validate();

//ai
var azureOpenAISettings = new AzureOpenAISettings();
builder.Configuration.GetSection(AzureOpenAISettings.AzureOpenAI).Bind(azureOpenAISettings);
azureOpenAISettings.Validate();

//storage
var storageAccountSettings = new StorageAccountSettings();
builder.Configuration.GetSection(StorageAccountSettings.StorageAccount).Bind(storageAccountSettings);
storageAccountSettings.Validate();

//sample prompts
var samplePromptsSettings = new SamplePromptsSettings();
builder.Configuration.GetSection(SamplePromptsSettings.SamplePrompts).Bind(samplePromptsSettings);

//Azure DevOps Wiki's to import
var azureDevOpsSettings = new AzureDevOpsSettings();
builder.Configuration.GetSection(AzureDevOpsSettings.AzureDevOps).Bind(azureDevOpsSettings);
var devOpsConfig = JsonSerializer.Serialize(azureDevOpsSettings);
devOpsConfig = Convert.ToBase64String(Encoding.UTF8.GetBytes(devOpsConfig));

var personalAccessToken = builder.AddParameter("ADOPAT", secret: true);

var samplePrompts = new StringBuilder();
foreach (var prompt in samplePromptsSettings.Prompts)
{
    samplePrompts.Append($"{prompt};");
}

var chatCompletionDeployment = new AzureOpenAIDeployment(
    name: azureOpenAISettings.CompletionDeploymentName!,
    modelName: azureOpenAISettings.CompletionDeploymentName!,
    modelVersion: azureOpenAISettings.CompletionModelVersion!,
    skuName: azureOpenAISettings.CompletionModelSku,
    skuCapacity: azureOpenAISettings.CompletionModelSkuCapacity
);

var embeddingDeployment = new AzureOpenAIDeployment(
    name: azureOpenAISettings.EmbeddingDeploymentName!,
    modelName: azureOpenAISettings.EmbeddingDeploymentName!,
    modelVersion: azureOpenAISettings.EmbeddingModelVersion!,
    skuName: azureOpenAISettings.EmbeddingModelSku,
    skuCapacity: azureOpenAISettings.EmbeddingModelSkuCapacity
);

var searchIdentity = new ManagedServiceIdentity();
searchIdentity.ManagedServiceIdentityType = ManagedServiceIdentityType.SystemAssigned;

var search = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureSearch("search")
        .ConfigureInfrastructure(infra =>
        {
            var searchService = infra.GetProvisionableResources()
                                     .OfType<SearchService>()
                                     .Single();


            searchService.Identity = searchIdentity;
            
            
            
        })
        
    : builder.AddConnectionString("search");


var openai = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("openai")
        .AddDeployment(chatCompletionDeployment)
        .AddDeployment(embeddingDeployment)
    : builder.AddConnectionString("openai");

var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsPreviewEmulator(emulator =>
        {
            emulator.WithDataExplorer();
            emulator.WithLifetime(ContainerLifetime.Persistent);
        });

var db = cosmos.AddCosmosDatabase(cosmosDbSettings.DatabaseName!);    
var container = db.AddContainer(cosmosDbSettings.ContainerName!, "/userId");

var webapi = builder.AddProject<Projects.KBChat_Api>("webapi")
    .WithReference(search)
    .WithReference(openai)
    .WithReference(cosmos)
    .WithEnvironment("COMPLETION_DEPLOYMENT_NAME", azureOpenAISettings.CompletionDeploymentName)
    .WithEnvironment("EMBEDDING_DEPLOYMENT_NAME", azureOpenAISettings.EmbeddingDeploymentName)
    .WithEnvironment("EMBEDDING_VECTOR_DIMENSIONS", azureOpenAISettings.EmbeddingVectorDimensions.ToString())
    .WithEnvironment("AZURE_SEARCH_INDEX", azureOpenAISettings.SearchIndex)
    .WithEnvironment("AZURE_STORAGE_CONTAINER", storageAccountSettings.ContainerName)
    .WithEnvironment("COSMOS_DATABASE_NAME", cosmosDbSettings.DatabaseName)
    .WithEnvironment("COSMOS_CONTAINER_NAME", cosmosDbSettings.ContainerName)
    .WithEnvironment("ENTRAID_INSTANCE", entraIdSettings.Instance)
    .WithEnvironment("ENTRAID_CLIENTID", entraIdSettings.ApiClientId)
    .WithEnvironment("ENTRAID_AUDIENCE", entraIdSettings.Audience)
    .WithEnvironment("AZURE_DEVOPS_WIKIS", devOpsConfig) 
    .WithEnvironment("AZURE_DEVOPS_PAT", personalAccessToken)
    .WithEnvironment("IS_CLOUD", builder.ExecutionContext.IsPublishMode.ToString())
    .WaitFor(container);
   
if (builder.ExecutionContext.IsPublishMode)
{
    var storage = builder.AddAzureInfrastructure("storage", infra =>
    {
        var storageAccount = new StorageAccount("storage")
        {
            Sku = new()
            {
                Name = StorageSkuName.StandardLrs,
            },
            Kind = StorageKind.StorageV2,
            MinimumTlsVersion = StorageMinimumTlsVersion.Tls1_2
        };

        infra.Add(storageAccount);

        var endpointOutput = new ProvisioningOutput("storageEndpoint", typeof(string))
        {
            Value = storageAccount.PrimaryEndpoints.BlobUri
        };

        var resourceIdOutput = new ProvisioningOutput("storageResourceId", typeof(string))
        {
            Value = storageAccount.Id
        };

        infra.Add(endpointOutput);
        infra.Add(resourceIdOutput);

    });


    webapi.WithEnvironment("AZURE_STORAGE_ENDPOINT", new BicepOutputReference("storageEndpoint", storage.Resource));
    webapi.WithEnvironment("AZURE_STORAGE_RESOURCE_ID", new BicepOutputReference("storageResourceId", storage.Resource));
}
else
{
    webapi.WithEnvironment("AZURE_STORAGE_ENDPOINT", storageAccountSettings.BlobEndpoint);
    webapi.WithEnvironment("AZURE_STORAGE_RESOURCE_ID", storageAccountSettings.ResourceId);
}



var frontend = builder.AddNpmApp("chatapp", "../KBChat.FrontEnd")
        .WithReference(webapi)
        .WithHttpEndpoint(env: "VITE_PORT")
        .WithEnvironment("BROWSER", "none")
        .WithEnvironment("VITE_PUBLIC_APP_ID", entraIdSettings.FrontEndClientId)
        .WithEnvironment("VITE_PUBLIC_AUTHORITY_URL", entraIdSettings.Instance)
        .WithEnvironment("VITE_BACKEND_SCOPE", entraIdSettings.BackendScope)
        .WithEnvironment("VITE_SAMPLEPROMPTS", samplePrompts.ToString())
        .WithEnvironment("VITE_API_PATH",$"{webapi.GetEndpoint("https")}/chat")
        .WithExternalHttpEndpoints()
        .PublishAsDockerFile(c =>
            c.WithBuildArg("VITE_PUBLIC_APP_ID", entraIdSettings.FrontEndClientId)
             .WithBuildArg("VITE_PUBLIC_AUTHORITY_URL", entraIdSettings.Instance)
             .WithBuildArg("VITE_BACKEND_SCOPE", entraIdSettings.BackendScope)
             .WithBuildArg("VITE_SAMPLEPROMPTS", samplePrompts.ToString())
             .WithBuildArg("VITE_API_PATH", "/api/chat")
        );

//Add CORS when running locally
if (!builder.ExecutionContext.IsPublishMode)
{
    webapi.WithEnvironment("CORS_ALLOW", frontend.GetEndpoint("http"));
}

builder.Build().Run();
