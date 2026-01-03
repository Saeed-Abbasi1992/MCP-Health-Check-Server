using McpHealthServer;
using McpHealthServer.Models;
using McpHealthServer.Models.Iniitialize;
using System.Collections.Concurrent;

public class SessionService
{
    public ConcurrentDictionary<Guid, Session> Sessions { get; } = new();

    public Session CreateSession()
    {
        var session = new Session { SessionId = Guid.NewGuid() };
        Sessions[session.SessionId] = session;
        return session;
    }

    public void EnqueueResponse(Guid sessionId, CheckStatusResponseBase response)
    {
        if (Sessions.TryGetValue(sessionId, out var session))
        {
            session.Messages.Enqueue(response);
            session.LastActive = DateTime.UtcNow;
        }
    }

    public CheckStatusResponseBase DequeueResponse(Guid sessionId)
    {
        if (Sessions.TryGetValue(sessionId, out var session))
        {
            session.LastActive = DateTime.UtcNow;
            session.Messages.TryDequeue(out var response);
            return response;
        }
        return null;
    }

    public InitializeResponse CreateInitializeResponse(Session session)
    {
        return new InitializeResponse
        {
            SessionId = session.SessionId,
            Capabilities = new CapabilitiesResponse
            {
                Tools = new[] { new ToolInfo { Name = Constants.CheckApiStatus, InputSchema = new { url = "string" } } }
            },
            SseUrl = $"/mcp/sse/{session.SessionId}"
        };
    }

    // TTL Cleanup (background task)
    public void CleanupExpiredSessions(TimeSpan timeout)
    {
        var now = DateTime.UtcNow;
        foreach (var keyValue in Sessions.ToArray())
        {
            if ((now - keyValue.Value.LastActive) > timeout)
            {
                Sessions.TryRemove(keyValue.Key, out _);
            }
        }
    }
}
