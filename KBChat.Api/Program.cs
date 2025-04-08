using Azure.Identity;
using Azure.Search.Documents.Indexes;
using KBChat.Api.Interfaces;
using KBChat.Api.Models;
using KBChat.Api.Plugins;
using KBChat.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

var environment = builder.Environment;

var managedIdentityClientId = builder.Configuration.GetValue<string>("AZURE_CLIENT_ID");
var azureCredential = new ManagedIdentityCredential(managedIdentityClientId);
var localCredential = new AzureCliCredential();



builder.AddServiceDefaults();

builder.AddAzureSearchClient(
    connectionName: "search",
    configureSettings: settings =>
    {
        settings.Credential = environment.IsDevelopment() ? localCredential : azureCredential;

    });

builder.AddAzureOpenAIClient(
    connectionName: "openai",
    configureSettings: settings =>
    {
        settings.Credential = environment.IsDevelopment() ? localCredential : azureCredential;
    });

builder.AddAzureCosmosClient(
    connectionName: "cosmos",
    configureSettings: settings =>
    {
        settings.Credential = environment.IsDevelopment() ? localCredential : azureCredential;
    },
    configureClientOptions: options =>
    {
        //options.ConnectionMode = ConnectionMode.Direct;
        options.RequestTimeout = TimeSpan.FromSeconds(30);
        options.SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        };
    });


builder.Services.AddSingleton<IThreadRepository, CosmosThreadRepository>();

SearchIndexConfiguration searchIndexConfig = new SearchIndexConfiguration(builder.Configuration);
await searchIndexConfig.Configure();
AzureDevOpsImport azureDevOpsImport = new AzureDevOpsImport(builder.Configuration);
await azureDevOpsImport.ImportWiki();



string? completionModel = builder.Configuration.GetValue<string>("COMPLETION_DEPLOYMENT_NAME");
string? embeddingModel = builder.Configuration.GetValue<string>("EMBEDDING_DEPLOYMENT_NAME");
string? azureSearchIndex = builder.Configuration.GetValue<string>("AZURE_SEARCH_INDEX");

#pragma warning disable SKEXP0010
builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(completionModel)
    .AddAzureAISearchVectorStoreRecordCollection<KnowledgeDocument>(azureSearchIndex)
    .AddAzureOpenAITextEmbeddingGeneration(embeddingModel)
    .Plugins.AddFromType<VectorSearchPlugin>();



var allowCors = builder.Configuration.GetValue<string>("CORS_ALLOW");

if (allowCors is not null)
{

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowChatClient",
            builder =>
            {
                builder.WithOrigins(
                    [
                        allowCors

                    ])
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
    });
}

//Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = Environment.GetEnvironmentVariable("ENTRAID_INSTANCE"); 
        options.Audience = Environment.GetEnvironmentVariable("ENTRAID_AUDIENCE");
        
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
             ValidateIssuer = true,
             ValidateAudience = true,
             ValidateLifetime = true,
             ValidateIssuerSigningKey = true,
             ValidIssuer = options.Authority,
             ValidAudience = options.Audience
        };
    });

builder.Services.AddControllers();

var app = builder.Build();

// Enable CORS
if (allowCors is not null)
{
    app.UseCors("AllowChatClient");
}


app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
