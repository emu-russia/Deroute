using CollabMCP.Server.Config;
using Microsoft.Extensions.Options;

namespace CollabMCP.Server.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;
    private readonly string _adminApiKey;

    public ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger, IOptions<ServerConfig> config)
    {
        _next = next;
        _logger = logger;
        _adminApiKey = config.Value.AdminApiKey;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Allow health check without API key
        if (context.Request.Path.StartsWithSegments("/api/health"))
        {
            await _next(context);
            return;
        }

        var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Request without API key from {RemoteIp} to {Path}", 
                context.Connection.RemoteIpAddress, context.Request.Path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API key required" });
            return;
        }

        if (apiKey != _adminApiKey)
        {
            _logger.LogWarning("Invalid API key from {RemoteIp} to {Path}", 
                context.Connection.RemoteIpAddress, context.Request.Path);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }

        context.Items["ApiKeyValid"] = true;
        await _next(context);
    }
}
