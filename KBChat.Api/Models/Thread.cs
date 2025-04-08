namespace KBChat.Api.Models
{
    public record Thread
    {
       
        public required string Id { get; set; }

        
        public required string Type { get; set; }

     
        public required string UserId { get; set; }

        public string? ThreadName { get; set; }

        
    }
}
