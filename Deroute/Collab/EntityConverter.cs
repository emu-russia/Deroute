using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace DerouteSharp.Collab
{
    public static class EntityConverter
    {
        public static Entity ToEntity(VectorPrimitiveData prim, string userId)
        {
            var entity = new Entity
            {
                Label = prim.Id,
                UserData = userId.GetHashCode(),
                SelectTimeStamp = DateTime.Now.Ticks
            };

            var color = ColorTranslator.FromHtml(prim.StrokeColor ?? "#000000");
            entity.ColorOverride = color;

            if (string.IsNullOrEmpty(prim.Type))
            {
                entity.Type = EntityType.WireInterconnect;
            }
            else
            {
                switch (prim.Type.ToLower())
                {
                    case "rectangle":
                    case "polygon":
                        entity.Type = EntityType.Region;
                        break;
                    case "ellipse":
                        entity.Type = EntityType.Region;
                        break;
                    case "line":
                    case "polyline":
                        entity.Type = EntityType.WireInterconnect;
                        break;
                    default:
                        entity.Type = EntityType.WireInterconnect;
                        break;
                }
            }

            if (prim.Points != null && prim.Points.Count >= 4)
            {
                entity.LambdaX = prim.Points[0];
                entity.LambdaY = prim.Points[1];
                entity.LambdaEndX = prim.Points[prim.Points.Count - 2];
                entity.LambdaEndY = prim.Points[prim.Points.Count - 1];

                if (prim.Points.Count > 4)
                {
                    entity.PathPoints = new List<PointF>();
                    for (int i = 0; i < prim.Points.Count; i += 2)
                    {
                        if (i + 1 < prim.Points.Count)
                        {
                            entity.PathPoints.Add(new PointF(prim.Points[i], prim.Points[i + 1]));
                        }
                    }
                }
            }

            entity.WidthOverride = prim.StrokeWidth > 0 ? (int)prim.StrokeWidth : 1;

            return entity;
        }

        public static VectorPrimitiveData ToPrimitiveData(Entity entity, string userId)
        {
            var data = new VectorPrimitiveData
            {
                Id = entity.Label ?? Guid.NewGuid().ToString(),
                Type = entity.Type.ToString().ToLower(),
                CreatedBy = userId,
                StrokeColor = ColorTranslator.ToHtml(entity.ColorOverride),
                StrokeWidth = entity.WidthOverride,
                Points = new List<float>()
            };

            if (entity.PathPoints != null && entity.PathPoints.Count > 0)
            {
                foreach (var pt in entity.PathPoints)
                {
                    data.Points.Add((float)pt.X);
                    data.Points.Add((float)pt.Y);
                }
            }
            else
            {
                data.Points.Add(entity.LambdaX);
                data.Points.Add(entity.LambdaY);
                data.Points.Add(entity.LambdaEndX);
                data.Points.Add(entity.LambdaEndY);
            }

            return data;
        }

        public static VectorPrimitiveData CreateEntityBoxRegion(float x1, float y1, float x2, float y2, string strokeColor = "#FF0000")
        {
            return new VectorPrimitiveData
            {
                Id = Guid.NewGuid().ToString(),
                Type = "rectangle",
                Points = new List<float> { x1, y1, x2, y2 },
                StrokeColor = strokeColor,
                StrokeWidth = 2f,
                FillColor = "transparent",
                CreatedBy = "user"
            };
        }
    }
}
