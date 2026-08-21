using System.Collections.Concurrent;

namespace CollabMCP.Server.Models;

/// <summary>A single 2D coordinate in the application entity model (float, like PointF).</summary>
public class EntityPoint
{
    public float X { get; set; }
    public float Y { get; set; }
}

/// <summary>
/// Server-side mirror of the application's Entity model (EntityBox.Entity), enriched with
/// collaboration service fields (id, lock, version, timestamps). The Type is the EntityType
/// enum name (e.g. "ViasInput", "CellNot", "Region", "Layer"); legacy vector types
/// ("rectangle", "polygon", "ellipse", "line", "polyline") are mapped on load/migration.
/// </summary>
public class EntityNode
{
    // --- collaboration service fields ---
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CreatedBy { get; set; } = string.Empty;
    public string? LockedBy { get; set; }
    public DateTime? LockedAt { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // --- application entity model (mirror of EntityBox.Entity) ---
    public string? Label { get; set; }
    public string Type { get; set; } = "WireInterconnect";
    public float LambdaX { get; set; }
    public float LambdaY { get; set; }
    public float LambdaEndX { get; set; }
    public float LambdaEndY { get; set; }
    public float LambdaWidth { get; set; }
    public float LambdaHeight { get; set; }
    public int Priority { get; set; }
    public int WidthOverride { get; set; }
    public string? ColorOverride { get; set; }   // "#RRGGBB"
    public string? FontOverride { get; set; }    // FontXmlConverter string
    public string LabelAlignment { get; set; } = "GlobalSettings";
    public List<EntityPoint>? PathPoints { get; set; }
    public List<string>? TraverseBlackList { get; set; }
    public string? Module { get; set; }
    public bool Visible { get; set; } = true;

    // --- hierarchy (layers are EntityType.Layer containers) ---
    public List<EntityNode> Children { get; set; } = new();
}

/// <summary>
/// Wire DTO (camelCase JSON) for the full entity model. Points are transmitted as a flat
/// number array [x1, y1, x2, y2, ...] and converted to/from EntityNode.PathPoints.
/// </summary>
public class EntityDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "WireInterconnect";
    public string? Label { get; set; }
    public float LambdaX { get; set; }
    public float LambdaY { get; set; }
    public float LambdaEndX { get; set; }
    public float LambdaEndY { get; set; }
    public float LambdaWidth { get; set; }
    public float LambdaHeight { get; set; }
    public int Priority { get; set; }
    public int WidthOverride { get; set; }
    public string? ColorOverride { get; set; }
    public string? FontOverride { get; set; }
    public string LabelAlignment { get; set; } = "GlobalSettings";
    public List<double>? Points { get; set; }    // flat [x1, y1, ...]
    public List<string>? TraverseBlackList { get; set; }
    public string? Module { get; set; }
    public bool Visible { get; set; } = true;
    public List<EntityDto>? Children { get; set; }
    public string? CreatedBy { get; set; }
    public string? LockedBy { get; set; }
    public string? LockedAt { get; set; }
    public int Version { get; set; } = 1;
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
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
    /// <summary>Top-level entities; the list order is the z-order (stable for equal Priority).</summary>
    public List<EntityNode> Entities { get; set; } = new();
    /// <summary>O(1) lookup by entity id across the whole tree.</summary>
    public ConcurrentDictionary<string, EntityNode> EntityIndex { get; set; } = new();
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
