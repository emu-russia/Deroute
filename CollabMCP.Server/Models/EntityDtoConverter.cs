namespace CollabMCP.Server.Models;

/// <summary>
/// Shared conversion between the storage model (EntityNode) and the wire DTO (EntityDto).
/// The DTO uses flat point arrays [x1, y1, x2, y2, ...] and stores timestamps as ISO strings.
/// </summary>
public static class EntityDtoConverter
{
    /// <summary>Returns the wire type as-is (EntityType enum name); null/empty default to WireInterconnect.</summary>
    public static string NormalizeType(string? type)
    {
        return string.IsNullOrWhiteSpace(type) ? "WireInterconnect" : type;
    }

    public static EntityDto ToDto(EntityNode node)
    {
        return new EntityDto
        {
            Id = node.Id,
            Type = node.Type,
            Label = node.Label,
            LambdaX = node.LambdaX,
            LambdaY = node.LambdaY,
            LambdaEndX = node.LambdaEndX,
            LambdaEndY = node.LambdaEndY,
            LambdaWidth = node.LambdaWidth,
            LambdaHeight = node.LambdaHeight,
            Priority = node.Priority,
            WidthOverride = node.WidthOverride,
            ColorOverride = node.ColorOverride,
            FontOverride = node.FontOverride,
            LabelAlignment = node.LabelAlignment,
            Points = node.PathPoints?
                .SelectMany(p => new[] { (double)p.X, (double)p.Y })
                .ToList() ?? new List<double>(),
            TraverseBlackList = node.TraverseBlackList?.ToList(),
            Module = node.Module,
            Visible = node.Visible,
            Children = node.Children.Select(ToDto).ToList(),
            CreatedBy = node.CreatedBy,
            LockedBy = node.LockedBy ?? "none",
            LockedAt = node.LockedAt?.ToString("o") ?? "",
            Version = node.Version,
            CreatedAt = node.CreatedAt.ToString("o"),
            UpdatedAt = node.UpdatedAt.ToString("o")
        };
    }

    public static EntityNode ToNode(EntityDto dto)
    {
        var node = new EntityNode
        {
            Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
            Type = NormalizeType(dto.Type),
            Label = dto.Label,
            LambdaX = dto.LambdaX,
            LambdaY = dto.LambdaY,
            LambdaEndX = dto.LambdaEndX,
            LambdaEndY = dto.LambdaEndY,
            LambdaWidth = dto.LambdaWidth,
            LambdaHeight = dto.LambdaHeight,
            Priority = dto.Priority,
            WidthOverride = dto.WidthOverride,
            ColorOverride = dto.ColorOverride,
            FontOverride = dto.FontOverride,
            LabelAlignment = dto.LabelAlignment,
            TraverseBlackList = dto.TraverseBlackList?.ToList(),
            Module = dto.Module,
            Visible = dto.Visible,
            CreatedBy = dto.CreatedBy ?? string.Empty,
            LockedBy = string.IsNullOrEmpty(dto.LockedBy) || dto.LockedBy == "none" ? null : dto.LockedBy,
            LockedAt = string.IsNullOrEmpty(dto.LockedAt) ? null : DateTime.TryParse(dto.LockedAt, out var la) ? la : null,
            Version = dto.Version,
            CreatedAt = string.IsNullOrEmpty(dto.CreatedAt) ? DateTime.UtcNow : DateTime.TryParse(dto.CreatedAt, out var ca) ? ca : DateTime.UtcNow,
            UpdatedAt = string.IsNullOrEmpty(dto.UpdatedAt) ? DateTime.UtcNow : DateTime.TryParse(dto.UpdatedAt, out var ua) ? ua : DateTime.UtcNow
        };

        var flat = dto.Points;
        if (flat != null && flat.Count >= 2)
        {
            var points = new List<EntityPoint>();
            for (int i = 0; i + 1 < flat.Count; i += 2)
            {
                points.Add(new EntityPoint
                {
                    X = (float)flat[i],
                    Y = (float)flat[i + 1]
                });
            }
            if (points.Count > 0)
            {
                node.PathPoints = points;
                if (dto.LambdaX == 0 && dto.LambdaY == 0 && dto.LambdaEndX == 0 && dto.LambdaEndY == 0)
                {
                    node.LambdaX = points[0].X;
                    node.LambdaY = points[0].Y;
                    node.LambdaEndX = points[^1].X;
                    node.LambdaEndY = points[^1].Y;
                }
            }
        }

        if (dto.Children != null)
            node.Children = dto.Children.Select(ToNode).ToList();

        return node;
    }
}
