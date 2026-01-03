using Microsoft.AspNetCore.Routing;

namespace McpHealthServer.Models;

public class Capabilities
{
    public Roots Roots { get; set; }
    public object Sampling { get; set; } = new { };
}