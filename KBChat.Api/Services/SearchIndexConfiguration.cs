using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Storage.Blobs;

namespace KBChat.Api.Services;

public class SearchIndexConfiguration
{
    private readonly IConfiguration _config;
    private readonly ILogger _logger;
    private SearchIndexClient _indexClient;
    private SearchIndexerClient _indexerClient;
    private string _searchEndpoint;
    private string _openAiEndpoint;
    private BlobServiceClient _blobServiceClient;


    public SearchIndexConfiguration(IConfiguration config)
    {
        _config = config;
        var managedIdentityClientId = config.GetValue<string>("AZURE_CLIENT_ID");
        var azureCredential = new ManagedIdentityCredential(managedIdentityClientId);
        var localCredential = new AzureCliCredential();
        TokenCredential tokenCredential = localCredential;

        
        //Aspire inject "Endpoint=" to the connection string when not running local
        _searchEndpoint = _config.GetConnectionString("search");
        Console.WriteLine($"Search Endpoint: {_searchEndpoint}");
        if (bool.Parse(config["IS_CLOUD"]))
        {
            tokenCredential = azureCredential;
            _searchEndpoint = _searchEndpoint.Replace("Endpoint=", "");
            Console.WriteLine($"Search Endpoint: {_searchEndpoint}");
        }
        

        _openAiEndpoint = _config.GetConnectionString("openai");
        Console.WriteLine($"Azure OpenAI Endpoint: {_openAiEndpoint}");
        if (bool.Parse(config["IS_CLOUD"]))
        {
            tokenCredential = azureCredential;
            _openAiEndpoint = _openAiEndpoint.Replace("Endpoint=", "");
            Console.WriteLine($"Azure OpenAI Endpoint: {_openAiEndpoint}");
        }
        


        _indexerClient = new SearchIndexerClient(
             endpoint: new Uri(_searchEndpoint),
             tokenCredential: tokenCredential
         );

        _indexClient = new SearchIndexClient(
            endpoint: new Uri(_searchEndpoint),
            tokenCredential: tokenCredential
        );


        _blobServiceClient = new(
            serviceUri: new Uri(_config["AZURE_STORAGE_ENDPOINT"]),
            credential: tokenCredential
        );


    }


    public async Task Configure()
    {

        BlobContainerClient blobContainerClient = _blobServiceClient.GetBlobContainerClient(_config["AZURE_STORAGE_CONTAINER"]);
        if (!await blobContainerClient.ExistsAsync())
        {
            await blobContainerClient.CreateAsync();
        }

        var indexName = _config["AZURE_SEARCH_INDEX"];
        var vectorSearchDimensions = _config.GetValue<int>("EMBEDDING_VECTOR_DIMENSIONS");
        
        if (!await DoesKbIndexExist(indexName))
        {
            await CreateKbIndexAsync(indexName, vectorSearchDimensions);
        }

        var dataSource = await CreateDataSource(indexName, _indexerClient);
        var skillSet = await CreateSkillSet(indexName, _indexerClient);
        await CreateIndexerAsync(indexName,_indexerClient, dataSource, skillSet);

        
    }

    public async Task<bool> DoesKbIndexExist(string indexName)
    {
        bool indexExists = false;
        try
        {
            var index = await _indexClient.GetIndexAsync(indexName);
            if (index != null)
            {
                indexExists = true;
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            indexExists = false;
        }

        return indexExists;
    }
    public async Task CreateKbIndexAsync(string indexName, int vectorSearchDimensions)
    {
        var vectorSearchConfigName = $"{indexName}-vectorconfig";
        var vectorSearchProfileName = $"{indexName}-vectorprofile";
        var vectorizerName = $"{indexName}-vectorizer";

        var model = _config["EMBEDDING_DEPLOYMENT_NAME"];

        // Create the index
        SearchIndex index = new SearchIndex(indexName)
        {
            VectorSearch = new()
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(vectorSearchConfigName)
                },
                Profiles =
                {
                    new VectorSearchProfile(vectorSearchProfileName, vectorSearchConfigName)
                    {
                        VectorizerName = vectorizerName
                    }
                },
                Vectorizers =
                {
                    new AzureOpenAIVectorizer(vectorizerName)
                    {
                       Parameters = new AzureOpenAIVectorizerParameters()
                       {
                           ResourceUri = new Uri(_openAiEndpoint),
                           ModelName = model,
                           DeploymentName = model
                       }

                    }
                }
            },
            Fields =
            {
                new SearchableField("chunk_id") { IsKey = true, AnalyzerName = LexicalAnalyzerName.Keyword },
                new SimpleField("parent_id", SearchFieldDataType.String) { IsFilterable = true },
                new SearchableField("content") { AnalyzerName = LexicalAnalyzerName.StandardLucene },
                new SimpleField("title", SearchFieldDataType.String),
                new SimpleField("file_uri", SearchFieldDataType.String ),
                new SearchField("content_vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    VectorSearchDimensions = vectorSearchDimensions,
                    IsSearchable = true,
                    VectorSearchProfileName = vectorSearchProfileName
                }
            },
        };

        await _indexClient.CreateIndexAsync(index);
    }

    public async Task CreateIndexerAsync(
        string indexName,
        SearchIndexerClient indexerClient,
        SearchIndexerDataSourceConnection dataSource,
        SearchIndexerSkillset skillset
        )
    { 
        var schedule = new IndexingSchedule(TimeSpan.FromMinutes(5))
        {
            StartTime = DateTimeOffset.Now
        };

        var parameters = new IndexingParameters()
        {
            BatchSize = 100,
            MaxFailedItems = 0,
            MaxFailedItemsPerBatch = 0,
            IndexingParametersConfiguration = new()
            {
                DataToExtract = new BlobIndexerDataToExtract("contentAndMetadata")
            }
        };
    
        var indexer = new SearchIndexer($"{indexName}-indexer", dataSource.Name, indexName)
        {
            Description = "Indexer to pick up Knowlegde Base Documents from blob Storage",
            Schedule = schedule,
            SkillsetName = skillset.Name,
            Parameters = parameters,
            FieldMappings =
                {
                    new FieldMapping("metadata_storage_name") {TargetFieldName = "title"},
                }
        };

        await indexerClient.CreateOrUpdateIndexerAsync(indexer);


    }

    public async Task<SearchIndexerDataSourceConnection> CreateDataSource(string indexName, SearchIndexerClient indexerClient)
    {
        var dataSource = new SearchIndexerDataSourceConnection(
            name: $"{indexName}-datasource",
            type: SearchIndexerDataSourceType.AzureBlob,
            //connectionString: "BlobEndpoint=" + config.GetConnectionString("blobs"),
            connectionString: "ResourceId=" + _config["AZURE_STORAGE_RESOURCE_ID"],
            container: new SearchIndexerDataContainer(_config["AZURE_STORAGE_CONTAINER"])
        );

       
        
        await indexerClient.CreateOrUpdateDataSourceConnectionAsync(dataSource);

        return dataSource;
        
    }

    public async Task<SearchIndexerSkillset> CreateSkillSet(string indexName,SearchIndexerClient indexerClient)
    {
        var splitSkill = CreateSplitSkill();
        var embeddingSkil = CreateEmbeddingSkill();
        List<SearchIndexerSkill> skills = new List<SearchIndexerSkill>();
        skills.Add(splitSkill);
        skills.Add(embeddingSkil);

        SearchIndexerIndexProjectionSelector selector = new(
            targetIndexName: indexName,
            parentKeyFieldName: "parent_id",
            sourceContext: "/document/pages/*",
            mappings: new List<InputFieldMappingEntry>()
            {
                new InputFieldMappingEntry("content_vector")
                {
                    Source = "/document/pages/*/text_vector"
                },
                new InputFieldMappingEntry("content")
                {
                    Source = "/document/pages/*"
                },
                new InputFieldMappingEntry("title")
                {
                    Source = "/document/title"
                },
                new InputFieldMappingEntry("file_uri")
                {
                    Source = "/document/file_uri"
                }
            });

        List<SearchIndexerIndexProjectionSelector> searchIndexerIndexProjectionSelectors = new();
        searchIndexerIndexProjectionSelectors.Add(selector);

        SearchIndexerSkillset skillset = new SearchIndexerSkillset($"{indexName}-skillset", skills);
        skillset.IndexProjection = new SearchIndexerIndexProjection(searchIndexerIndexProjectionSelectors);
      

        await indexerClient.CreateOrUpdateSkillsetAsync(skillset);
        return skillset;
    }

    public SplitSkill CreateSplitSkill()
    {
        List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>();
        inputMappings.Add(new InputFieldMappingEntry("text")
        {
            Source = "/document/content"
        });
        
        List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>();
        outputMappings.Add(new OutputFieldMappingEntry("textItems")
        {
            TargetName = "pages",
        });

        SplitSkill splitSkill = new (inputMappings, outputMappings)
        {
            Description = "chunk documents",
            Context = "/document",
            TextSplitMode = TextSplitMode.Pages,
            MaximumPageLength = 4000,
            PageOverlapLength = 500,
            DefaultLanguageCode = SplitSkillLanguage.En
        };

        return splitSkill;
    }

    public AzureOpenAIEmbeddingSkill CreateEmbeddingSkill()
    {
        List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>();
        inputMappings.Add(new InputFieldMappingEntry("text")
        {
            Source = "/document/pages/*"


        });

        List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>();
        outputMappings.Add(new OutputFieldMappingEntry("embedding")
        {
            TargetName = "text_vector"
        });

        AzureOpenAIEmbeddingSkill embeddingSkill = new(inputMappings, outputMappings)
        {
            Description = "create embeddings",
            Context = "/document/pages/*",
            ResourceUri = new Uri(_openAiEndpoint),
            DeploymentName = _config.GetValue<string>("EMBEDDING_DEPLOYMENT_NAME"),
            ModelName = _config.GetValue<string>("EMBEDDING_DEPLOYMENT_NAME"),
            Dimensions = _config.GetValue<int>("EMBEDDING_VECTOR_DIMENSIONS"),

        };

        return embeddingSkill;

    }
}
