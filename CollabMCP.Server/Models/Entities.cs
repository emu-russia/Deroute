using System.Collections.Concurrent;

namespace CollabMCP.Server.Models;

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class VectorPrimitive
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public List<Point> Points { get; set; } = new();
    public string StrokeColor { get; set; } = "#000000";
    public double StrokeWidth { get; set; } = 1.0;
    public string FillColor { get; set; } = "transparent";
    public string CreatedBy { get; set; } = string.Empty;
    public string? LockedBy { get; set; }
    public DateTime? LockedAt { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SessionMetadata
{
    public string SessionId { get; set; } = string.Empty;
    public string? BackgroundImageId { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;
}

public class SessionState
{
    public SessionMetadata Metadata { get; set; } = new();
    public ConcurrentDictionary<string, VectorPrimitive> Primitives { get; set; } = new();
    public List<OperationLogEntry> History { get; set; } = new();
    public HashSet<string> ConnectedUsers { get; set; } = new();
    public int Version { get; set; } = 1;
}

public class OperationLogEntry
{
    public string Operation { get; set; } = string.Empty;
    public string PrimitiveId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Details { get; set; }
}
