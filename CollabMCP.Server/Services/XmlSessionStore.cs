using System.Collections.Concurrent;
using System.Xml.Linq;
using CollabMCP.Server.Config;
using CollabMCP.Server.Models;
using Microsoft.Extensions.Options;

namespace CollabMCP.Server.Services;

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

            var metadata = new SessionMetadata
            {
                SessionId = sessionId,
                BackgroundImageId = root.Element("BackgroundImageId")?.Value,
                BackgroundImageUrl = root.Element("BackgroundImageUrl")?.Value,
                ImageWidth = int.Parse(root.Element("ImageWidth")?.Value ?? "0"),
                ImageHeight = int.Parse(root.Element("ImageHeight")?.Value ?? "0"),
                CreatedAt = DateTime.TryParse(root.Element("CreatedAt")?.Value, out var dt) ? dt : DateTime.UtcNow,
                LastActivity = DateTime.TryParse(root.Element("LastActivity")?.Value, out var dt2) ? dt2 : DateTime.UtcNow
            };

            var primitives = new ConcurrentDictionary<string, VectorPrimitive>();
            var primitivesNode = root.Element("Primitives");
            if (primitivesNode != null)
            {
                foreach (var primNode in primitivesNode.Elements("Primitive"))
                {
                    var prim = DeserializePrimitive(primNode);
                    if (prim != null)
                        primitives[prim.Id] = prim;
                }
            }

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

            var connectedUsers = new HashSet<string>();
            var usersNode = root.Element("ConnectedUsers");
            if (usersNode != null)
            {
                foreach (var userNode in usersNode.Elements("User"))
                {
                    var userId = userNode.Value;
                    if (!string.IsNullOrWhiteSpace(userId))
                        connectedUsers.Add(userId);
                }
            }

            return new SessionState
            {
                Metadata = metadata,
                Primitives = primitives,
                History = history,
                ConnectedUsers = connectedUsers,
                Version = metadata.Version
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading session {SessionId} from {FilePath}", sessionId, filePath);
            return CreateNewSession(sessionId);
        }
    }

    public void SaveSession(SessionState state)
    {
        var filePath = GetSessionFilePath(state.Metadata.SessionId);

        try
        {
            var doc = new XDocument(
                new XElement("Session",
                    new XElement("Metadata",
                        new XElement("BackgroundImageId", state.Metadata.BackgroundImageId ?? string.Empty),
                        new XElement("BackgroundImageUrl", state.Metadata.BackgroundImageUrl ?? string.Empty),
                        new XElement("ImageWidth", state.Metadata.ImageWidth ?? 0),
                        new XElement("ImageHeight", state.Metadata.ImageHeight ?? 0),
                        new XElement("CreatedAt", state.Metadata.CreatedAt.ToString("o")),
                        new XElement("LastActivity", DateTime.UtcNow.ToString("o"))
                    ),
                    new XElement("Primitives",
                        from prim in state.Primitives.Values
                        select new XElement("Primitive",
                            new XElement("Id", prim.Id),
                            new XElement("Type", prim.Type),
                            new XElement("Points",
                                from pt in prim.Points
                                select new XElement("Point",
                                    new XElement("X", pt.X),
                                    new XElement("Y", pt.Y)
                                )
                            ),
                            new XElement("StrokeColor", prim.StrokeColor),
                            new XElement("StrokeWidth", prim.StrokeWidth),
                            new XElement("FillColor", prim.FillColor),
                            new XElement("CreatedBy", prim.CreatedBy),
                            new XElement("LockedBy", prim.LockedBy ?? string.Empty),
                            new XElement("LockedAt", prim.LockedAt?.ToString("o") ?? string.Empty),
                            new XElement("Version", prim.Version),
                            new XElement("CreatedAt", prim.CreatedAt.ToString("o")),
                            new XElement("UpdatedAt", DateTime.UtcNow.ToString("o"))
                        )
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
            Primitives = new ConcurrentDictionary<string, VectorPrimitive>(),
            History = new List<OperationLogEntry>(),
            ConnectedUsers = new HashSet<string>()
        };
    }

    private VectorPrimitive? DeserializePrimitive(XElement element)
    {
        try
        {
            var pointsElement = element.Element("Points");
            var points = new List<Point>();
            if (pointsElement != null)
            {
                foreach (var ptElement in pointsElement.Elements("Point"))
                {
                    points.Add(new Point
                    {
                        X = double.Parse(ptElement.Element("X")?.Value ?? "0"),
                        Y = double.Parse(ptElement.Element("Y")?.Value ?? "0")
                    });
                }
            }

            var lockedBy = element.Element("LockedBy")?.Value;
            var lockedAt = element.Element("LockedAt")?.Value;

            return new VectorPrimitive
            {
                Id = element.Element("Id")?.Value ?? Guid.NewGuid().ToString(),
                Type = element.Element("Type")?.Value ?? string.Empty,
                Points = points,
                StrokeColor = element.Element("StrokeColor")?.Value ?? "#000000",
                StrokeWidth = double.Parse(element.Element("StrokeWidth")?.Value ?? "1"),
                FillColor = element.Element("FillColor")?.Value ?? "transparent",
                CreatedBy = element.Element("CreatedBy")?.Value ?? string.Empty,
                LockedBy = string.IsNullOrEmpty(lockedBy) ? null : lockedBy,
                LockedAt = string.IsNullOrEmpty(lockedAt) ? null : DateTime.TryParse(lockedAt, out var la) ? la : null,
                Version = int.Parse(element.Element("Version")?.Value ?? "1"),
                CreatedAt = DateTime.TryParse(element.Element("CreatedAt")?.Value, out var ca) ? ca : DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing primitive");
            return null;
        }
    }
}
