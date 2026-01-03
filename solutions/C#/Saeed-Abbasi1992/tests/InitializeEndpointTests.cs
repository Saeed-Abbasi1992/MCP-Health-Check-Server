using McpHealthServer;
using McpHealthServer.Models;
using McpHealthServer.Models.Iniitialize;

[TestFixture]
public class InitializeEndpointTests
{
    [Test]
    public void Initialize_ReturnsSessionIdAndTools()
    {
        var sessionService = new SessionService();
        var session = sessionService.CreateSession();

        var response = new InitializeResponse
        {
            SessionId = session.SessionId,
            Capabilities = new CapabilitiesResponse
            {
                Tools = new[]
                {
                        new ToolInfo { Name = Constants.CheckApiStatus, InputSchema = new { url = "string" } }
                    }
            },
            SseUrl = $"/mcp/sse/{session.SessionId}"
        };

        Assert.AreEqual(session.SessionId, response.SessionId);
        Assert.IsNotNull(response.Capabilities);
        Assert.IsNotEmpty(response.Capabilities.Tools);
        Assert.AreEqual(Constants.CheckApiStatus, response.Capabilities.Tools[0].Name);
        Assert.IsTrue(response.SseUrl.Contains(session.SessionId.ToString()));
    }
}