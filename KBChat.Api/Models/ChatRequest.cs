namespace KBChat.Api.Models;

public class ChatRequest
{
    public string Message { get; set; }
    public string ThreadId { get; set; } = "";
}
