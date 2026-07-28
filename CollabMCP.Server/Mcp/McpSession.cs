using System.Collections.Concurrent;
using System.Text.Json;

namespace CollabMCP.Server.Mcp;

public class McpSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public string WriterId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastAccess { get; set; } = DateTime.UtcNow;
}

public class McpSessionManager
{
    private readonly ConcurrentDictionary<string, McpSession> _sessions = new();
    private readonly ILogger<McpSessionManager> _logger;
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(30);

    public McpSessionManager(ILogger<McpSessionManager> logger)
    {
        _logger = logger;
    }

    public McpSession CreateSession(string writerId)
    {
        var session = new McpSession { WriterId = writerId };
        _sessions[session.SessionId] = session;
        _logger.LogInformation("MCP session created: {SessionId}", session.SessionId);
        return session;
    }

    public McpSession? GetSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.LastAccess = DateTime.UtcNow;
            return session;
        }
        return null;
    }

    public void RemoveSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out _))
        {
            _logger.LogInformation("MCP session removed: {SessionId}", sessionId);
        }
    }

    public void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _sessions)
        {
            if (now - kvp.Value.LastAccess > _timeout)
            {
                _sessions.TryRemove(kvp.Key, out _);
                _logger.LogInformation("MCP session expired: {SessionId}", kvp.Key);
            }
        }
    }

    public void Touch(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.LastAccess = DateTime.UtcNow;
        }
    }
}
