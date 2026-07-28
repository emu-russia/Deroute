using CollabMCP.Server.Config;
using CollabMCP.Server.Hubs;
using CollabMCP.Server.Middleware;
using CollabMCP.Server.Services;
using Serilog;
using Mcp = CollabMCP.Server.Mcp;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var serverConfig = config.GetSection("Server").Get<ServerConfig>() ?? new ServerConfig();

// Ensure directories exist
Directory.CreateDirectory(serverConfig.XmlStoragePath);
Directory.CreateDirectory(serverConfig.LogPath);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(serverConfig.LogPath, "collabmcp-.log"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30
    )
    .CreateLogger();

try
{
    Log.Information("Starting CollabMCP Server on {Url}:{Port}...", serverConfig.Url, serverConfig.Port);

    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddConfiguration(config);

    builder.Services.Configure<ServerConfig>(config.GetSection("Server"));
    builder.Services.AddSingleton<IConfiguration>(config);
    builder.Services.AddSingleton<XmlSessionStore>();
    builder.Services.AddSingleton<SessionManager>();
    builder.Services.AddSignalR();

    // MCP services
    builder.Services.AddSingleton<Mcp.McpSessionManager>();
    builder.Services.AddSingleton<Mcp.McpResources>();
    builder.Services.AddSingleton<Mcp.McpPrompts>();
    builder.Services.AddSingleton<Mcp.McpTools>();

    var app = builder.Build();

    app.UseMiddleware<ApiKeyAuthMiddleware>();

    // MCP endpoint - handles all /mcp routes internally
    app.UseMiddleware<Mcp.McpEndpoint>();

    app.MapHub<CollabHub>("/collabhub");

    app.MapGet("/api/sessions", (SessionManager sessionManager) =>
    {
        var sessionIds = sessionManager.GetSessionIds();
        return Results.Ok(sessionIds);
    });

    app.MapGet("/api/sessions/{sessionId}", (string sessionId, SessionManager sessionManager) =>
    {
#pragma warning disable CS8602
        if (sessionManager.TryGetSession(sessionId, out var state) && state.Metadata is { } metadata)
#pragma warning restore CS8602
        {
            return Results.Ok(new
            {
                Metadata = metadata,
                PrimitiveCount = state.Primitives.Count,
                ConnectedUsers = state.ConnectedUsers.ToList()
            });
        }
        return Results.NotFound();
    });

    app.MapDelete("/api/sessions/{sessionId}", (string sessionId, SessionManager sessionManager) =>
    {
        sessionManager.RemoveSession(sessionId);
        return Results.Ok();
    });

    app.MapGet("/api/health", () => Results.Ok(new { status = "ok", uptime = DateTime.UtcNow.ToString("o") }));

    app.Run($"{serverConfig.Url}:{serverConfig.Port}");
}
catch (Exception ex)
{
    Log.Fatal(ex, "CollabMCP Server failed to start");
}
finally
{
    Log.Information("CollabMCP Server stopped");
    Log.CloseAndFlush();
}
