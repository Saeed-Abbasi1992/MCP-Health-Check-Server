using System.Text.Json;

namespace McpHealthServer.Endpoints;
public class InitializeEndpoint
{
    private readonly ILogger<InitializeEndpoint> _logger;
    private readonly SessionService _sessionService;

    public InitializeEndpoint(ILogger<InitializeEndpoint> logger, SessionService sessionService)
    {
        _logger = logger;
        _sessionService = sessionService;
    }

    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/mcp/initialize", async (HttpContext context) =>
        {
            var session = _sessionService.CreateSession();

            _logger.LogInformation("New session created: {SessionId}", session.SessionId);

            var response = _sessionService.CreateInitializeResponse(session);
            return Results.Json(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        });
    }
}

