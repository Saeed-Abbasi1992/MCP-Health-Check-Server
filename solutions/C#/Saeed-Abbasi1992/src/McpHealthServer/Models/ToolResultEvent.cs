using System.Text.Json.Serialization;

namespace McpHealthServer.Models;

public class ToolResultEvent
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("http_status")]
    public int? HttpStatus { get; set; }

    [JsonPropertyName("latency_ms")]
    public long? LatencyMs { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("checked_at")]
    public DateTime CheckedAt { get; set; }
}
