namespace McpHealthServer.Models
{
    public record ToolRequest(Guid SessionId, string Name, ToolInput Input);

    public record ToolInput(string Url);
}
