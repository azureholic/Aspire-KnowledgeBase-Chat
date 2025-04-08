using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace KBChat.Api.Models;

public sealed class KnowledgeDocument()
{
#pragma warning disable SKEXP0001

    [VectorStoreRecordKey]
    public string chunk_id { get; set; }
    
    [VectorStoreRecordData]
    [TextSearchResultName]
    public string parent_id { get; set; }

    [VectorStoreRecordData]
    [TextSearchResultName]
    public string content { get; set; }

    [VectorStoreRecordData]
    [TextSearchResultName]
    public string title { get; set; }

    [VectorStoreRecordData]
    [TextSearchResultName]
    public string file_uri { get; set; }

    [VectorStoreRecordVector(3072)]
    public ReadOnlyMemory<float> content_vector { get; set; }

   


}