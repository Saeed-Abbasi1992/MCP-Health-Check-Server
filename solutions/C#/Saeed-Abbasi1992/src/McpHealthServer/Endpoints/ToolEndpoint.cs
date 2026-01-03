using McpHealthServer.Models;
using McpHealthServer.Services;
using McpHealthServer.Tools;

namespace McpHealthServer.Endpoints;

public class ToolEndpoint
{
    private readonly ILogger<ToolEndpoint> _logger;
    private readonly CheckApiStatusTool _tool;
    private readonly UrlPolicyService _policy;
    private readonly SessionService _sessionManager;

    public ToolEndpoint(ILogger<ToolEndpoint> logger,
                        CheckApiStatusTool tool,
                        UrlPolicyService policy,
                        SessionService sessionManager)
    {
        _logger = logger;
        _tool = tool;
        _policy = policy;
        _sessionManager = sessionManager;
    }
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/mcp/tools/", async (ToolRequest request,
            UrlPolicyService policy,
            CheckApiStatusTool tool,
            SessionService sessionManager,
            ILogger<ToolEndpoint> logger) =>
        {
            logger.LogInformation("Received tool request: {ToolName} for session {SessionId}", request.Name, request.SessionId);

            // Check session
            if (!sessionManager.Sessions.ContainsKey(request.SessionId))
            {
                logger.LogWarning("Session not found: {SessionId}", request.SessionId);
                return Results.NotFound(new { error = "Session not found" });
            }

            // Check URL Policies
            if (!policy.IsAllowed(request.Input.Url, out var reason))
            {
                logger.LogWarning("Blocked URL request: {Url}. Reason: {Reason}", request.Input.Url, reason);
                return Results.BadRequest(new { error = reason });
            }
            try
            {
                var result = await tool.ExecuteAsync(request.Input.Url);

                sessionManager.EnqueueResponse(request.SessionId, result);
                logger.LogInformation("Tool executed successfully for {Url}", request.Input.Url);

                // HTTP 202 Accepted
                return Results.Accepted($"/mcp/sse/{request.SessionId}", new
                {
                    accepted = true,
                    sessionId = request.SessionId
                });
            }

            catch (Exception ex)
            {
                logger.LogError(ex, "Tool execution failed for {Url}", request.Input.Url);
                return Results.Problem("Internal server error");
            }
        });
    }
}
