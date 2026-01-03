using McpHealthServer.Models;
using System.Text.Json;

namespace McpHealthServer.Endpoints;

public class JSONRPCUnifiedEndPoint
{
    private readonly ILogger<JSONRPCUnifiedEndPoint> _logger;
    private readonly McpRpcHandlers _handler;

    public JSONRPCUnifiedEndPoint(ILogger<JSONRPCUnifiedEndPoint> logger, McpRpcHandlers handlers)
    {
        _logger = logger;
        _handler = handlers;
    }

    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/mcp/unified", async (HttpContext context) =>
        {
            if (context.Request.ContentLength > 10_000_000)
            {
                _logger.LogWarning("Request too large: {ContentLength} bytes", context.Request.ContentLength);

                await context.Response.WriteAsJsonAsync(new JsonRpcResponse
                {
                    Id = 0,
                    Error = new JsonRpcError { Code = -32801, Message = "Content Too Large" }
                });

                return;
            }

            try
            {
                var request = await JsonSerializer.DeserializeAsync<JsonRpcRequest>(context.Request.Body);
                var response = await _handler.HandleAsync(request);
                await context.Response.WriteAsJsonAsync(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                _logger.LogInformation("JSON RPC request handled: Method={Method}, Id={Id}", request.Method, request.Id);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("JSON RPC request cancelled by client");
                await context.Response.WriteAsJsonAsync(new JsonRpcResponse
                {
                    Id = 0,
                    Error = new JsonRpcError { Code = Constants.ErrorCode_RequestCancelled, Message = Constants.Error_RequestCancelled }
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON RPC request");
                await context.Response.WriteAsJsonAsync(new JsonRpcResponse
                {
                    Id = 0,
                    Error = new JsonRpcError { Code = Constants.ErrorCode_ParseError, Message = Constants.Error_ParseError }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in JSON RPC request");
                await context.Response.WriteAsJsonAsync(new JsonRpcResponse
                {
                    Id = 0,
                    Error = new JsonRpcError { Code = Constants.InternalServerError_Code, Message = Constants.InternalServerError, Data = ex.Message }
                });
            }
        });

    }
}