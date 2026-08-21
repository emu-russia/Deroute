using System.Collections.Concurrent;
using System.Xml.Linq;
using CollabMCP.Server.Config;
using CollabMCP.Server.Models;
using Microsoft.Extensions.Options;

namespace CollabMCP.Server.Services;

/// <summary>
/// XML persistence for sessions (format v2): full EntityNode model.
/// Only the current format is supported — no legacy/migration paths.
/// </summary>
public class XmlSessionStore
{
    private readonly string _storagePath;
    private readonly ILogger<XmlSessionStore> _logger;

    public XmlSessionStore(IOptions<ServerConfig> config, ILogger<XmlSessionStore> logger)
    {
        _storagePath = config.Value.XmlStoragePath;
        _logger = logger;

        if (!Directory.Exists(_storagePath))
            Directory.CreateDirectory(_storagePath);
    }

    public string GetSessionFilePath(string sessionId)
    {
        return Path.Combine(_storagePath, $"{sessionId}.xml");
    }

    public bool SessionExists(string sessionId)
    {
        return File.Exists(GetSessionFilePath(sessionId));
    }

    public SessionState LoadSession(string sessionId)
    {
        var filePath = GetSessionFilePath(sessionId);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Session file not found: {FilePath}", filePath);
            return CreateNewSession(sessionId);
        }

        try
        {
            var doc = XDocument.Load(filePath);
            var root = doc.Root;

            if (root == null)
                return CreateNewSession(sessionId);

            return LoadSessionV2(root, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading session {SessionId} from {FilePath}", sessionId, filePath);
            return CreateNewSession(sessionId);
        }
    }

    private SessionState LoadSessionV2(XElement root, string sessionId)
    {
        var metadata = ReadMetadata(root, sessionId);

        var entities = new List<EntityNode>();
        var index = new ConcurrentDictionary<string, EntityNode>();
        var entitiesNode = root.Element("Entities");
        if (entitiesNode != null)
        {
            foreach (var nodeEl in entitiesNode.Elements("EntityNode"))
            {
                var node = DeserializeEntityNode(nodeEl);
                if (node != null)
                {
                    entities.Add(node);
                    IndexEntity(node, index);
                }
            }
        }

        var history = ReadHistory(root);
        var connectedUsers = ReadConnectedUsers(root);

        return new SessionState
        {
            Metadata = metadata,
            Entities = entities,
            EntityIndex = index,
            History = history,
            ConnectedUsers = connectedUsers,
            Version = metadata.Version
        };
    }

    private static void IndexEntity(EntityNode node, ConcurrentDictionary<string, EntityNode> index)
    {
        if (string.IsNullOrEmpty(node.Id)) return;
        index[node.Id] = node;
        foreach (var child in node.Children)
            IndexEntity(child, index);
    }

    public void SaveSession(SessionState state)
    {
        var filePath = GetSessionFilePath(state.Metadata.SessionId);

        try
        {
            var doc = new XDocument(
                new XElement("Session",
                    new XAttribute("FormatVersion", "2"),
                    new XElement("Metadata",
                        new XElement("SessionId", state.Metadata.SessionId),
                        new XElement("BackgroundImageId", state.Metadata.BackgroundImageId ?? string.Empty),
                        new XElement("BackgroundImageUrl", state.Metadata.BackgroundImageUrl ?? string.Empty),
                        new XElement("ImageWidth", state.Metadata.ImageWidth ?? 0),
                        new XElement("ImageHeight", state.Metadata.ImageHeight ?? 0),
                        new XElement("CreatedAt", state.Metadata.CreatedAt.ToString("o")),
                        new XElement("LastActivity", DateTime.UtcNow.ToString("o")),
                        new XElement("Version", state.Metadata.Version)
                    ),
                    new XElement("Entities",
                        from node in state.Entities
                        select SerializeEntityNode(node)
                    ),
                    new XElement("History",
                        from entry in state.History
                        select new XElement("Entry",
                            new XElement("Operation", entry.Operation),
                            new XElement("PrimitiveId", entry.PrimitiveId),
                            new XElement("UserId", entry.UserId),
                            new XElement("Timestamp", entry.Timestamp.ToString("o")),
                            new XElement("Details", entry.Details ?? string.Empty)
                        )
                    ),
                    new XElement("ConnectedUsers",
                        from user in state.ConnectedUsers
                        select new XElement("User", user)
                    )
                )
            );

            doc.Save(filePath);
            _logger.LogDebug("Session saved: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving session {SessionId} to {FilePath}", state.Metadata.SessionId, filePath);
        }
    }

    private static XElement SerializeEntityNode(EntityNode node)
    {
        return new XElement("EntityNode",
            new XElement("Id", node.Id),
            new XElement("Type", node.Type),
            new XElement("Label", node.Label ?? string.Empty),
            new XElement("LambdaX", node.LambdaX),
            new XElement("LambdaY", node.LambdaY),
            new XElement("LambdaEndX", node.LambdaEndX),
            new XElement("LambdaEndY", node.LambdaEndY),
            new XElement("LambdaWidth", node.LambdaWidth),
            new XElement("LambdaHeight", node.LambdaHeight),
            new XElement("Priority", node.Priority),
            new XElement("WidthOverride", node.WidthOverride),
            new XElement("ColorOverride", node.ColorOverride ?? string.Empty),
            new XElement("FontOverride", node.FontOverride ?? string.Empty),
            new XElement("LabelAlignment", node.LabelAlignment),
            new XElement("PathPoints",
                from pt in node.PathPoints ?? new List<EntityPoint>()
                select new XElement("Point",
                    new XElement("X", pt.X),
                    new XElement("Y", pt.Y)
                )
            ),
            new XElement("TraverseBlackList",
                from t in node.TraverseBlackList ?? new List<string>()
                select new XElement("EntityType", t)
            ),
            new XElement("Module", node.Module ?? string.Empty),
            new XElement("Visible", node.Visible),
            new XElement("CreatedBy", node.CreatedBy),
            new XElement("LockedBy", node.LockedBy ?? string.Empty),
            new XElement("LockedAt", node.LockedAt?.ToString("o") ?? string.Empty),
            new XElement("Version", node.Version),
            new XElement("CreatedAt", node.CreatedAt.ToString("o")),
            new XElement("UpdatedAt", node.UpdatedAt.ToString("o")),
            new XElement("Children",
                from child in node.Children
                select SerializeEntityNode(child)
            )
        );
    }

    private EntityNode? DeserializeEntityNode(XElement el)
    {
        try
        {
            var lockedBy = el.Element("LockedBy")?.Value;
            var lockedAt = el.Element("LockedAt")?.Value;

            var node = new EntityNode
            {
                Id = el.Element("Id")?.Value ?? Guid.NewGuid().ToString(),
                Type = el.Element("Type")?.Value ?? "WireInterconnect",
                Label = el.Element("Label")?.Value,
                LambdaX = ParseFloat(el, "LambdaX"),
                LambdaY = ParseFloat(el, "LambdaY"),
                LambdaEndX = ParseFloat(el, "LambdaEndX"),
                LambdaEndY = ParseFloat(el, "LambdaEndY"),
                LambdaWidth = ParseFloat(el, "LambdaWidth"),
                LambdaHeight = ParseFloat(el, "LambdaHeight"),
                Priority = ParseInt(el, "Priority"),
                WidthOverride = ParseInt(el, "WidthOverride"),
                ColorOverride = el.Element("ColorOverride")?.Value,
                FontOverride = el.Element("FontOverride")?.Value,
                LabelAlignment = el.Element("LabelAlignment")?.Value ?? "GlobalSettings",
                Module = el.Element("Module")?.Value,
                Visible = !bool.TryParse(el.Element("Visible")?.Value, out var vis) || vis,
                CreatedBy = el.Element("CreatedBy")?.Value ?? string.Empty,
                LockedBy = string.IsNullOrEmpty(lockedBy) ? null : lockedBy,
                LockedAt = string.IsNullOrEmpty(lockedAt) ? null : DateTime.TryParse(lockedAt, out var la) ? la : null,
                Version = ParseInt(el, "Version", 1),
                CreatedAt = DateTime.TryParse(el.Element("CreatedAt")?.Value, out var ca) ? ca : DateTime.UtcNow,
                UpdatedAt = DateTime.TryParse(el.Element("UpdatedAt")?.Value, out var ua) ? ua : DateTime.UtcNow
            };

            var pathPoints = new List<EntityPoint>();
            var ptsEl = el.Element("PathPoints");
            if (ptsEl != null)
            {
                foreach (var pt in ptsEl.Elements("Point"))
                {
                    pathPoints.Add(new EntityPoint
                    {
                        X = ParseFloat(pt, "X"),
                        Y = ParseFloat(pt, "Y")
                    });
                }
            }
            node.PathPoints = pathPoints.Count > 0 ? pathPoints : null;

            var blackList = new List<string>();
            var blEl = el.Element("TraverseBlackList");
            if (blEl != null)
            {
                foreach (var t in blEl.Elements("EntityType"))
                {
                    if (!string.IsNullOrEmpty(t.Value))
                        blackList.Add(t.Value);
                }
            }
            node.TraverseBlackList = blackList.Count > 0 ? blackList : null;

            var children = new List<EntityNode>();
            var childrenEl = el.Element("Children");
            if (childrenEl != null)
            {
                foreach (var childEl in childrenEl.Elements("EntityNode"))
                {
                    var child = DeserializeEntityNode(childEl);
                    if (child != null)
                        children.Add(child);
                }
            }
            node.Children = children;

            return node;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing entity node");
            return null;
        }
    }

    public bool DeleteSession(string sessionId)
    {
        var filePath = GetSessionFilePath(sessionId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Session deleted: {FilePath}", filePath);
            return true;
        }
        return false;
    }

    public List<string> ListSessions()
    {
        if (!Directory.Exists(_storagePath))
            return new();

        return Directory.GetFiles(_storagePath, "*.xml")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .ToList();
    }

    // ---------------------------------------------------------------- shared readers

    private static SessionMetadata ReadMetadata(XElement root, string sessionId)
    {
        var meta = root.Element("Metadata");
        XElement? Field(string name) => meta?.Element(name) ?? root.Element(name);

        return new SessionMetadata
        {
            SessionId = sessionId,
            BackgroundImageId = Field("BackgroundImageId")?.Value,
            BackgroundImageUrl = Field("BackgroundImageUrl")?.Value,
            ImageWidth = int.TryParse(Field("ImageWidth")?.Value, out var w) ? w : null,
            ImageHeight = int.TryParse(Field("ImageHeight")?.Value, out var h) ? h : null,
            CreatedAt = DateTime.TryParse(Field("CreatedAt")?.Value, out var dt) ? dt : DateTime.UtcNow,
            LastActivity = DateTime.TryParse(Field("LastActivity")?.Value, out var dt2) ? dt2 : DateTime.UtcNow,
            Version = int.TryParse(Field("Version")?.Value, out var v) ? v : 1
        };
    }

    private static List<OperationLogEntry> ReadHistory(XElement root)
    {
        var history = new List<OperationLogEntry>();
        var historyNode = root.Element("History");
        if (historyNode != null)
        {
            foreach (var entryNode in historyNode.Elements("Entry"))
            {
                history.Add(new OperationLogEntry
                {
                    Operation = entryNode.Element("Operation")?.Value ?? string.Empty,
                    PrimitiveId = entryNode.Element("PrimitiveId")?.Value ?? string.Empty,
                    UserId = entryNode.Element("UserId")?.Value ?? string.Empty,
                    Timestamp = DateTime.TryParse(entryNode.Element("Timestamp")?.Value, out var ts) ? ts : DateTime.UtcNow,
                    Details = entryNode.Element("Details")?.Value
                });
            }
        }
        return history;
    }

    private static HashSet<string> ReadConnectedUsers(XElement root)
    {
        var connectedUsers = new HashSet<string>();
        var usersNode = root.Element("ConnectedUsers");
        if (usersNode != null)
        {
            foreach (var userNode in usersNode.Elements("User"))
            {
                if (!string.IsNullOrWhiteSpace(userNode.Value))
                    connectedUsers.Add(userNode.Value);
            }
        }
        return connectedUsers;
    }

    private SessionState CreateNewSession(string sessionId)
    {
        return new SessionState
        {
            Metadata = new SessionMetadata
            {
                SessionId = sessionId,
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            },
            Entities = new List<EntityNode>(),
            EntityIndex = new ConcurrentDictionary<string, EntityNode>(),
            History = new List<OperationLogEntry>(),
            ConnectedUsers = new HashSet<string>()
        };
    }

    private static float ParseFloat(XElement el, string name, float def = 0)
    {
        return float.TryParse(el.Element(name)?.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : def;
    }

    private static int ParseInt(XElement el, string name, int def = 0)
    {
        return int.TryParse(el.Element(name)?.Value, out var v) ? v : def;
    }
}
