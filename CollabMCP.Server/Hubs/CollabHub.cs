using System.Collections.Concurrent;
using System.Text;
using CollabMCP.Server.Models;
using CollabMCP.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace CollabMCP.Server.Hubs;

public class ClientInfo
{
    public string ConnectionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
}

public class CollabHub : Hub
{
    private readonly SessionManager _sessionManager;
    private readonly ILogger<CollabHub> _logger;
    private readonly int _throttleIntervalMs;
    private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, List<PrimitiveDelta>>> _pendingUpdates = new();

    public CollabHub(SessionManager sessionManager, ILogger<CollabHub> logger, IConfiguration config)
    {
        _sessionManager = sessionManager;
        _logger = logger;
        _throttleIntervalMs = config.GetValue<int>("Server:ThrottleIntervalMs", 33);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var client = _clients.TryRemove(Context.ConnectionId, out var clientInfo);
        if (client && clientInfo != null)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}, user {UserId}", Context.ConnectionId, clientInfo.UserId);
            _sessionManager.RemoveUserFromSession(clientInfo.SessionId, clientInfo.UserId);

            await Clients.Group(clientInfo.SessionId).SendAsync("OnUserLeft", clientInfo.UserId);

            // Send pending updates for this user's locked primitives
            FlushPendingUpdates(clientInfo.SessionId, clientInfo.UserId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinSession(string sessionId, string userId)
    {
        _logger.LogInformation("User {UserId} joining session {SessionId} via connection {ConnectionId}",
            userId, sessionId, Context.ConnectionId);

        var existingClient = _clients.FirstOrDefault(c => c.Value.UserId == userId);
        if (existingClient.Key != null)
        {
            await Clients.Clients(existingClient.Key).SendAsync("OnSessionError", "User already connected from another connection");
            return;
        }

        _clients[Context.ConnectionId] = new ClientInfo
        {
            ConnectionId = Context.ConnectionId,
            UserId = userId,
            SessionId = sessionId
        };

        _sessionManager.AddUserToSession(sessionId, userId);
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

        var snapshot = _sessionManager.GetOrCreateSession(sessionId);

        await Clients.Caller.SendAsync("OnUserJoined", new
        {
            UserId = userId,
            Snapshot = new
            {
                Metadata = snapshot.Metadata,
                Primitives = snapshot.Primitives.Values.Select(p => SerializePrimitive(p)),
                ConnectedUsers = snapshot.ConnectedUsers.ToList()
            }
        });

        await Clients.Group(sessionId).SendAsync("OnUserJoined", userId);

        _logger.LogInformation("User {UserId} joined session {SessionId}", userId, sessionId);
    }

    public async Task SendPrimitiveCreated(string sessionId, string primitiveId, string type,
        List<double> points, string strokeColor, double strokeWidth, string fillColor, string userId)
    {
        var prim = new VectorPrimitive
        {
            Id = primitiveId,
            Type = type,
            Points = points.Select((p, i) => i % 2 == 0
                ? new Models.Point { X = p, Y = points[Math.Min(i + 1, points.Count - 1)] }
                : new Models.Point { X = p, Y = 0 }).ToList(),
            StrokeColor = strokeColor,
            StrokeWidth = strokeWidth,
            FillColor = fillColor
        };

        // Reconstruct points properly
        prim.Points = new List<Models.Point>();
        for (int i = 0; i < points.Count; i += 2)
        {
            prim.Points.Add(new Models.Point
            {
                X = points[i],
                Y = i + 1 < points.Count ? points[i + 1] : 0
            });
        }

        if (!_clients.TryGetValue(Context.ConnectionId, out var clientInfo) || clientInfo.SessionId != sessionId)
            return;

        var result = _sessionManager.AddPrimitive(sessionId, prim, clientInfo.UserId);

        if (result.Error != null)
        {
            await Clients.Caller.SendAsync("OnPrimitiveError", result.Error);
            return;
        }

        var serialized = SerializePrimitive(result.Primitive!);
        await Clients.Group(sessionId).SendAsync("OnPrimitiveCreated", serialized);
        _logger.LogDebug("Primitive created: {PrimitiveId} in session {SessionId}", primitiveId, sessionId);
    }

    public async Task SendPrimitiveUpdated(string sessionId, string primitiveId, string type,
        List<double> points, string strokeColor, double strokeWidth, string fillColor, string userId)
    {
        if (!_clients.TryGetValue(Context.ConnectionId, out var clientInfo) || clientInfo.SessionId != sessionId)
            return;

        var existing = _sessionManager.GetPrimitive(sessionId, primitiveId);
        if (existing == null)
        {
            await Clients.Caller.SendAsync("OnPrimitiveError", "Primitive not found");
            return;
        }

        var updated = new VectorPrimitive
        {
            Id = primitiveId,
            Type = type,
            Points = new List<Models.Point>(),
            StrokeColor = strokeColor,
            StrokeWidth = strokeWidth,
            FillColor = fillColor
        };

        for (int i = 0; i < points.Count; i += 2)
        {
            updated.Points.Add(new Models.Point
            {
                X = points[i],
                Y = i + 1 < points.Count ? points[i + 1] : 0
            });
        }

        var result = _sessionManager.UpdatePrimitive(sessionId, primitiveId, updated, clientInfo.UserId);

        if (result.Error != null)
        {
            await Clients.Caller.SendAsync("OnPrimitiveError", result.Error);
            return;
        }

        var serialized = SerializePrimitive(result.Primitive!);
        await Clients.Group(sessionId).SendAsync("OnPrimitiveUpdated", serialized);
        _logger.LogDebug("Primitive updated: {PrimitiveId} in session {SessionId}", primitiveId, sessionId);
    }

    public async Task SendPositionUpdate(string sessionId, string primitiveId, List<double> points, string userId)
    {
        if (!_clients.TryGetValue(Context.ConnectionId, out var clientInfo) || clientInfo.SessionId != sessionId)
            return;

        // Throttle: buffer updates
        var key = $"{sessionId}_{primitiveId}_{clientInfo.UserId}";
        var deltas = _pendingUpdates.GetOrAdd(key, _ => new ConcurrentDictionary<string, List<PrimitiveDelta>>());
        var sessionDeltas = new ConcurrentDictionary<string, List<PrimitiveDelta>>();

        if (!deltas.TryGetValue(sessionId, out var sessionList))
        {
            sessionList = new List<PrimitiveDelta>();
            deltas[sessionId] = sessionList;
        }

        sessionList.Add(new PrimitiveDelta
        {
            PrimitiveId = primitiveId,
            Points = points,
            Timestamp = DateTime.UtcNow
        });

        // Flush immediately for now (throttle would be done via timer in production)
        FlushPendingUpdates(sessionId, clientInfo.UserId);
    }

    public async Task LockPrimitive(string sessionId, string primitiveId)
    {
        if (!_clients.TryGetValue(Context.ConnectionId, out var clientInfo) || clientInfo.SessionId != sessionId)
            return;

        var result = _sessionManager.TryLockPrimitive(sessionId, primitiveId, clientInfo.UserId);

        if (result.Error != null)
        {
            await Clients.Caller.SendAsync("OnLockError", new
            {
                PrimitiveId = primitiveId,
                Error = result.Error
            });
            return;
        }

        var serialized = SerializePrimitive(result.Primitive!);
        await Clients.Group(sessionId).SendAsync("OnPrimitiveLocked", serialized);
        _logger.LogDebug("Primitive locked: {PrimitiveId} by {UserId}", primitiveId, clientInfo.UserId);
    }

    public async Task UnlockPrimitive(string sessionId, string primitiveId)
    {
        if (!_clients.TryGetValue(Context.ConnectionId, out var clientInfo) || clientInfo.SessionId != sessionId)
            return;

        var prim = _sessionManager.UnlockPrimitive(sessionId, primitiveId, clientInfo.UserId);
        var serialized = SerializePrimitive(prim);
        await Clients.Group(sessionId).SendAsync("OnPrimitiveUnlocked", serialized);
        _logger.LogDebug("Primitive unlocked: {PrimitiveId}", primitiveId);
    }

    public async Task<Dictionary<string, object>> GetSessionState(string sessionId)
    {
        if (!_clients.TryGetValue(Context.ConnectionId, out var clientInfo) || clientInfo.SessionId != sessionId)
            return new();

        var session = _sessionManager.GetOrCreateSession(sessionId);
        return new Dictionary<string, object>
        {
            ["metadata"] = session.Metadata,
            ["primitives"] = session.Primitives.Values.Select(SerializePrimitive).ToList(),
            ["connectedUsers"] = session.ConnectedUsers.ToList()
        };
    }

    public async Task<List<OperationLogEntry>> GetHistory(string sessionId, int count = 50)
    {
        if (!_clients.TryGetValue(Context.ConnectionId, out var clientInfo) || clientInfo.SessionId != sessionId)
            return new();

        return _sessionManager.GetHistory(sessionId, count);
    }

    public async Task<List<string>> GetConnectedUsers(string sessionId)
    {
        if (!_clients.TryGetValue(Context.ConnectionId, out var clientInfo) || clientInfo.SessionId != sessionId)
            return new();

        return _sessionManager.GetConnectedUsers(sessionId).ToList();
    }

    private void FlushPendingUpdates(string sessionId, string userId)
    {
        var key = $"{sessionId}_{userId}";
        if (_pendingUpdates.TryRemove(key, out var deltas))
        {
            foreach (var sessionDeltas in deltas)
            {
                if (sessionDeltas.Value.Count == 0) continue;

                // Take the latest delta for each primitive
                var latestByPrimitive = new Dictionary<string, PrimitiveDelta>();
                foreach (var delta in sessionDeltas.Value)
                {
                    if (!latestByPrimitive.ContainsKey(delta.PrimitiveId) ||
                        delta.Timestamp > latestByPrimitive[delta.PrimitiveId].Timestamp)
                    {
                        latestByPrimitive[delta.PrimitiveId] = delta;
                    }
                }

                foreach (var kvp in latestByPrimitive)
                {
                    var pointsList = kvp.Value.Points;
                    _ = Clients.Group(sessionId).SendAsync("OnPositionUpdated", new
                    {
                        PrimitiveId = kvp.Value.PrimitiveId,
                        Points = pointsList
                    });
                }
            }

            _pendingUpdates.TryRemove(key, out _);
        }
    }

    private Dictionary<string, object> SerializePrimitive(VectorPrimitive prim)
    {
        return new Dictionary<string, object>
        {
            ["id"] = prim.Id,
            ["type"] = prim.Type,
            ["points"] = prim.Points.Select(p => new { p.X, p.Y }).ToList(),
            ["strokeColor"] = prim.StrokeColor,
            ["strokeWidth"] = prim.StrokeWidth,
            ["fillColor"] = prim.FillColor,
            ["createdBy"] = prim.CreatedBy,
            ["lockedBy"] = prim.LockedBy ?? "none",
            ["lockedAt"] = prim.LockedAt?.ToString("o") ?? "",
            ["version"] = prim.Version,
            ["createdAt"] = prim.CreatedAt.ToString("o"),
            ["updatedAt"] = prim.UpdatedAt.ToString("o")
        };
    }
}

public class PrimitiveDelta
{
    public string PrimitiveId { get; set; } = string.Empty;
    public List<double> Points { get; set; } = new();
    public DateTime Timestamp { get; set; }
}
