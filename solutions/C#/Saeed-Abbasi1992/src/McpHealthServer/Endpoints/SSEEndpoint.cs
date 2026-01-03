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
                _logger.LogWarning("SSE requested for non-existing session: {SessionId}", sessionId);
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Session not found");
                return;
            }

            context.Response.Headers.Add("Content-Type", "text/event-stream");

            // Handshake / Ready Event
            var readyEvent = new ReadyEvent
            {
                SessionId = sessionId,
                Message = "Session active, send tool requests to /mcp/tool"
            };
            await context.Response.WriteAsync($"event: mcp.ready\ndata: {JsonSerializer.Serialize(readyEvent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })}\n\n");
            await context.Response.Body.FlushAsync();
            _logger.LogInformation("SSE handshake sent for session {SessionId}", sessionId);

            // Heartbeat loop
            _ = Task.Run(async () =>
            {
                while (!context.RequestAborted.IsCancellationRequested && _sessionService.Sessions.ContainsKey(sessionId))
                {
                    await Task.Delay(10000);
                    await context.Response.WriteAsync($": heartbeat\n\n");
                    await context.Response.Body.FlushAsync();
                }
            });

            // Stream tool results
            while (!context.RequestAborted.IsCancellationRequested)
            {
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

                    await context.Response.WriteAsync($"event: mcp.tool.result\ndata: {JsonSerializer.Serialize(toolEvent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })}\n\n");
                    await context.Response.Body.FlushAsync();
                    _logger.LogInformation("SSE tool result sent for {Url}", response.Url);
                }
                else
                {
                    await Task.Delay(100);
                }
            }
        });
    }
}
