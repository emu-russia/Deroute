using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CollabMCP.Server.Hubs;
using CollabMCP.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ILogger = Serilog.ILogger;

namespace CollabMCP.Server.Mcp;

public class McpEndpoint
{
    private readonly RequestDelegate _next;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly McpSessionManager _sessionManager;
    private readonly McpResources _resources;
    private readonly McpPrompts _prompts;
    private readonly McpTools _tools;
    private readonly IHubContext<CollabHub> _hubContext;
    private readonly string _adminApiKey;
    private static readonly JsonSerializerOptions JsOptions = new() { WriteIndented = false };

    public McpEndpoint(RequestDelegate next, Microsoft.Extensions.Logging.ILogger<McpEndpoint> logger, McpSessionManager sessionManager,
        McpResources resources, McpPrompts prompts, McpTools tools,
        IHubContext<CollabHub> hubContext, IOptions<Config.ServerConfig> config)
    {
        _next = next;
        _logger = logger;
        _sessionManager = sessionManager;
        _resources = resources;
        _prompts = prompts;
        _tools = tools;
        _hubContext = hubContext;
        _adminApiKey = config.Value.AdminApiKey;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _sessionManager.CleanupExpired();

        if (context.Request.Method == HttpMethods.Post && context.Request.Path == "/mcp/sse")
        {
            await HandleSseStart(context);
            return;
        }

        if (context.Request.Method == HttpMethods.Post && context.Request.Path == "/mcp")
        {
            await HandleJsonRpc(context);
            return;
        }

        if (context.Request.Method == HttpMethods.Get && context.Request.Path == "/mcp/events")
        {
            await HandleSseStream(context);
            return;
        }

        if (context.Request.Method == HttpMethods.Get && context.Request.Path == "/mcp/resources")
        {
            await HandleListResources(context);
            return;
        }

        if (context.Request.Method == HttpMethods.Get && context.Request.Path == "/mcp/prompts")
        {
            await HandleListPrompts(context);
            return;
        }

        if (context.Request.Method == HttpMethods.Get && context.Request.Path == "/mcp/tools")
        {
            await HandleListTools(context);
            return;
        }

        await _next(context);
    }

    private async Task HandleSseStart(HttpContext context)
    {
        var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (apiKey != _adminApiKey)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }

        var writerId = context.Request.Headers["X-Mcp-Writer-Id"].FirstOrDefault() ?? "unknown";
        var session = _sessionManager.CreateSession(writerId);

        context.Response.StatusCode = 201;
        context.Response.ContentType = "application/json";
        context.Response.Headers["Cache-Control"] = "no-cache";
        context.Response.Headers["Connection"] = "keep-alive";
        context.Response.Headers["X-Accel-Buffering"] = "no";

        await context.Response.WriteAsJsonAsync(new
        {
            session = session.SessionId,
            endpoint = $"/mcp/events?session={session.SessionId}"
        }, JsOptions);

        _logger.LogInformation("MCP SSE session started: {SessionId} by {WriterId}", session.SessionId, writerId);
    }

    private async Task HandleSseStream(HttpContext context)
    {
        var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (apiKey != _adminApiKey)
        {
            context.Response.StatusCode = 403;
            return;
        }

        var sessionId = context.Request.Query["session"];
        if (string.IsNullOrEmpty(sessionId))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Missing session parameter" });
            return;
        }

        var mcpSession = _sessionManager.GetSession(sessionId);
        if (mcpSession == null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { error = "Session not found" });
            return;
        }

        context.Response.StatusCode = 200;
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers["Cache-Control"] = "no-cache";
        context.Response.Headers["Connection"] = "keep-alive";
        context.Response.Headers["X-Accel-Buffering"] = "no";

        var connected = true;

        context.Response.Body.Flush();
        await context.Response.Body.FlushAsync();

        while (connected)
        {
            try
            {
                _sessionManager.Touch(sessionId);

                var heartbeat = $"event: heartbeat\ndata: {{\"type\":\"heartbeat\",\"timestamp\":\"{DateTime.UtcNow:O}\"}}\n\n";
                await context.Response.WriteAsync(heartbeat);
                await context.Response.Body.FlushAsync();

                await Task.Delay(15000, context.RequestAborted);
            }
            catch (TaskCanceledException)
            {
                connected = false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MCP SSE stream error for session {SessionId}", sessionId);
                connected = false;
            }
        }

        _logger.LogInformation("MCP SSE stream ended for session {SessionId}", sessionId);
    }

    private async Task HandleJsonRpc(HttpContext context)
    {
        var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (apiKey != _adminApiKey)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }

        using var ms = new MemoryStream();
        await context.Request.Body.CopyToAsync(ms);
        var body = ms.ToArray();
        if (body == null || body.Length == 0)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Empty request body" });
            return;
        }

        var bodyString = Encoding.UTF8.GetString(body.ToArray());
        if (string.IsNullOrEmpty(bodyString))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Empty request body" });
            return;
        }

        McpRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<McpRequest>(bodyString);
        }
        catch
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid JSON-RPC request" });
            return;
        }

        if (request == null)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Null request" });
            return;
        }

        var callingUserId = context.Request.Headers["X-Mcp-Writer-Id"].FirstOrDefault() ?? "mcp-agent";
        var result = await ProcessRequest(request, callingUserId);

        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/json";

        if (result is McpResponse response)
        {
            await context.Response.WriteAsJsonAsync(response, JsOptions);
        }
        else
        {
            await context.Response.WriteAsJsonAsync(new { result }, JsOptions);
        }
    }

    private async Task<object> ProcessRequest(McpRequest request, string callingUserId)
    {
        return request.Method switch
        {
            "initialize" => HandleInitialize(request),
            "resources/list" => await HandleResourcesList(request),
            "resources/read" => await HandleResourceRead(request),
            "prompts/list" => await HandlePromptsList(request),
            "prompts/get" => await HandlePromptsGet(request),
            "tools/list" => await HandleToolsList(request),
            "tools/call" => await HandleToolsCall(request, callingUserId),
            "notifications/initialized" => new { },
            _ => new { error = $"Unknown method: {request.Method}" }
        };
    }

    private object HandleInitialize(McpRequest request)
    {
        return new McpResponse(
            id: request.GetId(),
            result: new
            {
                protocolVersion = "2024-11-05",
                serverInfo = new { name = "CollabMCP", version = "1.0.0" },
                capabilities = new
                {
                    resources = new { listChanged = true },
                    prompts = new { listChanged = true },
                    tools = new { listChanged = true }
                }
            }
        );
    }

    private async Task<object> HandleResourcesList(McpRequest request)
    {
        var resources = _resources.ListResources();
        return new McpResponse(request.GetId(), new
        {
            resources = resources.Select(r => new
            {
                r.Uri,
                r.Name,
                r.Description,
                r.MimeType
            }).ToList()
        });
    }

    private async Task<object> HandleResourceRead(McpRequest request)
    {
        if (request.Params is not JsonElement paramsEl)
            return new { error = "Missing parameters" };

        var uri = paramsEl.TryGetProperty("uri", out var uriEl) ? (uriEl.GetString() ?? string.Empty) : string.Empty;
        var (found, content, mimeType) = _resources.GetResource(uri);

        if (!found)
            return new { error = $"Resource not found: {uri}" };

        return new McpResponse(request.GetId(), new
        {
            contents = new[] { new { uri, name = uri, mimeType, content } }
        });
    }

    private async Task<object> HandlePromptsList(McpRequest request)
    {
        var promptList = _prompts.ListPrompts();
        return new McpResponse(request.GetId(), new
        {
            prompts = promptList.Select(p => new { p.Name, p.Description }).ToList()
        });
    }

    private async Task<object> HandlePromptsGet(McpRequest request)
    {
        if (request.Params is not JsonElement paramsEl)
            return new { error = "Missing parameters" };

        var name = paramsEl.TryGetProperty("name", out var nameEl) ? (nameEl.GetString() ?? string.Empty) : string.Empty;
        var arguments = new Dictionary<string, object>();

        if (paramsEl.TryGetProperty("arguments", out var argsEl) && argsEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in argsEl.EnumerateObject())
            {
                arguments[prop.Name] = prop.Value;
            }
        }

        var (found, promptName, description, message, args) = _prompts.GetPrompt(name, arguments);

        if (!found)
            return new { error = $"Prompt not found: {name}" };

        return new McpResponse(request.GetId(), new
        {
            name = promptName,
            description,
            message = new { role = "user", content = message ?? string.Empty },
            arguments = args
        });
    }

    private async Task<object> HandleToolsList(McpRequest request)
    {
        var toolList = _tools.ListTools();
        return new McpResponse(request.GetId(), new
        {
            tools = toolList.Select(t => new
            {
                t.Name,
                t.Description,
                inputSchema = t.Schema
            }).ToList()
        });
    }

    private async Task<object> HandleToolsCall(McpRequest request, string callingUserId)
    {
        if (request.Params is not JsonElement paramsEl)
            return new { error = "Missing parameters" };

        var name = paramsEl.TryGetProperty("name", out var nameEl) ? (nameEl.GetString() ?? string.Empty) : string.Empty;
        var arguments = new Dictionary<string, object>();

        if (paramsEl.TryGetProperty("arguments", out var argsEl) && argsEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in argsEl.EnumerateObject())
            {
                arguments[prop.Name] = prop.Value;
            }
        }

        if (string.IsNullOrEmpty(name))
            return new { error = "Missing tool name" };

        try
        {
            var result = await _tools.CallTool(name, arguments, callingUserId);
            var contentObj = JsonSerializer.Deserialize<JsonElement>(result);
            return new McpResponse(request.GetId(), new { content = contentObj });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP tool call error: {ToolName}", name);
            return new McpResponse(request.GetId(), new
            {
                content = new[] { new { type = "text", text = $"Error: {ex.Message}" } },
                isError = true
            });
        }
    }

    private async Task HandleListResources(HttpContext context)
    {
        var resources = _resources.ListResources();
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { resources }, JsOptions);
    }

    private async Task HandleListPrompts(HttpContext context)
    {
        var prompts = _prompts.ListPrompts();
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { prompts }, JsOptions);
    }

    private async Task HandleListTools(HttpContext context)
    {
        var tools = _tools.ListTools();
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { tools }, JsOptions);
    }
}

public class McpRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }

    public string? GetId()
    {
        if (Id == null) return null;
        if (Id.Value.ValueKind == JsonValueKind.String)
            return Id.Value.GetString();
        return Id.Value.GetRawText();
    }
}

public class McpResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    public object? Error { get; set; }

    public McpResponse(string? id, object? result = null, object? error = null)
    {
        Id = id;
        Result = result;
        Error = error;
    }
}
