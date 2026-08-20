using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;

namespace DerouteSharp.Collab
{
    /// <summary>
    /// Client-side wire model of the server EntityDto: the full application entity
    /// (all EntityType values, hierarchy, colors, fonts, priorities), plus collab fields.
    /// Points are a flat list [x1, y1, x2, y2, ...].
    /// </summary>
    public class EntityData
    {
        public string Id { get; set; }
        public string Type { get; set; }              // EntityType enum name, e.g. "ViasInput"
        public string Label { get; set; }
        public float LambdaX { get; set; }
        public float LambdaY { get; set; }
        public float LambdaEndX { get; set; }
        public float LambdaEndY { get; set; }
        public float LambdaWidth { get; set; }
        public float LambdaHeight { get; set; }
        public int Priority { get; set; }
        public int WidthOverride { get; set; }
        public string ColorOverride { get; set; }     // "#RRGGBB"
        public string FontOverride { get; set; }
        public string LabelAlignment { get; set; }    // TextAlignment enum name
        public List<float> Points { get; set; }       // flat [x1, y1, ...]
        public List<string> TraverseBlackList { get; set; }
        public string Module { get; set; }
        public bool Visible { get; set; } = true;
        public List<EntityData> Children { get; set; }
        public string CreatedBy { get; set; }
        public string LockedBy { get; set; }
        public string LockedAt { get; set; }
        public int Version { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
    }

    /// <summary>
    /// Bidirectional, lossless conversion between the wire model (EntityData) and the
    /// application model (EntityBox.Entity). All EntityType values are preserved.
    /// </summary>
    public static class EntityConverter
    {
        public static Entity ToEntity(EntityData data, string userId)
        {
            var entity = new Entity
            {
                Label = data.Label,
                CollabId = data.Id,
                UserData = (userId ?? string.Empty).GetHashCode(),
                SelectTimeStamp = DateTime.Now.Ticks,
                Type = ParseEntityType(data.Type),
                LambdaX = data.LambdaX,
                LambdaY = data.LambdaY,
                LambdaEndX = data.LambdaEndX,
                LambdaEndY = data.LambdaEndY,
                LambdaWidth = data.LambdaWidth,
                LambdaHeight = data.LambdaHeight,
                Priority = data.Priority,
                WidthOverride = data.WidthOverride > 0 ? data.WidthOverride : 1,
                LabelAlignment = ParseTextAlignment(data.LabelAlignment),
                Module = data.Module,
                Visible = data.Visible
            };

            if (!string.IsNullOrEmpty(data.ColorOverride))
            {
                try { entity.ColorOverride = ColorTranslator.FromHtml(data.ColorOverride); }
                catch { entity.ColorOverride = Color.Empty; }
            }

            if (!string.IsNullOrEmpty(data.FontOverride))
            {
                try { entity.FontOverride = FontXmlConverter.ConvertToFont(data.FontOverride); }
                catch { entity.FontOverride = null; }
            }

            if (data.Points != null && data.Points.Count >= 2)
            {
                entity.PathPoints = new List<PointF>();
                for (int i = 0; i + 1 < data.Points.Count; i += 2)
                    entity.PathPoints.Add(new PointF(data.Points[i], data.Points[i + 1]));

                if (data.LambdaX == 0 && data.LambdaY == 0 && data.LambdaEndX == 0 && data.LambdaEndY == 0)
                {
                    entity.LambdaX = entity.PathPoints[0].X;
                    entity.LambdaY = entity.PathPoints[0].Y;
                    entity.LambdaEndX = entity.PathPoints[entity.PathPoints.Count - 1].X;
                    entity.LambdaEndY = entity.PathPoints[entity.PathPoints.Count - 1].Y;
                }
            }

            if (data.TraverseBlackList != null && data.TraverseBlackList.Count > 0)
            {
                entity.TraverseBlackList = new List<EntityType>();
                foreach (var t in data.TraverseBlackList)
                {
                    EntityType parsed;
                    if (Enum.TryParse(t, true, out parsed))
                        entity.TraverseBlackList.Add(parsed);
                }
            }

            if (data.Children != null && data.Children.Count > 0)
            {
                foreach (var child in data.Children)
                {
                    var childEntity = ToEntity(child, userId);
                    childEntity.parent = entity;
                    entity.Children.Add(childEntity);
                }
            }

            return entity;
        }

        public static EntityData ToEntityData(Entity entity, string userId)
        {
            var data = new EntityData
            {
                Id = entity.CollabId ?? entity.Label ?? Guid.NewGuid().ToString(),
                Type = entity.Type.ToString(),
                Label = entity.Label,
                LambdaX = entity.LambdaX,
                LambdaY = entity.LambdaY,
                LambdaEndX = entity.LambdaEndX,
                LambdaEndY = entity.LambdaEndY,
                LambdaWidth = entity.LambdaWidth,
                LambdaHeight = entity.LambdaHeight,
                Priority = entity.Priority,
                WidthOverride = entity.WidthOverride,
                LabelAlignment = entity.LabelAlignment.ToString(),
                Module = entity.Module,
                Visible = entity.Visible,
                CreatedBy = userId,
                Points = new List<float>()
            };

            if (entity.ColorOverride != Color.Empty)
            {
                try { data.ColorOverride = ColorTranslator.ToHtml(entity.ColorOverride); }
                catch { data.ColorOverride = null; }
            }

            if (entity.FontOverride != null)
            {
                try { data.FontOverride = FontXmlConverter.ConvertToString(entity.FontOverride); }
                catch { data.FontOverride = null; }
            }

            if (entity.PathPoints != null && entity.PathPoints.Count > 0)
            {
                foreach (var pt in entity.PathPoints)
                {
                    data.Points.Add(pt.X);
                    data.Points.Add(pt.Y);
                }
            }
            else
            {
                data.Points.Add(entity.LambdaX);
                data.Points.Add(entity.LambdaY);
                data.Points.Add(entity.LambdaEndX);
                data.Points.Add(entity.LambdaEndY);
            }

            if (entity.TraverseBlackList != null && entity.TraverseBlackList.Count > 0)
            {
                data.TraverseBlackList = new List<string>();
                foreach (var t in entity.TraverseBlackList)
                    data.TraverseBlackList.Add(t.ToString());
            }

            if (entity.Children != null && entity.Children.Count > 0)
            {
                data.Children = new List<EntityData>();
                foreach (var child in entity.Children)
                    data.Children.Add(ToEntityData(child, userId));
            }

            return data;
        }

        /// <summary>Copies all application-model fields from a converted entity onto an existing
        /// entity (used for remote updates, preserving the object identity in the canvas tree).</summary>
        public static void CopyTo(Entity source, Entity target)
        {
            target.Label = source.Label;
            target.CollabId = source.CollabId;
            target.Type = source.Type;
            target.LambdaX = source.LambdaX;
            target.LambdaY = source.LambdaY;
            target.LambdaEndX = source.LambdaEndX;
            target.LambdaEndY = source.LambdaEndY;
            target.LambdaWidth = source.LambdaWidth;
            target.LambdaHeight = source.LambdaHeight;
            target.Priority = source.Priority;
            target.WidthOverride = source.WidthOverride;
            target.ColorOverride = source.ColorOverride;
            target.FontOverride = source.FontOverride;
            target.LabelAlignment = source.LabelAlignment;
            target.PathPoints = source.PathPoints;
            target.TraverseBlackList = source.TraverseBlackList;
            target.Module = source.Module;
            target.Visible = source.Visible;
            target.Children.Clear();
            foreach (var child in source.Children)
            {
                child.parent = target;
                target.Children.Add(child);
            }
        }

        /// <summary>
        /// Converts a plain CLR dictionary (from GetSessionStateAsync, camelCase keys) into
        /// EntityData, including nested children. Points may be a flat numeric list or a list
        /// of {x,y} objects (case-insensitive).
        /// </summary>
        public static EntityData FromDict(Dictionary<string, object> dict)
        {
            if (dict == null) return null;

            var data = new EntityData
            {
                Id = GetStr(dict, "id"),
                Type = GetStr(dict, "type"),
                Label = GetStr(dict, "label"),
                LambdaX = GetF(dict, "lambdaX"),
                LambdaY = GetF(dict, "lambdaY"),
                LambdaEndX = GetF(dict, "lambdaEndX"),
                LambdaEndY = GetF(dict, "lambdaEndY"),
                LambdaWidth = GetF(dict, "lambdaWidth"),
                LambdaHeight = GetF(dict, "lambdaHeight"),
                Priority = GetI(dict, "priority"),
                WidthOverride = GetI(dict, "widthOverride"),
                ColorOverride = GetStr(dict, "colorOverride"),
                FontOverride = GetStr(dict, "fontOverride"),
                LabelAlignment = GetStr(dict, "labelAlignment"),
                Module = GetStr(dict, "module"),
                Visible = GetBool(dict, "visible", true),
                CreatedBy = GetStr(dict, "createdBy"),
                LockedBy = GetStr(dict, "lockedBy"),
                LockedAt = GetStr(dict, "lockedAt"),
                Version = GetI(dict, "version"),
                CreatedAt = GetStr(dict, "createdAt"),
                UpdatedAt = GetStr(dict, "updatedAt"),
                Points = new List<float>()
            };

            var points = Get(dict, "points") as System.Collections.IEnumerable;
            if (points != null)
            {
                foreach (var item in points)
                {
                    var pt = item as IDictionary<string, object>;
                    if (pt != null)
                    {
                        data.Points.Add(GetF(pt, "x"));
                        data.Points.Add(GetF(pt, "y"));
                    }
                    else
                    {
                        data.Points.Add(ToF(item));
                    }
                }
            }

            var blackList = Get(dict, "traverseBlackList") as System.Collections.IEnumerable;
            if (blackList != null)
            {
                data.TraverseBlackList = new List<string>();
                foreach (var item in blackList)
                {
                    var s = item as string;
                    if (!string.IsNullOrEmpty(s))
                        data.TraverseBlackList.Add(s);
                }
            }

            var children = Get(dict, "children") as System.Collections.IEnumerable;
            if (children != null)
            {
                data.Children = new List<EntityData>();
                foreach (var item in children)
                {
                    var childDict = item as Dictionary<string, object>;
                    if (childDict != null)
                    {
                        var child = FromDict(childDict);
                        if (child != null)
                            data.Children.Add(child);
                    }
                }
            }

            return data;
        }

        private static object Get(IDictionary<string, object> dict, string key)
        {
            object v;
            if (dict.TryGetValue(key, out v)) return v;
            foreach (var kvp in dict)
            {
                if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }
            return null;
        }

        private static string GetStr(IDictionary<string, object> dict, string key)
        {
            return Get(dict, key) as string;
        }

        private static float GetF(IDictionary<string, object> dict, string key)
        {
            var v = Get(dict, key);
            if (v == null) return 0;
            try { return Convert.ToSingle(v, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static int GetI(IDictionary<string, object> dict, string key)
        {
            var v = Get(dict, key);
            if (v == null) return 0;
            try { return Convert.ToInt32(v, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static bool GetBool(IDictionary<string, object> dict, string key, bool def)
        {
            var v = Get(dict, key);
            if (v == null) return def;
            if (v is bool b) return b;
            bool parsed;
            return bool.TryParse(v.ToString(), out parsed) ? parsed : def;
        }

        private static float ToF(object v)
        {
            if (v == null) return 0;
            try { return Convert.ToSingle(v, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static EntityType ParseEntityType(string type)
        {
            if (string.IsNullOrEmpty(type)) return EntityType.WireInterconnect;
            EntityType parsed;
            return Enum.TryParse(type, true, out parsed) ? parsed : EntityType.WireInterconnect;
        }

        private static TextAlignment ParseTextAlignment(string alignment)
        {
            if (string.IsNullOrEmpty(alignment)) return TextAlignment.GlobalSettings;
            TextAlignment parsed;
            return Enum.TryParse(alignment, true, out parsed) ? parsed : TextAlignment.GlobalSettings;
        }
    }
}
