using KBChat.Api.Models;
using Microsoft.SemanticKernel.ChatCompletion;
using Thread = KBChat.Api.Models.Thread;

namespace KBChat.Api.Interfaces
{
   
    public interface IThreadRepository
    {
        Task<Thread> CreateThreadAsync(string userId, string threadId);
        Task<ChatHistory> GetChatHistoryAsync(string userId, string threadId);
        Task<bool> PostMessageAsync(string userId, string threadId, string message, AuthorRole role);
        
    }
}
