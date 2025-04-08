using Microsoft.SemanticKernel.ChatCompletion;

namespace KBChat.Api.Models
{
    public record ThreadMessage
    {
       
        public required string Id { get; set; }

       
        public required string UserId { get; set; }

      
        public required string ThreadId { get; set; }

       
        public required string Type { get; set; }

     
        public required string Role { get; set; }

     
        public required string Content { get; set; }
               

    }
}
