using McpHealthServer.Models.Iniitialize;
using McpHealthServer.Tools;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using NUnit.Framework;
using System.Net;
using System.Text.Json;

namespace Tests
{
    [TestFixture]
    public class SSEIntegrationTests
    {
        private WebApplicationFactory<Program> _factory;

        [SetUp]
        public void Setup()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        // Remove hosted services to avoid background jobs running
                        var hostedServices = services.Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
                        foreach (var service in hostedServices)
                            services.Remove(service);
                    });
                });
        }

        [TearDown]
        public void TearDown() => _factory?.Dispose();

        [Test]
        public async Task SSE_ReadyEventAndMockedToolResult_WorkCorrectly()
        {
            var client = _factory.CreateClient();

            //1.Initialize session
            var initResponse = await client.PostAsync("/mcp/initialize", null);
            var initJson = await initResponse.Content.ReadAsStringAsync();
            var initData = JsonSerializer.Deserialize<InitializeResponse>(initJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.IsNotNull(initData);
            var sessionId = initData.SessionId;

            //2.Mock HttpClient for tool
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.SetupRequest(HttpMethod.Get, "https://example.com/health")
                       .ReturnsResponse(HttpStatusCode.OK);
            var mockedHttpClient = mockHandler.CreateClient();
            var logger = Mock.Of<ILogger<CheckApiStatusTool>>();
            var tool = new CheckApiStatusTool(mockedHttpClient, logger);

            //3.Enqueue tool result in the real SessionService
            var scope = _factory.Services.CreateScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<SessionService>();
            var toolResult = await tool.ExecuteAsync("https://example.com/health");
            sessionService.EnqueueResponse(sessionId, toolResult);

            // 4.Start SSE stream
            var sseRequest = new HttpRequestMessage(HttpMethod.Get, $"/mcp/sse/{sessionId}");
            var sseResponse = await client.SendAsync(sseRequest, HttpCompletionOption.ResponseHeadersRead);
            sseResponse.EnsureSuccessStatusCode();

            using var stream = await sseResponse.Content.ReadAsStreamAsync();
            using var reader = new System.IO.StreamReader(stream);

            // 5.Read handshake event
            string firstLine = await reader.ReadLineAsync();
            Assert.IsTrue(firstLine.Contains("event: mcp.ready"));

            // 6.Read tool result event (with timeout)
            bool toolResultReceived = false;
            var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));

            while (!cts.Token.IsCancellationRequested)
            {
                string line = await reader.ReadLineAsync();
                if (line == null) break;

                if (line.StartsWith("event: mcp.tool.result"))
                {
                    var dataLine = await reader.ReadLineAsync();
                    Assert.IsTrue(dataLine.StartsWith("data:"));
                    var jsonData = dataLine.Substring("data:".Length).Trim();

                    using var doc = JsonDocument.Parse(jsonData);
                    var status = doc.RootElement.GetProperty("status").GetString();
                    Assert.AreEqual("UP", status);

                    toolResultReceived = true;
                    break;
                }
            }

            Assert.IsTrue(toolResultReceived, "Tool result should appear in SSE stream");
        }
    }
}
