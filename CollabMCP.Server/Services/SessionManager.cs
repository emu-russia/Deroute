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

    /// <summary>
    /// Read-oriented lazy load: returns the session from memory, or loads it from the
    /// XML store if present, or returns false when the session does not exist at all.
    /// Unlike <see cref="GetOrCreateSession"/> it never creates a new empty session.
    /// </summary>
    public bool TryLoadSession(string sessionId, out SessionState? session)
    {
        if (_sessions.TryGetValue(sessionId, out session))
            return true;

        lock (_lockObj)
        {
            if (_sessions.TryGetValue(sessionId, out session))
                return true;

            if (!_xmlStore.SessionExists(sessionId))
            {
                session = null;
                return false;
            }

            session = _xmlStore.LoadSession(sessionId);
            _sessions[sessionId] = session;
            _logger.LogInformation("Session loaded from XML: {SessionId}", sessionId);
            return true;
        }
    }

    public void RemoveSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            // A DELETE removes the session entirely: drop the in-memory state and
            // delete its XML file so it does not reappear on the next lazy load.
            _xmlStore.DeleteSession(sessionId);
            _logger.LogInformation("Session removed and deleted: {SessionId}", sessionId);
        }
    }

    public void SaveSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            _xmlStore.SaveSession(session);
        }
    }

    // ---------------------------------------------------------------- users

    public void AddUserToSession(string sessionId, string userId)
    {
        var session = GetOrCreateSession(sessionId);
        session.ConnectedUsers.Add(userId);
        session.Metadata.LastActivity = DateTime.UtcNow;

        AddHistory(session, "UserJoined", string.Empty, userId, null);
        _xmlStore.SaveSession(session);
        _logger.LogInformation("User {UserId} joined session {SessionId}", userId, sessionId);
    }

    public void RemoveUserFromSession(string sessionId, string userId)
    {
        var session = GetOrCreateSession(sessionId);
        session.ConnectedUsers.Remove(userId);
        session.Metadata.LastActivity = DateTime.UtcNow;

        // Automatic unlock of all primitives locked by the departing user
        foreach (var prim in session.EntityIndex.Values)
        {
            if (prim.LockedBy == userId)
            {
                prim.LockedBy = null;
                prim.LockedAt = null;
            }
        }

        AddHistory(session, "UserLeft", string.Empty, userId, null);
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

    // ---------------------------------------------------------------- entity CRUD

    public EntityNode? GetEntity(string sessionId, string entityId)
    {
        var session = GetOrCreateSession(sessionId);
        return session.EntityIndex.TryGetValue(entityId, out var node) ? node : null;
    }

    public (EntityNode? Entity, string? Error) AddEntity(string sessionId, EntityNode entity, string userId, string? parentId = null)
    {
        var session = GetOrCreateSession(sessionId);

        if (session.EntityIndex.ContainsKey(entity.Id))
            return (null, "Entity with this ID already exists");

        entity.CreatedBy = userId;
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.Version = 1;
        entity.LockedBy = null;
        entity.LockedAt = null;

        if (!string.IsNullOrEmpty(parentId))
        {
            var parent = session.EntityIndex.TryGetValue(parentId, out var p) ? p : null;
            if (parent == null)
                return (null, "Parent entity not found");
            parent.Children.Add(entity);
        }
        else
        {
            session.Entities.Add(entity);
        }

        session.Version++;
        IndexEntity(session, entity);
        AddHistory(session, "Created", entity.Id, userId, null);
        _xmlStore.SaveSession(session);
        return (entity, null);
    }

    public (EntityNode? Entity, string? Error) UpdateEntity(string sessionId, string entityId, EntityNode updated, string userId)
    {
        var session = GetOrCreateSession(sessionId);

        if (!session.EntityIndex.TryGetValue(entityId, out var existing))
            return (null, "Entity not found");

        if (existing.LockedBy != null && existing.LockedBy != userId)
            return (null, $"Entity locked by user {existing.LockedBy}");

        existing.Label = updated.Label;
        existing.Type = updated.Type;
        existing.LambdaX = updated.LambdaX;
        existing.LambdaY = updated.LambdaY;
        existing.LambdaEndX = updated.LambdaEndX;
        existing.LambdaEndY = updated.LambdaEndY;
        existing.LambdaWidth = updated.LambdaWidth;
        existing.LambdaHeight = updated.LambdaHeight;
        existing.Priority = updated.Priority;
        existing.WidthOverride = updated.WidthOverride;
        existing.ColorOverride = updated.ColorOverride;
        existing.FontOverride = updated.FontOverride;
        existing.LabelAlignment = updated.LabelAlignment;
        existing.PathPoints = updated.PathPoints;
        existing.TraverseBlackList = updated.TraverseBlackList;
        existing.Module = updated.Module;
        existing.Visible = updated.Visible;
        if (updated.Children.Count > 0)
            existing.Children = updated.Children;
        existing.Version++;
        existing.UpdatedAt = DateTime.UtcNow;

        session.Version++;
        AddHistory(session, "Updated", entityId, userId, $"Version {existing.Version}");
        _xmlStore.SaveSession(session);
        return (existing, null);
    }

    public (bool Success, string? Error) DeleteEntity(string sessionId, string entityId, string userId)
    {
        var session = GetOrCreateSession(sessionId);

        if (!session.EntityIndex.TryGetValue(entityId, out var target))
            return (false, "Entity not found");

        if (target.LockedBy != null && target.LockedBy != userId)
            return (false, $"Entity locked by user {target.LockedBy}");

        var removed = RemoveFromTree(session.Entities, target);
        if (!removed)
            return (false, "Entity not found");

        session.Version++;
        RemoveFromIndex(session, target);
        AddHistory(session, "Deleted", entityId, userId, null);
        _xmlStore.SaveSession(session);
        return (true, null);
    }

    public (bool Success, string? Error) ClearCanvas(string sessionId, string userId)
    {
        var session = GetOrCreateSession(sessionId);
        var count = session.Entities.Count;
        session.Entities.Clear();
        session.EntityIndex.Clear();
        session.Version++;

        AddHistory(session, "Cleared", string.Empty, userId, $"Cleared {count} entities");
        _xmlStore.SaveSession(session);
        return (true, null);
    }

    public (EntityNode? Entity, string? Error) TryLockEntity(string sessionId, string entityId, string userId)
    {
        var session = GetOrCreateSession(sessionId);

        if (!session.EntityIndex.TryGetValue(entityId, out var node))
            return (null, "Entity not found");

        if (node.LockedBy != null && node.LockedBy != userId)
        {
            _logger.LogWarning("Entity {EntityId} locked by {LockedBy}, request from {UserId}",
                entityId, node.LockedBy, userId);
            return (null, $"Entity locked by user {node.LockedBy}");
        }

        node.LockedBy = userId;
        node.LockedAt = DateTime.UtcNow;
        node.Version++;
        node.UpdatedAt = DateTime.UtcNow;

        session.Version++;
        AddHistory(session, "Locked", entityId, userId, null);
        _xmlStore.SaveSession(session);
        return (node, null);
    }

    public EntityNode UnlockEntity(string sessionId, string entityId, string userId)
    {
        var session = GetOrCreateSession(sessionId);
        if (!session.EntityIndex.TryGetValue(entityId, out var node))
            return new EntityNode { Id = entityId };

        if (node.LockedBy == userId)
        {
            node.LockedBy = null;
            node.LockedAt = null;
            node.Version++;
            node.UpdatedAt = DateTime.UtcNow;
            session.Version++;

            AddHistory(session, "Unlocked", entityId, userId, null);
            _xmlStore.SaveSession(session);
        }

        return node;
    }

    /// <summary>
    /// Applies a real-time position delta to the stored entity (PathPoints and Lambda* from
    /// the first/last point). Position updates are transient: no version bump, no history
    /// entry, and no XML write (the state is persisted by the next real mutation).
    /// </summary>
    public void ApplyPositionUpdate(string sessionId, string entityId, List<double> flatPoints)
    {
        var session = GetOrCreateSession(sessionId);
        if (!session.EntityIndex.TryGetValue(entityId, out var node))
            return;

        if (flatPoints == null || flatPoints.Count < 2)
            return;

        var points = new List<EntityPoint>();
        for (int i = 0; i + 1 < flatPoints.Count; i += 2)
        {
            points.Add(new EntityPoint
            {
                X = (float)flatPoints[i],
                Y = (float)flatPoints[i + 1]
            });
        }

        if (points.Count > 0)
        {
            node.PathPoints = points;
            node.LambdaX = points[0].X;
            node.LambdaY = points[0].Y;
            node.LambdaEndX = points[^1].X;
            node.LambdaEndY = points[^1].Y;
        }
        session.Metadata.LastActivity = DateTime.UtcNow;
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

    /// <summary>All session IDs: in-memory sessions plus sessions persisted on disk.</summary>
    public List<string> GetAllSessionIds()
    {
        var ids = new HashSet<string>(_sessions.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (var id in _xmlStore.ListSessions())
            ids.Add(id);
        return ids.ToList();
    }

    // ---------------------------------------------------------------- helpers

    private static void AddHistory(SessionState session, string operation, string entityId, string userId, string? details)
    {
        session.History.Add(new OperationLogEntry
        {
            Operation = operation,
            PrimitiveId = entityId,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            Details = details
        });
        if (session.History.Count > 1000)
            session.History = session.History.Skip(Math.Max(0, session.History.Count - 500)).ToList();
    }

    private static void IndexEntity(SessionState session, EntityNode node)
    {
        if (string.IsNullOrEmpty(node.Id)) return;
        session.EntityIndex[node.Id] = node;
        foreach (var child in node.Children)
            IndexEntity(session, child);
    }

    private static void RemoveFromIndex(SessionState session, EntityNode node)
    {
        session.EntityIndex.TryRemove(node.Id, out _);
        foreach (var child in node.Children)
            RemoveFromIndex(session, child);
    }

    /// <summary>Removes the node (with its subtree) from the tree. Returns false if not found.</summary>
    private static bool RemoveFromTree(List<EntityNode> nodes, EntityNode target)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == target)
            {
                nodes.RemoveAt(i);
                return true;
            }
            if (RemoveFromTree(nodes[i].Children, target))
                return true;
        }
        return false;
    }
}
