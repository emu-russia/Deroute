using System.Collections.Concurrent;
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
    // NOTE: SignalR hub instances are transient (a new instance is created for each
    // client method invocation), so any per-instance state is lost between calls.
    // The connection registry therefore lives in a static (process-wide) collection;
    // position-update buffering lives in the singleton PositionUpdateFlusher service.
    private readonly SessionManager _sessionManager;
    private readonly PositionUpdateFlusher _positionFlusher;
    private readonly ILogger<CollabHub> _logger;
    private static readonly ConcurrentDictionary<string, ClientInfo> _clients = new();

    public CollabHub(SessionManager sessionManager, PositionUpdateFlusher positionFlusher, ILogger<CollabHub> logger)
    {
        _sessionManager = sessionManager;
        _positionFlusher = positionFlusher;
        _logger = logger;
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

            // Flush any buffered position updates for the departing user
            _positionFlusher.FlushSession(clientInfo.SessionId, clientInfo.UserId);
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
                Entities = snapshot.Entities.Select(EntityDtoConverter.ToDto).ToList(),
                ConnectedUsers = snapshot.ConnectedUsers.ToList()
            }
        });

        await Clients.Group(sessionId).SendAsync("OnUserJoined", userId);

        _logger.LogInformation("User {UserId} joined session {SessionId}", userId, sessionId);
    }

    public async Task SendEntityCreated(string sessionId, EntityDto entity, string userId, string parentId)
    {
        if (!_clients.TryGetValue(Context.ConnectionId, out var clientInfo) || clientInfo.SessionId != sessionId)
            return;

        var node = EntityDtoConverter.ToNode(entity);
        var result = _sessionManager.AddEntity(sessionId, node, clientInfo.UserId,
            string.IsNullOrEmpty(parentId) ? null : parentId);

        if (result.Error != null)
        {
            await Clients.Caller.SendAsync("OnEntityError", result.Error);
            return;
        }

        var dto = EntityDtoConverter.ToDto(result.Entity!);
        await Clients.Group(sessionId).SendAsync("OnEntityCreated", dto);
        _logger.LogDebug("Entity created: {EntityId} in session {SessionId}", node.Id, sessionId);
    }

    public async Task SendEntityUpdated(string sessionId, EntityDto entity, string userId)
    {
        if (!_clients.TryGetValue(Context.ConnectionId, out var clientInfo) || clientInfo.SessionId != sessionId)
            return;

        var node = EntityDtoConverter.ToNode(entity);
        var result = _sessionManager.UpdateEntity(sessionId, node.Id, node, clientInfo.UserId);

        if (result.Error != null)
        {
            await Clients.Caller.SendAsync("OnEntityError", result.Error);
            return;
        }

        var dto = EntityDtoConverter.ToDto(result.Entity!);
        await Clients.Group(sessionId).SendAsync("OnEntityUpdated", dto);
        _logger.LogDebug("Entity updated: {EntityId} in session {SessionId}", node.Id, sessionId);
    }

    public async Task SendEntityDeleted(string sessionId, string entityId)
    {
        if (!_clients.TryGetValue(Context.ConnectionId, out var clientInfo) || clientInfo.SessionId != sessionId)
            return;

        var result = _sessionManager.DeleteEntity(sessionId, entityId, clientInfo.UserId);

        if (!result.Success)
        {
            await Clients.Caller.SendAsync("OnEntityError", result.Error);
            return;
        }

        await Clients.Group(sessionId).SendAsync("OnEntityDeleted", entityId);
        _logger.LogDebug("Entity deleted: {EntityId} in session {SessionId}", entityId, sessionId);
    }

    public async Task SendPositionUpdate(string sessionId, string entityId, List<double> points, string userId)
    {
        if (!_clients.TryGetValue(Context.ConnectionId, out var clientInfo) || clientInfo.SessionId != sessionId)
            return;

        // Apply the delta to the stored entity (transient: no version bump, no XML write)
        _sessionManager.ApplyPositionUpdate(sessionId, entityId, points);

        // Buffer for the throttled broadcast to the session group
        _positionFlusher.Buffer(sessionId, clientInfo.UserId, entityId, points);
    }

    public async Task LockEntity(string sessionId, string entityId)
    {
        if (!_clients.TryGetValue(Context.ConnectionId, out var clientInfo) || clientInfo.SessionId != sessionId)
            return;

        var result = _sessionManager.TryLockEntity(sessionId, entityId, clientInfo.UserId);

        if (result.Error != null)
        {
            await Clients.Caller.SendAsync("OnLockError", new
            {
                EntityId = entityId,
                Error = result.Error
            });
            return;
        }

        var dto = EntityDtoConverter.ToDto(result.Entity!);
        await Clients.Group(sessionId).SendAsync("OnEntityLocked", dto);
        _logger.LogDebug("Entity locked: {EntityId} by {UserId}", entityId, clientInfo.UserId);
    }

    public async Task UnlockEntity(string sessionId, string entityId)
    {
        if (!_clients.TryGetValue(Context.ConnectionId, out var clientInfo) || clientInfo.SessionId != sessionId)
            return;

        var node = _sessionManager.UnlockEntity(sessionId, entityId, clientInfo.UserId);
        var dto = EntityDtoConverter.ToDto(node);
        await Clients.Group(sessionId).SendAsync("OnEntityUnlocked", dto);
        _logger.LogDebug("Entity unlocked: {EntityId}", entityId);
    }

    public async Task<Dictionary<string, object>> GetSessionState(string sessionId)
    {
        if (!_clients.TryGetValue(Context.ConnectionId, out var clientInfo) || clientInfo.SessionId != sessionId)
            return new();

        var session = _sessionManager.GetOrCreateSession(sessionId);
        return new Dictionary<string, object>
        {
            ["metadata"] = session.Metadata,
            ["entities"] = session.Entities.Select(EntityDtoConverter.ToDto).ToList(),
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
}

public class PrimitiveDelta
{
    public string PrimitiveId { get; set; } = string.Empty;
    public List<double> Points { get; set; } = new();
    public DateTime Timestamp { get; set; }
}
