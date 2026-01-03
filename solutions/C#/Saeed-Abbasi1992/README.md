# MCP Health Server

This project implements a simplified MCP (Microservice Control Protocol) Health Server in .NET 8, providing endpoints to initialize sessions, stream tool results via SSE, and handle JSON-RPC requests. It includes session management, session cleanup, and logging.

## Table of Contents

- [Endpoints](#endpoints)
- [Session Management](#session-management)
- [Tools](#tools)
- [SSE (Server-Sent Events)](#sse-server-sent-events)
- [JSON-RPC Unified Endpoint](#json-rpc-unified-endpoint)
- [Logging](#logging)
- [Session Cleanup](#session-cleanup)
- [Testing](#testing)

## Endpoints

### Initialize

- **URL:** `/mcp/initialize`
- **Method:** `POST`
- **Description:** Creates a new session and returns capabilities.
- **Response Example:**
```json
{
  "protocol": "mcp",
  "protocolVersion": "2024-11-05",
  "sessionId": "b3a6f1f2-3e7b-4b1d-8cfa-2f5d3c4a9e1f",
  "capabilities": {
    "tools": [
      { "name": "check_api_status", "inputSchema": { "url": "string" } }
    ]
  },
  "sseUrl": "/mcp/sse/b3a6f1f2-3e7b-4b1d-8cfa-2f5d3c4a9e1f"
}
```

### Tool Execution

- **URL:** `/mcp/tools/`
- **Method:** `POST`
- **Description:** Sends a tool request for a given session.
- **Request Example:**
```json
{
  "sessionId": "b3a6f1f2-3e7b-4b1d-8cfa-2f5d3c4a9e1f",
  "name": "check_api_status",
  "input": {
    "url": "https://example.com/health"
  }
}
```
- **Response Example:**  
  - HTTP 202 Accepted
```json
{
  "accepted": true,
  "sessionId": "b3a6f1f2-3e7b-4b1d-8cfa-2f5d3c4a9e1f"
}
```

### SSE (Server-Sent Events)

- **URL:** `/mcp/sse/{sessionId}`
- **Method:** `GET`
- **Description:** Streams handshake and tool results for a session.
- **Handshake Event:**
```text
event: mcp.ready
data: {"sessionId":"b3a6f1f2-3e7b-4b1d-8cfa-2f5d3c4a9e1f","message":"Session active, send tool requests to /mcp/tool"}
```
- **Tool Result Event:**
```json
{
  "url": "https://example.com/health",
  "status": "UP",
  "http_status": 200,
  "latency_ms": 87,
  "checked_at": "2025-12-30T12:00:00Z"
}
```
- **Failure Example:**
```json
{
  "url": "https://example.com/health",
  "status": "DOWN",
  "error": "Timeout after 3000ms",
  "checked_at": "2025-12-30T12:00:00Z"
}
```

### JSON-RPC Unified Endpoint

- **URL:** `/mcp/unified`
- **Method:** `POST`
- **Description:** Handles JSON-RPC requests for any MCP operation.
- **Request Example:**
```json
{
  "jsonrpc": "2.0",
  "method": "check_api_status",
  "params": {
    "sessionId": "b3a6f1f2-3e7b-4b1d-8cfa-2f5d3c4a9e1f",
    "name": "check_api_status",
    "input": { "url": "https://example.com/health" }
  },
  "id": 1
}
```

- **Error Codes:**
  - `-32700`: Parse Error
  - `-32600`: Invalid Request
  - `-32601`: Method Not Found
  - `-32602`: Params Invalid
  - `-32603`: Internal Error
  - `-32800`: Request Cancelled
  - `-32801`: Content Too Large

## Session Management

- Sessions are stored in memory using `SessionService`.
- Each session has:
  - `SessionId`
  - Queue of tool results (`ConcurrentQueue<CheckHealthResponseBase>`)
  - `LastActive` timestamp
- Sessions support enqueue/dequeue of tool results for SSE streaming.

## Tools

- Currently implemented: `CheckApiStatusTool`
- Checks HTTP/HTTPS endpoint availability.
- Returns `UpStatusResponse` or `DownStatusResponse`.

## Logging

- All endpoints log requests, warnings, errors, and tool execution.
- SSE handshake and tool results are logged.

## Session Cleanup

- Implemented as a background `IHostedService` (`SessionCleanupService`).
- Periodically removes sessions inactive beyond a TTL (default 30 minutes).
- Logs cleanup operations.

## Testing

- Unit tests: NUnit
- Integration tests: SSE handshake, tool execution, JSON-RPC
- Use `Moq` for HTTP client mocking.

## Notes

- Ensure URLs follow the policy: no private IPs or loopback addresses.
- SSE events are session-bound; each session has its own queue.
- JSON serialization uses camelCase for consistency.

