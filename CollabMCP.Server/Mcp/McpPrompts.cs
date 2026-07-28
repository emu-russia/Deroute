using CollabMCP.Server.Services;

namespace CollabMCP.Server.Mcp;

public class McpPrompts
{
    private readonly SessionManager _sessionManager;
    private readonly ILogger<McpPrompts> _logger;

    public McpPrompts(SessionManager sessionManager, ILogger<McpPrompts> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public (bool Found, string Name, string Description, string? Message, object? Arguments) GetPrompt(string name, Dictionary<string, object>? arguments)
    {
        return name switch
        {
            "AnalyzeCanvas" => AnalyzeCanvas(arguments),
            "GenerateLayout" => GenerateLayout(arguments),
            _ => (false, name, string.Empty, null, null)
        };
    }

    public List<PromptInfo> ListPrompts()
    {
        return new()
        {
            new PromptInfo { Name = "AnalyzeCanvas", Description = "Analyze the current canvas state and provide insights about the vector primitives" },
            new PromptInfo { Name = "GenerateLayout", Description = "Generate vector primitives based on a textual description of a layout" }
        };
    }

    private (bool, string, string, string?, object?) AnalyzeCanvas(Dictionary<string, object>? arguments)
    {
        if (arguments == null || !arguments.TryGetValue("sessionId", out var sidObj) || sidObj is not string sessionId)
        {
            return (false, "AnalyzeCanvas", "Analyze the current canvas state and provide insights about the vector primitives",
                "Error: sessionId is required", null);
        }

        if (!_sessionManager.TryGetSession(sessionId, out var state))
        {
            return (false, "AnalyzeCanvas", "Analyze the current canvas state and provide insights about the vector primitives",
                $"Session '{sessionId}' not found", null);
        }

        var analysis = new
        {
            sessionId,
            primitiveCount = state.Primitives.Count,
            primitivesByType = state.Primitives.Values.GroupBy(p => p.Type)
                .ToDictionary(g => g.Key, g => g.Count()),
            connectedUsers = state.ConnectedUsers.ToList(),
            lockedPrimitives = state.Primitives.Values.Where(p => p.LockedBy != null)
                .Select(p => new { p.Id, p.Type, p.LockedBy, p.LockedAt })
                .ToList(),
            backgroundImage = state.Metadata.BackgroundImageUrl ?? state.Metadata.BackgroundImageId,
            imageDimensions = (state.Metadata.ImageWidth, state.Metadata.ImageHeight)
        };

        var message = $"Canvas analysis for session '{sessionId}': " +
            $"{analysis.primitiveCount} primitives, " +
            $"{state.ConnectedUsers.Count} connected users, " +
            $"{analysis.lockedPrimitives.Count} locked primitives.";

        return (true, "AnalyzeCanvas", "Analyze the current canvas state and provide insights about the vector primitives",
            message, analysis);
    }

    private (bool, string, string, string?, object?) GenerateLayout(Dictionary<string, object>? arguments)
    {
        if (arguments == null ||
            !arguments.TryGetValue("sessionId", out var sidObj) || sidObj is not string sessionId ||
            !arguments.TryGetValue("description", out var descObj) || descObj is not string description)
        {
            return (false, "GenerateLayout",
                "Generate vector primitives based on a textual description of a layout",
                "Error: sessionId and description are required", null);
        }

        var message = $"Layout generation requested for session '{sessionId}': '{description}'. " +
            "Use the 'add_primitive' tool to create the primitives based on your analysis of the description.";

        var template = new
        {
            sessionId,
            description,
            suggestedTypes = new[] { "rectangle", "polygon", "line", "ellipse" },
            instructions = new[]
            {
                "Parse the description to identify shapes and their positions",
                "Use add_primitive tool to create each shape",
                "Set appropriate coordinates relative to the image dimensions",
                "Use meaningful stroke colors to differentiate element types"
            }
        };

        return (true, "GenerateLayout",
            "Generate vector primitives based on a textual description of a layout",
            message, template);
    }
}

public class PromptInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
