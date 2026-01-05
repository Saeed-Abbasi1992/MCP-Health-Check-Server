# Standalone MCP Health Check Server (ASP.NET Core)

This project implements a **Standalone MCP (Model Context Protocol) Server** that allows AI models to query the health status of cloud services over the public internet in a **standard, testable way** using **HTTP + Server-Sent Events (SSE)**.

The implementation is designed specifically to satisfy the requirements of the MCP Health Check challenge and focuses on:
- MCP lifecycle compliance (Initialize → Handshake → Tool Invocation)
- Multi-client session management
- Secure and reliable health probing (`check_api_status` tool)
- SSRF protection via domain allowlisting

---

##  Features

- ✅ Full MCP lifecycle over HTTP + SSE
- ✅ Multi-client concurrent sessions with isolation
- ✅ `check_api_status` tool for HTTP health checks
- ✅ Session-bound SSE streaming (no cross-talk)
- ✅ Configurable request timeout
- ✅ SSRF protection using domain allowlist
- ✅ Automatic session expiration (TTL)
- ✅ Clean, testable ASP.NET Core (.NET 8) architecture

---

##  MCP Lifecycle Overview

1. **Initialize (HTTP)**
   - Client starts a new MCP session
   - Server returns `session_id`, capabilities, available tools, and SSE URL

2. **Handshake (HTTP + SSE)**
   - Client connects to the SSE stream using `session_id`
   - Server emits handshake/ready events

3. **Tool Invocation (HTTP)**
   - Client invokes `check_api_status`
   - Server executes the probe and streams results via SSE

---

##  API Endpoints

### 1.Initialize Session

**Endpoint**
```
POST /mcp/initialize
```

**Description**
Creates a new MCP session and returns protocol metadata.

**Response Example:**
```json
{
  "protocol": "mcp",
  "protocolVersion": "2024-11-05",
  "sessionId": "ef5066fd-2b89-40f4-acd2-6078ea8b16ce",
  "capabilities": {
    "tools": [
      {
        "name": "check_api_status",
        "inputSchema": {
          "url": "string"
        }
      }
    ]
  },
  "sseUrl": "/mcp/sse/ef5066fd-2b89-40f4-acd2-6078ea8b16ce"
}
```

---

### 2.SSE Handshake / Stream

**Endpoint**
```
GET /mcp/sse?session_id={session_id}
```

**Description**
Opens a Server-Sent Events connection bound to the given session.

**Events Emitted**
- `handshake`
- `ready`
- `tool_result`
- `error`

**Example SSE Event**
```
event: ready
data: {"message":"Session is active"}
```

---

### 3.Tool Invocation

**Endpoint**
```
POST /mcp/tools
```

**Request Body**
```json
{
  "session_id": "ef5066fd-2b89-40f4-acd2-6078ea8b16ce",
  "name": "check_api_status",
  "input": {
    "url": "api.mycompany.com/health"
  }
}
```

**Notes**
- URL may be provided **without scheme**
- `https://` is assumed by default
- Domain must be allowlisted

---

##  Tool: `check_api_status`

### Input
```json
{
  "url": "api.mycompany.com/health"
}
```

### Success Output
```json
{
  "url": "https://api.mycompany.com/health",
  "status": "UP",
  "http_status": 200,
  "latency_ms": 87,
  "checked_at": "2025-12-30T12:00:00Z"
}
```

### Failure Output
```json
{
  "url": "https://api.mycompany.com/health",
  "status": "DOWN",
  "error": "Timeout after 3000ms",
  "checked_at": "2025-12-30T12:00:00Z"
}
```

### Behavior
- Default timeout: **3000 ms** (configurable)
- Handles:
  - Timeouts
  - DNS failures
  - TLS errors
  - Non-2xx HTTP responses
- Never crashes the server

---

##  Security & SSRF Protection

To mitigate SSRF risks, the server enforces a **domain allowlist policy**:

- Only domains defined in configuration are allowed
- Subdomains are allowed (e.g. `sub.api.mycompany.com`)
- Private, loopback, and local IP ranges are blocked
- Only HTTP/HTTPS schemes are accepted

### Example Configuration

```json
{
  "UrlPolicy": {
    "AllowedDomains": [
      "api.mycompany.com"
    ]
  }
}
```

**Allowed**
- `api.mycompany.com/health`
- `https://api.mycompany.com/health`

**Blocked**
- `localhost/health`
- `127.0.0.1/health`
- `evil.com/api.mycompany.com`

---

##  How to Run

### Prerequisites
- .NET SDK 8.0+

### Build & Run

```bash
dotnet restore
dotnet build
dotnet run --project src/McpHealthServer
```

Server starts on:
```
http://localhost:5058
https://localhost:7184
```

---

## Testing

```bash
dotnet test
```

### Covered Scenarios
- Initialize returns `session_id` and tools
- SSE stream binds correctly to session
- `check_api_status` reports UP/DOWN
- Parallel sessions do not mix events

---

##  Design Notes

- Session storage is thread-safe
- SSE events are strictly session-scoped
- Input normalization (URL scheme) is applied before execution
- HttpClient is reused and timeout-controlled

---

##  Conclusion

This server provides a clean, secure, and MCP-compliant way for AI models to query service health over the internet. It is production-minded while remaining simple and testable, making it suitable both for the challenge and real-world extensions.

---

**Author:** Saeed Abbasi

