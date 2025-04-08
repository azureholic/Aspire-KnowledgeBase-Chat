using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using KBChat.Api.Interfaces;
using KBChat.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using System.Text.Json;

namespace KBChat.Api.Controllers;



[Route("[controller]")]
[ApiController]
[Authorize]
public class ChatController(
    Kernel kernel, 
    IConfiguration configuration, 
    SearchIndexClient searchIndexClient,
    IThreadRepository threadRepository,
    ILogger<ChatController> logger
    ) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest req)
    {

        string userId = User.Identity.Name;

        var history = await threadRepository.GetChatHistoryAsync(userId, req.ThreadId);
        if (history.Count == 0)
        {
            await threadRepository.CreateThreadAsync(userId, req.ThreadId);
            await threadRepository.PostMessageAsync(userId, req.ThreadId, Prompts.SystemPrompt, AuthorRole.System);
            history.AddSystemMessage(Prompts.SystemPrompt);
        }
        
        await threadRepository.PostMessageAsync(userId, req.ThreadId, req.Message, AuthorRole.User);
        history.AddUserMessage(req.Message);


        IChatCompletionService completionService = kernel.GetRequiredService<IChatCompletionService>();

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        AzureOpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            ResponseFormat = typeof(ChatResponse)
        };
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


        var response = await completionService.GetChatMessageContentAsync(
                    chatHistory: history,
                    kernel: kernel,
                    executionSettings: openAIPromptExecutionSettings
                );

        
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        logger.LogInformation($"Assistant response: {response.Content}");
        var assistentResponse = JsonSerializer.Deserialize<ChatResponse>(response.Content, options);

        //get all history items after the last User's input. If a tool call was made, we
        //need the output in the history
        for (int i = history.Count - 1; i == 0; i--)
        {
            var chatHistory = history[i];
            if (chatHistory.Role != AuthorRole.User)
            {
                await threadRepository.PostMessageAsync(userId, req.ThreadId, chatHistory.Content, chatHistory.Role);
            }
        }

        //save the Assistant response
        await threadRepository.PostMessageAsync(userId, req.ThreadId, response.Content, AuthorRole.Assistant);

        return Ok(assistentResponse);
    }
}
