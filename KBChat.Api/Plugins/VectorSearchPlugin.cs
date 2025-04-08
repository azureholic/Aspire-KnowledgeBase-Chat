using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using KBChat.Api.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using System.ComponentModel;

namespace KBChat.Api.Plugins;

public class VectorSearchPlugin 
{
    private readonly ITextEmbeddingGenerationService _textEmbeddingGenerationService;
    private readonly IVectorStoreRecordCollection<string, KnowledgeDocument> _vectorStoreRecordCollection;
    private readonly IConfiguration _config;
    private readonly ILogger<VectorSearchPlugin> _logger;
   

    public VectorSearchPlugin(ITextEmbeddingGenerationService textEmbeddingGenerationService,
                              IVectorStoreRecordCollection<string, KnowledgeDocument> vectorStoreRecordCollection,
                              IConfiguration config,
                              ILogger<VectorSearchPlugin> logger
                            )
    {
        _textEmbeddingGenerationService = textEmbeddingGenerationService;
        _vectorStoreRecordCollection = vectorStoreRecordCollection;

        _config = config;
        _logger = logger;
        
    }


    [KernelFunction("search_knowledgebase")]
    [Description("search the knowledge base for relevant documents")]
    public async Task<string> SearchKnowledgeBase(
        [Description("The literal question of the user")] string query)
    {

        var vectorSearch = new VectorStoreTextSearch<KnowledgeDocument>(_vectorStoreRecordCollection, _textEmbeddingGenerationService);

        var searchOptions = new TextSearchOptions()
        {
            Top = 3,
        };
        KernelSearchResults<object> searchResults = await vectorSearch.GetSearchResultsAsync(query, searchOptions);


        string documents = "### DOCUMENTS FOUND ###\n";

        await foreach (KnowledgeDocument doc in searchResults.Results)
        {
            documents += $"DocumentReference: {doc.file_uri}\n";
            documents += $"Content: {doc.content}\n\n";
            documents += "------\n\n";
        }


         _logger.LogInformation($"Vector Search Results for \"{query}\": {documents}");

        return documents;
    }

    
}
