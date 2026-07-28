using System.Collections.Concurrent;
using CollabMCP.Server.Models;

namespace CollabMCP.Server.Services;

public class SessionManager
{
    private readonly ConcurrentDictionary<string, SessionState> _sessions = new();
    private readonly XmlSessionStore _xmlStore;
    private readonly ILogger<SessionManager> _logger;
    private readonly object _lockObj = new();

    public SessionManager(XmlSessionStore xmlStore, ILogger<SessionManager> logger)
    {
        _xmlStore = xmlStore;
        _logger = logger;
    }

    public SessionState GetOrCreateSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
            return session;

        lock (_lockObj)
        {
            if (_sessions.TryGetValue(sessionId, out session))
                return session;

            var loaded = _xmlStore.LoadSession(sessionId);
            _sessions[sessionId] = loaded;
            _logger.LogInformation("Session loaded/created: {SessionId}", sessionId);
            return loaded;
        }
    }

    public bool TryGetSession(string sessionId, out SessionState? session)
    {
        return _sessions.TryGetValue(sessionId, out session);
    }

    public void RemoveSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            _xmlStore.SaveSession(session);
            _logger.LogInformation("Session removed and saved: {SessionId}", sessionId);
        }
    }

    public void SaveSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            _xmlStore.SaveSession(session);
        }
    }

    public void AddUserToSession(string sessionId, string userId)
    {
        var session = GetOrCreateSession(sessionId);
        session.ConnectedUsers.Add(userId);
        session.Metadata.LastActivity = DateTime.UtcNow;

        var entry = new OperationLogEntry
        {
            Operation = "UserJoined",
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };
        session.History.Add(entry);
        if (session.History.Count > 1000)
            session.History = session.History.Skip(Math.Max(0, session.History.Count - 500)).ToList();

        _xmlStore.SaveSession(session);
        _logger.LogInformation("User {UserId} joined session {SessionId}", userId, sessionId);
    }

    public void RemoveUserFromSession(string sessionId, string userId)
    {
        var session = GetOrCreateSession(sessionId);
        session.ConnectedUsers.Remove(userId);
        session.Metadata.LastActivity = DateTime.UtcNow;

        foreach (var prim in session.Primitives.Values)
        {
            if (prim.LockedBy == userId)
            {
                prim.LockedBy = null;
                prim.LockedAt = null;
            }
        }

        var entry = new OperationLogEntry
        {
            Operation = "UserLeft",
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };
        session.History.Add(entry);

        _xmlStore.SaveSession(session);
        _logger.LogInformation("User {UserId} left session {SessionId}", userId, sessionId);
    }

    public HashSet<string> GetConnectedUsers(string sessionId)
    {
        var session = GetOrCreateSession(sessionId);
        return new HashSet<string>(session.ConnectedUsers);
    }

    public bool IsUserConnected(string sessionId, string userId)
    {
        var session = GetOrCreateSession(sessionId);
        return session.ConnectedUsers.Contains(userId);
    }

    public VectorPrimitive? GetPrimitive(string sessionId, string primitiveId)
    {
        var session = GetOrCreateSession(sessionId);
        return session.Primitives.TryGetValue(primitiveId, out var prim) ? prim : null;
    }

    public (VectorPrimitive? Primitive, string? Error) TryLockPrimitive(string sessionId, string primitiveId, string userId)
    {
        var session = GetOrCreateSession(sessionId);

        if (!session.Primitives.TryGetValue(primitiveId, out var prim))
            return (null, "Primitive not found");

        if (prim.LockedBy != null && prim.LockedBy != userId)
        {
            _logger.LogWarning("Primitive {PrimitiveId} locked by {LockedBy}, request from {UserId}",
                primitiveId, prim.LockedBy, userId);
            return (null, $"Primitive locked by user {prim.LockedBy}");
        }

        prim.LockedBy = userId;
        prim.LockedAt = DateTime.UtcNow;
        prim.Version++;
        prim.UpdatedAt = DateTime.UtcNow;

        session.Version++;

        var entry = new OperationLogEntry
        {
            Operation = "Locked",
            PrimitiveId = primitiveId,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };
        session.History.Add(entry);

        _xmlStore.SaveSession(session);
        return (prim, null);
    }

    public VectorPrimitive UnlockPrimitive(string sessionId, string primitiveId, string userId)
    {
        var session = GetOrCreateSession(sessionId);
        if (!session.Primitives.TryGetValue(primitiveId, out var prim))
            return new VectorPrimitive { Id = primitiveId };

        if (prim.LockedBy == userId)
        {
            prim.LockedBy = null;
            prim.LockedAt = null;
            prim.Version++;
            prim.UpdatedAt = DateTime.UtcNow;
            session.Version++;

            var entry = new OperationLogEntry
            {
                Operation = "Unlocked",
                PrimitiveId = primitiveId,
                UserId = userId,
                Timestamp = DateTime.UtcNow
            };
            session.History.Add(entry);

            _xmlStore.SaveSession(session);
        }

        return prim;
    }

    public (VectorPrimitive? Primitive, string? Error) AddPrimitive(string sessionId, VectorPrimitive primitive, string userId)
    {
        var session = GetOrCreateSession(sessionId);

        if (session.Primitives.ContainsKey(primitive.Id))
            return (null, "Primitive with this ID already exists");

        primitive.CreatedBy = userId;
        primitive.CreatedAt = DateTime.UtcNow;
        primitive.UpdatedAt = DateTime.UtcNow;
        primitive.Version = 1;

        session.Primitives[primitive.Id] = primitive;
        session.Version++;

        var entry = new OperationLogEntry
        {
            Operation = "Created",
            PrimitiveId = primitive.Id,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };
        session.History.Add(entry);

        _xmlStore.SaveSession(session);
        return (primitive, null);
    }

    public (VectorPrimitive? Primitive, string? Error) UpdatePrimitive(string sessionId, string primitiveId, VectorPrimitive updated, string userId)
    {
        var session = GetOrCreateSession(sessionId);

        if (!session.Primitives.TryGetValue(primitiveId, out var existing))
            return (null, "Primitive not found");

        if (existing.LockedBy != null && existing.LockedBy != userId)
            return (null, $"Primitive locked by user {existing.LockedBy}");

        existing.Type = updated.Type;
        existing.Points = updated.Points;
        existing.StrokeColor = updated.StrokeColor;
        existing.StrokeWidth = updated.StrokeWidth;
        existing.FillColor = updated.FillColor;
        existing.Version++;
        existing.UpdatedAt = DateTime.UtcNow;

        session.Version++;

        var entry = new OperationLogEntry
        {
            Operation = "Updated",
            PrimitiveId = primitiveId,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            Details = $"Version {existing.Version}"
        };
        session.History.Add(entry);

        _xmlStore.SaveSession(session);
        return (existing, null);
    }

    public (bool Success, string? Error) DeletePrimitive(string sessionId, string primitiveId, string userId)
    {
        var session = GetOrCreateSession(sessionId);

        if (!session.Primitives.TryRemove(primitiveId, out _))
            return (false, "Primitive not found");

        session.Version++;

        var entry = new OperationLogEntry
        {
            Operation = "Deleted",
            PrimitiveId = primitiveId,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };
        session.History.Add(entry);

        _xmlStore.SaveSession(session);
        return (true, null);
    }

    public (bool Success, string? Error) ClearCanvas(string sessionId, string userId)
    {
        var session = GetOrCreateSession(sessionId);
        var count = session.Primitives.Count;
        session.Primitives.Clear();
        session.Version++;

        var entry = new OperationLogEntry
        {
            Operation = "Cleared",
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            Details = $"Cleared {count} primitives"
        };
        session.History.Add(entry);

        _xmlStore.SaveSession(session);
        return (true, null);
    }

    public List<OperationLogEntry> GetHistory(string sessionId, int count = 50)
    {
        var session = GetOrCreateSession(sessionId);
        return session.History.Skip(Math.Max(0, session.History.Count - count)).ToList();
    }

    public List<string> GetSessionIds()
    {
        return _sessions.Keys.ToList();
    }
}
