using McpHealthServer.Models;
using System.Text.Json;

namespace McpHealthServer.Endpoints;

public class SSEEndpoint
{
    private readonly ILogger<SSEEndpoint> _logger;
    private readonly SessionService _sessionService;

    public SSEEndpoint(ILogger<SSEEndpoint> logger, SessionService sessionService)
    {
        _logger = logger;
        _sessionService = sessionService;
    }

    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/mcp/sse/{sessionId}", async (Guid sessionId, HttpContext context) =>
        {
            if (!_sessionService.Sessions.ContainsKey(sessionId))
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Session not found");
                return;
            }

            context.Response.Headers.Append("Content-Type", "text/event-stream");
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("Connection", "keep-alive");

            // Handshake / Ready
            var readyEvent = new ReadyEvent
            {
                SessionId = sessionId,
                Message = "Session active"
            };

            await context.Response.WriteAsync(
                $"event: mcp.ready\ndata: {JsonSerializer.Serialize(readyEvent)}\n\n");
            await context.Response.Body.FlushAsync();

            var lastHeartbeat = DateTime.UtcNow;

            while (!context.RequestAborted.IsCancellationRequested &&
                   _sessionService.Sessions.ContainsKey(sessionId))
            {
                // Send heartbeat every 10s
                if ((DateTime.UtcNow - lastHeartbeat).TotalSeconds >= 10)
                {
                    await context.Response.WriteAsync(": heartbeat\n\n");
                    await context.Response.Body.FlushAsync();
                    lastHeartbeat = DateTime.UtcNow;
                }

                var response = _sessionService.DequeueResponse(sessionId);
                if (response != null)
                {
                    var toolEvent = new ToolResultEvent
                    {
                        Url = response.Url,
                        Status = response.Status.ToString().ToUpper(),
                        HttpStatus = response is UpStatusResponse up ? (int)up.Http_Status : null,
                        LatencyMs = response is UpStatusResponse up2 ? up2.Latency_ms : null,
                        Error = response is DownStatusResponse down ? down.Error : null,
                        CheckedAt = response.Checked_at
                    };

                    await context.Response.WriteAsync(
                        $"event: mcp.tool.result\ndata: {JsonSerializer.Serialize(toolEvent)}\n\n");
                    await context.Response.Body.FlushAsync();
                }
                else
                {
                    await Task.Delay(100, context.RequestAborted);
                }
            }
        });

    }
}
