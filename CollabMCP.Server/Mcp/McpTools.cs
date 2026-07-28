using System.Text.Json;
using CollabMCP.Server.Hubs;
using CollabMCP.Server.Models;
using CollabMCP.Server.Services;
using Microsoft.AspNetCore.SignalR;
using ILogger = Serilog.ILogger;

namespace CollabMCP.Server.Mcp;

public class McpTools
{
    private readonly SessionManager _sessionManager;
    private readonly IHubContext<CollabHub> _hubContext;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    public McpTools(SessionManager sessionManager, IHubContext<CollabHub> hubContext, Microsoft.Extensions.Logging.ILogger<McpTools> logger)
    {
        _sessionManager = sessionManager;
        _hubContext = hubContext;
        _logger = logger;
    }

    public ToolInfo[] ListTools()
    {
        return new[]
        {
            new ToolInfo
            {
                Name = "add_primitive",
                Description = "Add a new vector primitive to the canvas",
                Schema = new
                {
                    type = "object",
                    required = new[] { "sessionId", "type", "points" },
                    properties = new
                    {
                        sessionId = new { type = "string", description = "Session ID" },
                        type = new { type = "string", description = "Primitive type (rectangle, polygon, line, ellipse, polyline)" },
                        points = new
                        {
                            type = "array",
                            description = "Array of [x1, y1, x2, y2, ...] coordinate pairs",
                            items = new { type = "number" }
                        },
                        strokeColor = new { type = "string", description = "Hex color code, default #000000" },
                        strokeWidth = new { type = "number", description = "Line width, default 1" },
                        fillColor = new { type = "string", description = "Fill color, default transparent" }
                    }
                }
            },
            new ToolInfo
            {
                Name = "update_primitive",
                Description = "Update an existing vector primitive",
                Schema = new
                {
                    type = "object",
                    required = new[] { "sessionId", "primitiveId", "points" },
                    properties = new
                    {
                        sessionId = new { type = "string", description = "Session ID" },
                        primitiveId = new { type = "string", description = "Primitive ID to update" },
                        points = new
                        {
                            type = "array",
                            description = "Array of [x1, y1, x2, y2, ...] coordinate pairs",
                            items = new { type = "number" }
                        },
                        strokeColor = new { type = "string", description = "Hex color code" },
                        strokeWidth = new { type = "number", description = "Line width" },
                        fillColor = new { type = "string", description = "Fill color" }
                    }
                }
            },
            new ToolInfo
            {
                Name = "delete_primitive",
                Description = "Delete a vector primitive from the canvas",
                Schema = new
                {
                    type = "object",
                    required = new[] { "sessionId", "primitiveId" },
                    properties = new
                    {
                        sessionId = new { type = "string", description = "Session ID" },
                        primitiveId = new { type = "string", description = "Primitive ID to delete" }
                    }
                }
            },
            new ToolInfo
            {
                Name = "clear_canvas",
                Description = "Remove all primitives from the canvas",
                Schema = new
                {
                    type = "object",
                    required = new[] { "sessionId" },
                    properties = new
                    {
                        sessionId = new { type = "string", description = "Session ID" }
                    }
                }
            },
            new ToolInfo
            {
                Name = "get_canvas_state",
                Description = "Get the current full state of the canvas",
                Schema = new
                {
                    type = "object",
                    required = new[] { "sessionId" },
                    properties = new
                    {
                        sessionId = new { type = "string", description = "Session ID" }
                    }
                }
            },
            new ToolInfo
            {
                Name = "list_sessions",
                Description = "List all available sessions",
                Schema = new { type = "object", required = new string[0], properties = new { } }
            }
        };
    }

    public async Task<string> CallTool(string name, Dictionary<string, object> arguments, string callingUserId)
    {
        return name switch
        {
            "add_primitive" => await AddPrimitive(arguments, callingUserId),
            "update_primitive" => UpdatePrimitive(arguments, callingUserId),
            "delete_primitive" => DeletePrimitive(arguments, callingUserId),
            "clear_canvas" => ClearCanvas(arguments, callingUserId),
            "get_canvas_state" => GetCanvasState(arguments),
            "list_sessions" => ListSessions(),
            _ => JsonSerializer.Serialize(new { error = $"Unknown tool: {name}" })
        };
    }

    private async Task<string> AddPrimitive(Dictionary<string, object> args, string userId)
    {
        if (!args.TryGetValue("sessionId", out var sidObj) || sidObj is not string sessionId)
            return SerializeError("sessionId is required");

        if (!args.TryGetValue("type", out var typeObj) || typeObj is not string type)
            return SerializeError("type is required");

        if (!args.TryGetValue("points", out var pointsObj) || pointsObj is not JsonElement pointsEl)
            return SerializeError("points is required");

        var pointsList = new List<double>();
        foreach (var element in pointsEl.EnumerateArray())
        {
            pointsList.Add(element.GetDouble());
        }

        var prim = new VectorPrimitive
        {
            Id = Guid.NewGuid().ToString(),
            Type = type,
            Points = new List<Point>(),
            StrokeColor = args.TryGetValue("strokeColor", out var sc) && sc is string ? (string)sc : "#000000",
            StrokeWidth = args.TryGetValue("strokeWidth", out var sw) ? Convert.ToDouble(sw) : 1.0,
            FillColor = args.TryGetValue("fillColor", out var fc) && fc is string ? (string)fc : "transparent"
        };

        for (int i = 0; i < pointsList.Count; i += 2)
        {
            prim.Points.Add(new Point
            {
                X = pointsList[i],
                Y = i + 1 < pointsList.Count ? pointsList[i + 1] : 0
            });
        }

        var result = _sessionManager.AddPrimitive(sessionId, prim, userId);

        if (result.Error != null)
            return SerializeError(result.Error);

        await _hubContext.Clients.Group(sessionId).SendAsync("OnPrimitiveCreated", SerializePrimitive(result.Primitive!));

        _logger.LogInformation("MCP: Primitive {PrimitiveId} added by AI (user {UserId})", prim.Id, userId);
        return SerializeSuccess(new
        {
            prim.Id,
            prim.Type,
            prim.Points,
            prim.StrokeColor,
            prim.StrokeWidth,
            prim.FillColor,
            message = "Primitive created successfully"
        });
    }

    private string UpdatePrimitive(Dictionary<string, object> args, string userId)
    {
        if (!args.TryGetValue("sessionId", out var sidObj) || sidObj is not string sessionId)
            return SerializeError("sessionId is required");

        if (!args.TryGetValue("primitiveId", out var pidObj) || pidObj is not string primitiveId)
            return SerializeError("primitiveId is required");

        var existing = _sessionManager.GetPrimitive(sessionId, primitiveId);
        if (existing == null)
            return SerializeError("Primitive not found");

        var updated = new VectorPrimitive
        {
            Id = primitiveId,
            Type = args.TryGetValue("type", out var typeObj) && typeObj is string t ? t : existing.Type,
            Points = new List<Point>(),
            StrokeColor = args.TryGetValue("strokeColor", out var sc) && sc is string ? (string)sc : existing.StrokeColor,
            StrokeWidth = args.TryGetValue("strokeWidth", out var sw) ? Convert.ToDouble(sw) : existing.StrokeWidth,
            FillColor = args.TryGetValue("fillColor", out var fc) && fc is string ? (string)fc : existing.FillColor
        };

        if (args.TryGetValue("points", out var pointsObj) && pointsObj is JsonElement pointsEl)
        {
            var pointsList = new List<double>();
            foreach (var element in pointsEl.EnumerateArray())
                pointsList.Add(element.GetDouble());

            for (int i = 0; i < pointsList.Count; i += 2)
            {
                updated.Points.Add(new Point
                {
                    X = pointsList[i],
                    Y = i + 1 < pointsList.Count ? pointsList[i + 1] : 0
                });
            }
        }

        var result = _sessionManager.UpdatePrimitive(sessionId, primitiveId, updated, userId);

        if (result.Error != null)
            return SerializeError(result.Error);

        _hubContext.Clients.Group(sessionId).SendAsync("OnPrimitiveUpdated", SerializePrimitive(result.Primitive!));

        _logger.LogInformation("MCP: Primitive {PrimitiveId} updated by AI (user {UserId})", primitiveId, userId);
        return SerializeSuccess(new { message = "Primitive updated successfully" });
    }

    private string DeletePrimitive(Dictionary<string, object> args, string userId)
    {
        if (!args.TryGetValue("sessionId", out var sidObj) || sidObj is not string sessionId)
            return SerializeError("sessionId is required");

        if (!args.TryGetValue("primitiveId", out var pidObj) || pidObj is not string primitiveId)
            return SerializeError("primitiveId is required");

        var result = _sessionManager.DeletePrimitive(sessionId, primitiveId, userId);

        if (!result.Success)
            return SerializeError(result.Error);

        _hubContext.Clients.Group(sessionId).SendAsync("OnPrimitiveDeleted", primitiveId);

        _logger.LogInformation("MCP: Primitive {PrimitiveId} deleted by AI (user {UserId})", primitiveId, userId);
        return SerializeSuccess(new { message = "Primitive deleted successfully" });
    }

    private string ClearCanvas(Dictionary<string, object> args, string userId)
    {
        if (!args.TryGetValue("sessionId", out var sidObj) || sidObj is not string sessionId)
            return SerializeError("sessionId is required");

        var result = _sessionManager.ClearCanvas(sessionId, userId);

        if (!result.Success)
            return SerializeError(result.Error);

        _hubContext.Clients.Group(sessionId).SendAsync("OnCanvasCleared", new { });

        _logger.LogInformation("MCP: Canvas cleared by AI (user {UserId})", userId);
        return SerializeSuccess(new { message = "Canvas cleared successfully" });
    }

    private string GetCanvasState(Dictionary<string, object> args)
    {
        if (!args.TryGetValue("sessionId", out var sidObj) || sidObj is not string sessionId)
            return SerializeError("sessionId is required");

        if (!_sessionManager.TryGetSession(sessionId, out var state))
            return SerializeError("Session not found");

        var canvas = new
        {
            Metadata = state.Metadata,
            Primitives = state.Primitives.Values.Select(SerializePrimitive).ToList(),
            ConnectedUsers = state.ConnectedUsers.ToList(),
            Timestamp = DateTime.UtcNow.ToString("o")
        };

        return SerializeSuccess(canvas);
    }

    private string ListSessions()
    {
        var sessionIds = _sessionManager.GetSessionIds();
        return SerializeSuccess(new { sessions = sessionIds, count = sessionIds.Count });
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

    private string SerializeSuccess(object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        return JsonSerializer.Serialize(new { content = new[] { new { type = "text", text = json } } });
    }

    private string SerializeError(string message)
    {
        return JsonSerializer.Serialize(new { error = message });
    }
}

public class ToolInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object Schema { get; set; } = new();
}
