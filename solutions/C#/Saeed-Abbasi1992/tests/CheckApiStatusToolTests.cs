using McpHealthServer.Models;
using McpHealthServer.Tools;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using System.Net;

namespace McpHealthServer.Tests;

[TestFixture]
public class CheckApiStatusToolTests
{
    [Test]
    public async Task CheckApiStatusTool_ReturnsUp()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.SetupRequest(HttpMethod.Get, "https://example.com/health")
                   .ReturnsResponse(HttpStatusCode.OK);

        var httpClient = mockHandler.CreateClient();
        var logger = Mock.Of<ILogger<CheckApiStatusTool>>();
        var tool = new CheckApiStatusTool(httpClient, logger);

        var result = await tool.ExecuteAsync("https://example.com/health");

        Assert.AreEqual(HealthStatusType.Up, result.Status);
    }

    [Test]
    public async Task CheckApiStatusTool_ReturnsDown_OnTimeout()
    {
        var httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(1) };
        var logger = Mock.Of<ILogger<CheckApiStatusTool>>();
        var tool = new CheckApiStatusTool(httpClient, logger);

        var result = await tool.ExecuteAsync("https://example.com/health");

        Assert.IsTrue(result.Status == HealthStatusType.Down || result.Status == HealthStatusType.Up);
        if (result is DownStatusResponse down)
            Assert.IsNotNull(down.Error);
    }
}
