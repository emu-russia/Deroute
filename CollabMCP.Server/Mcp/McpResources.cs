using System.Text.Json;
using CollabMCP.Server.Models;
using CollabMCP.Server.Services;

namespace CollabMCP.Server.Mcp;

public class McpResources
{
    private readonly SessionManager _sessionManager;
    private readonly ILogger<McpResources> _logger;

    public McpResources(SessionManager sessionManager, ILogger<McpResources> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public (bool Found, string Content, string? MimeType) GetResource(string uri)
    {
        // mcp://sessions/{sessionId}/canvas
        if (uri.StartsWith("mcp://sessions/") && uri.EndsWith("/canvas"))
        {
            var sessionId = uri.Replace("mcp://sessions/", "").Replace("/canvas", "");
            return GetCanvas(sessionId);
        }

        // mcp://sessions/{sessionId}/history
        if (uri.StartsWith("mcp://sessions/") && uri.EndsWith("/history"))
        {
            var sessionId = uri.Replace("mcp://sessions/", "").Replace("/history", "");
            return GetHistory(sessionId);
        }

        return (false, string.Empty, null);
    }

    private (bool, string, string?) GetCanvas(string sessionId)
    {
        if (!_sessionManager.TryGetSession(sessionId, out var state))
        {
            return (false, JsonSerializer.Serialize(new { error = "Session not found" }), "application/json");
        }

        var canvas = new
        {
            session = state.Metadata,
            primitives = state.Primitives.Values.Select(p => new
            {
                p.Id,
                p.Type,
                p.Points,
                p.StrokeColor,
                p.StrokeWidth,
                p.FillColor,
                p.CreatedBy,
                LockedBy = p.LockedBy ?? "none",
                LockedAt = p.LockedAt?.ToString("o") ?? "",
                p.Version,
                p.CreatedAt,
                p.UpdatedAt
            }).ToList(),
            connectedUsers = state.ConnectedUsers.ToList(),
            timestamp = DateTime.UtcNow.ToString("o")
        };

        return (true, JsonSerializer.Serialize(canvas, new JsonSerializerOptions { WriteIndented = true }), "application/json");
    }

    private (bool, string, string?) GetHistory(string sessionId)
    {
        if (!_sessionManager.TryGetSession(sessionId, out var state))
        {
            return (false, JsonSerializer.Serialize(new { error = "Session not found" }), "application/json");
        }

        var history = _sessionManager.GetHistory(sessionId, 200);

        var result = new
        {
            sessionId,
            entries = history.Select(e => new
            {
                e.Operation,
                e.PrimitiveId,
                e.UserId,
                e.Timestamp,
                e.Details
            }).ToList(),
            totalEntries = state.History.Count,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        return (true, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }), "application/json");
    }

    public List<(string Uri, string Name, string Description, string MimeType)> ListResources()
    {
        var sessionIds = _sessionManager.GetSessionIds();
        var resources = new List<(string, string, string, string)>();

        foreach (var sessionId in sessionIds)
        {
            resources.Add(($"mcp://sessions/{sessionId}/canvas",
                $"Canvas - {sessionId}",
                "Current canvas state with all primitives and metadata",
                "application/json"));

            resources.Add(($"mcp://sessions/{sessionId}/history",
                $"History - {sessionId}",
                "Operation history log for the session",
                "application/json"));
        }

        return resources;
    }
}
