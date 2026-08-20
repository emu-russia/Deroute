using System.Collections.Concurrent;
using CollabMCP.Server.Hubs;
using CollabMCP.Server.Models;
using Microsoft.AspNetCore.SignalR;

namespace CollabMCP.Server.Services;

/// <summary>
/// Buffers real-time position updates per (session, user) and flushes them to the
/// session group on a throttled timer (Server:ThrottleIntervalMs, default 33 ms ≈ 30 FPS).
/// The hub instances themselves are transient, so this state lives in a singleton service.
/// </summary>
public class PositionUpdateFlusher : IDisposable
{
    private readonly IHubContext<CollabHub> _hub;
    private readonly ILogger<PositionUpdateFlusher> _logger;
    private readonly Timer _timer;
    // key: "{sessionId}_{userId}", value: primitiveId -> latest delta
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PrimitiveDelta>> _pending = new();
    private readonly object _lockObj = new();
    private bool _disposed;

    public PositionUpdateFlusher(IHubContext<CollabHub> hub, ILogger<PositionUpdateFlusher> logger, IConfiguration config)
    {
        _hub = hub;
        _logger = logger;
        var intervalMs = Math.Max(1, config.GetValue<int>("Server:ThrottleIntervalMs", 33));
        _timer = new Timer(_ => FlushAll(), null, TimeSpan.FromMilliseconds(intervalMs), TimeSpan.FromMilliseconds(intervalMs));
    }

    public void Buffer(string sessionId, string userId, string primitiveId, List<double> points)
    {
        var key = MakeKey(sessionId, userId);
        var deltas = _pending.GetOrAdd(key, _ => new ConcurrentDictionary<string, PrimitiveDelta>());
        deltas[primitiveId] = new PrimitiveDelta
        {
            PrimitiveId = primitiveId,
            Points = points,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>Immediately flushes all buffered deltas for a (session, user) pair.</summary>
    public void FlushSession(string sessionId, string userId)
    {
        FlushKey(MakeKey(sessionId, userId));
    }

    private void FlushAll()
    {
        List<string> keys;
        lock (_lockObj)
        {
            keys = _pending.Keys.ToList();
        }

        foreach (var key in keys)
        {
            FlushKey(key);
        }
    }

    private void FlushKey(string key)
    {
        if (!_pending.TryRemove(key, out var deltas))
            return;

        // Split the key back into session/user (ids are GUID-derived and contain no '_').
        var sep = key.IndexOf('_');
        if (sep <= 0 || sep >= key.Length - 1)
            return;
        var sessionId = key[..sep];
        var userId = key[(sep + 1)..];

        foreach (var kvp in deltas)
        {
            _ = _hub.Clients.Group(sessionId).SendAsync("OnPositionUpdated", new
            {
                PrimitiveId = kvp.Key,
                Points = kvp.Value.Points
            });
        }
    }

    private static string MakeKey(string sessionId, string userId) => $"{sessionId}_{userId}";

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _timer?.Dispose();
    }
}
