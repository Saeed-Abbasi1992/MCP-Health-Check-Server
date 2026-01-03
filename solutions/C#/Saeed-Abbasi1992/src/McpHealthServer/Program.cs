using McpHealthServer;
using McpHealthServer.Endpoints;
using McpHealthServer.Services;
using McpHealthServer.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SessionService>();

builder.Services.Configure<SessionCleanupOptions>(builder.Configuration.GetSection(Constants.Config_SessionCleanUp_Key));

builder.Services.AddHostedService<SessionCleanUpJob>();
builder.Services.AddSingleton<UrlPolicyService>();

builder.Services.AddSingleton<McpRpcHandlers>();

builder.Services.AddSingleton<InitializeEndpoint>();
builder.Services.AddSingleton<SSEEndpoint>();
builder.Services.AddSingleton<ToolEndpoint>();
builder.Services.AddSingleton<JSONRPCUnifiedEndPoint>();

builder.Services.AddHttpClient<CheckApiStatusTool>();

var app = builder.Build();

var initializeEndpoint = app.Services.GetRequiredService<InitializeEndpoint>();
initializeEndpoint.MapEndpoint(app);

var sseEndpoint = app.Services.GetRequiredService<SSEEndpoint>();
sseEndpoint.MapEndpoint(app);

var toolEndpoint = app.Services.GetRequiredService<ToolEndpoint>();
toolEndpoint.MapEndpoint(app);

var jsonRpcEndpoint = app.Services.GetRequiredService<JSONRPCUnifiedEndPoint>();
jsonRpcEndpoint.MapEndpoint(app);


app.Run();

public partial class Program { }