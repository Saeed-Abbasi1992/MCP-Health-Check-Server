namespace McpHealthServer.Models;

public class ReadyEvent
{
    public Guid SessionId { get; set; }
    public string Message { get; set; }
}
