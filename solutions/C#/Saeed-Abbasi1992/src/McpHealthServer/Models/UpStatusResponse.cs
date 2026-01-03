using System.Net;
namespace McpHealthServer.Models;

public class UpStatusResponse : CheckStatusResponseBase
{
    public HttpStatusCode Http_Status { get; set; }
    public long Latency_ms { get; set; }
}