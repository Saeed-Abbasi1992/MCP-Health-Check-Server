namespace McpHealthServer.Models.Iniitialize;

public class InitializeResponse
{
    public string Protocol { get; set; } = "mcp";
    public string ProtocolVersion { get; set; } = "2024-11-05";
    public Guid SessionId { get; set; }
    public CapabilitiesResponse Capabilities { get; set; }
    public string SseUrl { get; set; }
}

