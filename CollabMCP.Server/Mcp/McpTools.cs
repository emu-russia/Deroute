using CollabMCP.Server.Hubs;
using CollabMCP.Server.Models;
using CollabMCP.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace CollabMCP.Server.Mcp;

public class McpToolResult
{
    public bool IsError { get; set; }
    public string Text { get; set; } = string.Empty;

    public static McpToolResult Ok(object data) => new() { Text = System.Text.Json.JsonSerializer.Serialize(data, JsonOptions) };
    public static McpToolResult Error(string message) => new() { IsError = true, Text = message };

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}

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
                Description = "Add a new entity to the canvas",
                Schema = new
                {
                    type = "object",
                    required = new[] { "sessionId", "type", "points" },
                    properties = new
                    {
                        sessionId = new { type = "string", description = "Session ID" },
                        type = new { type = "string", description = "EntityType name (ViasInput, ViasOutput, WireInterconnect, CellNot, Region, Layer, ...)" },
                        points = new
                        {
                            type = "array",
                            description = "Flat coordinate pairs [x1, y1, x2, y2, ...]",
                            items = new { type = "number" }
                        },
                        parentId = new { type = "string", description = "Optional parent entity id (e.g. a Layer) to attach under" },
                        label = new { type = "string", description = "Display label" },
                        priority = new { type = "number", description = "Z-order priority" },
                        widthOverride = new { type = "number", description = "Line/entity width override" },
                        colorOverride = new { type = "string", description = "Hex color code, e.g. #FF0000" },
                        fontOverride = new { type = "string", description = "Font override (FontXmlConverter string)" },
                        labelAlignment = new { type = "string", description = "TextAlignment name (GlobalSettings, Top, ...)" },
                        traverseBlackList = new { type = "array", description = "Prohibited entity types for traverse", items = new { type = "string" } },
                        module = new { type = "string", description = "Shared Verilog module name" },
                        visible = new { type = "boolean", description = "Visibility, default true" },
                        children = new { type = "array", description = "Nested entities (same schema)", items = new { type = "object" } }
                    }
                }
            },
            new ToolInfo
            {
                Name = "update_primitive",
                Description = "Update an existing entity's properties",
                Schema = new
                {
                    type = "object",
                    required = new[] { "sessionId", "primitiveId" },
                    properties = new
                    {
                        sessionId = new { type = "string", description = "Session ID" },
                        primitiveId = new { type = "string", description = "Entity ID to update" },
                        type = new { type = "string", description = "New EntityType name" },
                        points = new
                        {
                            type = "array",
                            description = "Flat coordinate pairs [x1, y1, x2, y2, ...]",
                            items = new { type = "number" }
                        },
                        label = new { type = "string", description = "Display label" },
                        priority = new { type = "number", description = "Z-order priority" },
                        widthOverride = new { type = "number", description = "Line/entity width override" },
                        colorOverride = new { type = "string", description = "Hex color code" },
                        fontOverride = new { type = "string", description = "Font override" },
                        labelAlignment = new { type = "string", description = "TextAlignment name" },
                        module = new { type = "string", description = "Shared Verilog module name" },
                        visible = new { type = "boolean", description = "Visibility" }
                    }
                }
            },
            new ToolInfo
            {
                Name = "delete_primitive",
                Description = "Delete an entity (with its subtree) from the canvas",
                Schema = new
                {
                    type = "object",
                    required = new[] { "sessionId", "primitiveId" },
                    properties = new
                    {
                        sessionId = new { type = "string", description = "Session ID" },
                        primitiveId = new { type = "string", description = "Entity ID to delete" }
                    }
                }
            },
            new ToolInfo
            {
                Name = "clear_canvas",
                Description = "Remove all entities from the canvas",
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
                Description = "Get the current full state of the canvas (full entity model)",
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

    public async Task<McpToolResult> CallTool(string name, Dictionary<string, object> arguments, string callingUserId)
    {
        return name switch
        {
            "add_primitive" => await AddPrimitive(arguments, callingUserId),
            "update_primitive" => UpdatePrimitive(arguments, callingUserId),
            "delete_primitive" => DeletePrimitive(arguments, callingUserId),
            "clear_canvas" => ClearCanvas(arguments, callingUserId),
            "get_canvas_state" => GetCanvasState(arguments),
            "list_sessions" => ListSessions(),
            _ => McpToolResult.Error($"Unknown tool: {name}")
        };
    }

    private async Task<McpToolResult> AddPrimitive(Dictionary<string, object> args, string userId)
    {
        if (!args.TryGetValue("sessionId", out var sidObj) || sidObj is not string sessionId || string.IsNullOrEmpty(sessionId))
            return McpToolResult.Error("sessionId is required");

        if (!args.TryGetValue("type", out var typeObj) || typeObj is not string type || string.IsNullOrEmpty(type))
            return McpToolResult.Error("type is required");

        if (!TryGetPoints(args, out var pointsList))
            return McpToolResult.Error("points is required and must be an array of numbers");

        var dto = BuildDtoFromArgs(args, type, pointsList);

        var node = EntityDtoConverter.ToNode(dto);
        var parentId = args.TryGetValue("parentId", out var pid) && pid is string pidStr ? pidStr : null;

        var result = _sessionManager.AddEntity(sessionId, node, userId, parentId);

        if (result.Error != null)
            return McpToolResult.Error(result.Error);

        await _hubContext.Clients.Group(sessionId).SendAsync("OnEntityCreated", EntityDtoConverter.ToDto(result.Entity!));

        _logger.LogInformation("MCP: Entity {EntityId} added by AI (user {UserId})", node.Id, userId);
        return McpToolResult.Ok(new
        {
            result.Entity!.Id,
            result.Entity.Type,
            result.Entity.Label,
            result.Entity.PathPoints,
            result.Entity.ColorOverride,
            result.Entity.WidthOverride,
            message = "Entity created successfully"
        });
    }

    private McpToolResult UpdatePrimitive(Dictionary<string, object> args, string userId)
    {
        if (!args.TryGetValue("sessionId", out var sidObj) || sidObj is not string sessionId || string.IsNullOrEmpty(sessionId))
            return McpToolResult.Error("sessionId is required");

        if (!args.TryGetValue("primitiveId", out var pidObj) || pidObj is not string primitiveId || string.IsNullOrEmpty(primitiveId))
            return McpToolResult.Error("primitiveId is required");

        var existing = _sessionManager.GetEntity(sessionId, primitiveId);
        if (existing == null)
            return McpToolResult.Error("Entity not found");

        var dto = EntityDtoConverter.ToDto(existing);
        ApplyDtoOverrides(dto, args);

        var node = EntityDtoConverter.ToNode(dto);
        var result = _sessionManager.UpdateEntity(sessionId, primitiveId, node, userId);

        if (result.Error != null)
            return McpToolResult.Error(result.Error);

        _hubContext.Clients.Group(sessionId).SendAsync("OnEntityUpdated", EntityDtoConverter.ToDto(result.Entity!));

        _logger.LogInformation("MCP: Entity {EntityId} updated by AI (user {UserId})", primitiveId, userId);
        return McpToolResult.Ok(new { message = "Entity updated successfully" });
    }

    private McpToolResult DeletePrimitive(Dictionary<string, object> args, string userId)
    {
        if (!args.TryGetValue("sessionId", out var sidObj) || sidObj is not string sessionId || string.IsNullOrEmpty(sessionId))
            return McpToolResult.Error("sessionId is required");

        if (!args.TryGetValue("primitiveId", out var pidObj) || pidObj is not string primitiveId || string.IsNullOrEmpty(primitiveId))
            return McpToolResult.Error("primitiveId is required");

        var result = _sessionManager.DeleteEntity(sessionId, primitiveId, userId);

        if (!result.Success)
            return McpToolResult.Error(result.Error!);

        _hubContext.Clients.Group(sessionId).SendAsync("OnEntityDeleted", primitiveId);

        _logger.LogInformation("MCP: Entity {EntityId} deleted by AI (user {UserId})", primitiveId, userId);
        return McpToolResult.Ok(new { message = "Entity deleted successfully" });
    }

    private McpToolResult ClearCanvas(Dictionary<string, object> args, string userId)
    {
        if (!args.TryGetValue("sessionId", out var sidObj) || sidObj is not string sessionId || string.IsNullOrEmpty(sessionId))
            return McpToolResult.Error("sessionId is required");

        var result = _sessionManager.ClearCanvas(sessionId, userId);

        if (!result.Success)
            return McpToolResult.Error(result.Error!);

        _hubContext.Clients.Group(sessionId).SendAsync("OnCanvasCleared", new { });

        _logger.LogInformation("MCP: Canvas cleared by AI (user {UserId})", userId);
        return McpToolResult.Ok(new { message = "Canvas cleared successfully" });
    }

    private McpToolResult GetCanvasState(Dictionary<string, object> args)
    {
        if (!args.TryGetValue("sessionId", out var sidObj) || sidObj is not string sessionId || string.IsNullOrEmpty(sessionId))
            return McpToolResult.Error("sessionId is required");

        if (!_sessionManager.TryLoadSession(sessionId, out var state) || state == null)
            return McpToolResult.Error("Session not found");

        var canvas = new
        {
            Metadata = state.Metadata,
            Entities = state.Entities.Select(EntityDtoConverter.ToDto).ToList(),
            ConnectedUsers = state.ConnectedUsers.ToList(),
            Timestamp = DateTime.UtcNow.ToString("o")
        };

        return McpToolResult.Ok(canvas);
    }

    private McpToolResult ListSessions()
    {
        var sessionIds = _sessionManager.GetAllSessionIds();
        return McpToolResult.Ok(new { sessions = sessionIds, count = sessionIds.Count });
    }

    // ---------------------------------------------------------------- arg parsing

    /// <summary>Builds an EntityDto from MCP arguments.</summary>
    private static EntityDto BuildDtoFromArgs(Dictionary<string, object> args, string type, List<double> points)
    {
        var dto = new EntityDto
        {
            Id = Guid.NewGuid().ToString(),
            Type = type,
            Points = points
        };
        ApplyDtoOverrides(dto, args);
        return dto;
    }

    /// <summary>Applies optional MCP arguments onto an EntityDto (keeps unspecified fields).</summary>
    private static void ApplyDtoOverrides(EntityDto dto, Dictionary<string, object> args)
    {
        if (args.TryGetValue("type", out var t) && t is string typeStr)
            dto.Type = typeStr;
        if (args.TryGetValue("label", out var l) && l is string label)
            dto.Label = label;
        if (args.TryGetValue("priority", out var pr))
            dto.Priority = Convert.ToInt32(pr);
        if (args.TryGetValue("widthOverride", out var wo))
            dto.WidthOverride = Convert.ToInt32(wo);
        if (args.TryGetValue("colorOverride", out var co) && co is string color)
            dto.ColorOverride = color;
        if (args.TryGetValue("fontOverride", out var fo) && fo is string font)
            dto.FontOverride = font;
        if (args.TryGetValue("labelAlignment", out var la) && la is string align)
            dto.LabelAlignment = align;
        if (args.TryGetValue("module", out var mo) && mo is string module)
            dto.Module = module;
        if (args.TryGetValue("visible", out var vi))
            dto.Visible = Convert.ToBoolean(vi);
        if (args.TryGetValue("points", out var pts) && TryGetPoints(args, out var flat))
            dto.Points = flat;
        if (args.TryGetValue("traverseBlackList", out var bl) && bl is List<object> blList)
            dto.TraverseBlackList = blList.OfType<string>().ToList();
    }

    /// <summary>Parses the "points" argument as a flat list of coordinates. Accepts both
    /// converted values (List&lt;object&gt; of numbers) and raw JsonElement arrays.</summary>
    private static bool TryGetPoints(Dictionary<string, object> args, out List<double> points)
    {
        points = new List<double>();
        if (!args.TryGetValue("points", out var pointsObj) || pointsObj == null)
            return false;

        switch (pointsObj)
        {
            case List<object> list:
                foreach (var item in list)
                {
                    if (!TryToDouble(item, out var d))
                        return false;
                    points.Add(d);
                }
                return true;
            case List<double> dlist:
                points = dlist;
                return true;
            case System.Text.Json.JsonElement el when el.ValueKind == System.Text.Json.JsonValueKind.Array:
                foreach (var element in el.EnumerateArray())
                {
                    if (!element.TryGetDouble(out var d))
                        return false;
                    points.Add(d);
                }
                return true;
            default:
                return false;
        }
    }

    private static bool TryToDouble(object value, out double result)
    {
        switch (value)
        {
            case double d: result = d; return true;
            case long l: result = l; return true;
            case int i: result = i; return true;
            case float f: result = f; return true;
            case decimal m: result = (double)m; return true;
            default:
                result = 0; return false;
        }
    }
}

public class ToolInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object Schema { get; set; } = new();
}
