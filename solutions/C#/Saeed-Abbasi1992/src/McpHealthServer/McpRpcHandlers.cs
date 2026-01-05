using McpHealthServer;
using McpHealthServer.Models;
using McpHealthServer.Models.Iniitialize;
using McpHealthServer.Tools;
using System.Text.Json;

public class McpRpcHandlers
{
    private readonly SessionService _sessionManager;
    private readonly CheckApiStatusTool _tool;
    private readonly UrlPolicyService _policy;

    public McpRpcHandlers(SessionService sessionManager, CheckApiStatusTool tool, UrlPolicyService policy)
    {
        _sessionManager = sessionManager;
        _tool = tool;
        _policy = policy;
    }

    public async Task<JsonRpcResponse> HandleAsync(JsonRpcRequest request)
    {
        if (request is null)
            return new JsonRpcResponse { Id = 0, Error = new JsonRpcError { Code = Constants.ErrorCode_ParseError, Message = Constants.Error_ParseError } };

        if (string.IsNullOrWhiteSpace(request.Method))
            return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = Constants.ErrorCode_InvlidRequest, Message = Constants.Error_InvalidRequest } };

        try
        {
            switch (request.Method)
            {
                case "initialize":
                    var initializeRequest = JsonSerializer.Deserialize<InitializeRequest>(request.Params.GetRawText());
                    var session = _sessionManager.CreateSession();
                    var result = _sessionManager.CreateInitializeResponse(session);
                    return new JsonRpcResponse { Id = request.Id, Result = result };

                case Constants.CheckApiStatus:
                    var toolRequest = JsonSerializer.Deserialize<ToolRequest>(request.Params.GetRawText());

                    if (!_sessionManager.Sessions.ContainsKey(toolRequest.SessionId))
                        return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = -32601, Message = "Session not found" } };

                    if (!_policy.IsAllowed(toolRequest.Input.Url, out var reason))
                        return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = Constants.ErrorCode_CustomeServerError, Message = reason } };

                    var toolResult = await _tool.ExecuteAsync(toolRequest.Input.Url);
                    _sessionManager.EnqueueResponse(toolRequest.SessionId, toolResult);

                    return new JsonRpcResponse { Id = request.Id, Result = new { accepted = true, sessionId = toolRequest.SessionId } };

                default:
                    return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = Constants.ErrorCode_InvalidMethod, Message = Constants.Error_InvalidMethod } };
            }
        }
        catch (JsonException)
        {
            return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = Constants.ErrorCode_InvalidParams, Message = Constants.Error_InvalidParams } };
        }
        catch (Exception ex)
        {
            return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = Constants.InternalServerError_Code, Message = Constants.InternalServerError, Data = ex.Message } };
        }
    }
}
