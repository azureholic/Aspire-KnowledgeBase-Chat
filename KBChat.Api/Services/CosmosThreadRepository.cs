using KBChat.Api.Interfaces;
using KBChat.Api.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.SemanticKernel.ChatCompletion;
using Container = Microsoft.Azure.Cosmos.Container;
using Thread = KBChat.Api.Models.Thread;

namespace KBChat.Api.Services;


public class CosmosThreadRepository : IThreadRepository
{

    private readonly IConfiguration _config;
    private readonly CosmosClient _cosmosClient;

    private Container _container;

    public CosmosThreadRepository(IConfiguration config, CosmosClient cosmosClient)
    {
        _config = config;
        _cosmosClient = cosmosClient;

        string databaseName = _config.GetValue<string>("COSMOS_DATABASE_NAME");
        string containerName = _config.GetValue<string>("COSMOS_CONTAINER_NAME");

        _container = _cosmosClient.GetContainer(databaseName, containerName);
    }


    

    

    

    

   
    public async Task<Thread> CreateThreadAsync(string userId, string threadId)
    {
        var newThread = new Thread
        {
            Id = threadId,
            Type = "CHAT_THREAD",
            UserId = userId,
            ThreadName = DateTime.Now.ToString("dd MMM yyyy, HH:mm"),
            
        };

        var response = await _container.CreateItemAsync<Thread>(newThread, new PartitionKey(userId));
        if (response.StatusCode != System.Net.HttpStatusCode.Created)
        {
            throw new Exception("Failed to create a new thread.");
        }
        return response;

    }

    public async Task<ChatHistory> GetChatHistoryAsync(string userId, string threadId)
    {

        using var iterator = _container
            .GetItemQueryIterator<ThreadMessage>(
                new QueryDefinition($"""
                
                    SELECT * FROM c 
                    WHERE c.threadId = @threadId
                    AND c.type = 'CHAT_MESSAGE' 
                    ORDER BY c._ts
                    
                    """)
                .WithParameter("@threadId", threadId)
            );

    


        ChatHistory history = new ChatHistory();
        // Iterate query result pages
        
        while (iterator.HasMoreResults)
        {
            FeedResponse<ThreadMessage> response = await iterator.ReadNextAsync();

            // Iterate query results
            
            foreach (ThreadMessage item in response)
            {
                AuthorRole role = new(item.Role);
                history.AddMessage(role, item.Content);
                
            }
        }

        return history;
    }

    public async Task<bool> PostMessageAsync(string userId, string threadId, string message, AuthorRole role)
    {
        string messageId = Guid.NewGuid().ToString();
        DateTime now = DateTime.Now;

        ThreadMessage newMessage = new()
        {

            Id = messageId,
            Type = "CHAT_MESSAGE",
            ThreadId = threadId,
            UserId = userId,
            Role = role.Label,
            Content = message,
            
        };

        var response = await _container.CreateItemAsync<ThreadMessage>(newMessage, new PartitionKey(userId));
        if (response.StatusCode != System.Net.HttpStatusCode.Created)
        {
            throw new Exception("Failed to create a new thread.");
        }
        return true;
    }

}