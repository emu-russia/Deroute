using System;
using System.Collections.Generic;
using System.Text;

namespace DerouteSharp
{
    internal static class MiniJson
    {
        public static Dictionary<string, object> Parse(string json)
        {
            var dict = new Dictionary<string, object>();
            var arr = new List<object>();
            var pos = 0;
            var result = ParseValue(json, ref pos);
            if (result is Dictionary<string, object> d) return d;
            return dict;
        }

        private static object ParseValue(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length) return null;

            if (json[pos] == '{') return ParseObject(json, ref pos);
            if (json[pos] == '[') return ParseArray(json, ref pos);
            if (json[pos] == '"') return ParseString(json, ref pos);
            if (json[pos] == 't' || json[pos] == 'f') return ParseBool(json, ref pos);
            if (json[pos] == 'n') return ParseNull(json, ref pos);
            return ParseNumber(json, ref pos);
        }

        private static Dictionary<string, object> ParseObject(string json, ref int pos)
        {
            var dict = new Dictionary<string, object>();
            pos++; // skip {
            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == '}') { pos++; return dict; }

            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                if (json[pos] == '"')
                {
                    var key = ParseString(json, ref pos);
                    SkipWhitespace(json, ref pos);
                    if (pos < json.Length && json[pos] == ':') pos++;
                    SkipWhitespace(json, ref pos);
                    var val = ParseValue(json, ref pos);
                    dict[key] = val;
                }
                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',') { pos++; continue; }
                if (pos < json.Length && json[pos] == '}') { pos++; break; }
            }
            return dict;
        }

        private static List<object> ParseArray(string json, ref int pos)
        {
            var list = new List<object>();
            pos++; // skip [
            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == ']') { pos++; return list; }

            while (pos < json.Length)
            {
                var val = ParseValue(json, ref pos);
                list.Add(val);
                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',') { pos++; continue; }
                if (pos < json.Length && json[pos] == ']') { pos++; break; }
            }
            return list;
        }

        private static string ParseString(string json, ref int pos)
        {
            pos++; // skip "
            var sb = new StringBuilder();
            while (pos < json.Length && json[pos] != '"')
            {
                if (json[pos] == '\\')
                {
                    pos++;
                    if (pos < json.Length)
                    {
                        switch (json[pos])
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'n': sb.Append('\n'); break;
                            case 't': sb.Append('\t'); break;
                            case 'r': sb.Append('\r'); break;
                            default: sb.Append(json[pos]); break;
                        }
                        pos++;
                    }
                }
                else
                {
                    sb.Append(json[pos]);
                    pos++;
                }
            }
            if (pos < json.Length) pos++; // skip closing "
            return sb.ToString();
        }

        private static bool ParseBool(string json, ref int pos)
        {
            if (json.Substring(pos, 4) == "true") { pos += 4; return true; }
            if (json.Substring(pos, 5) == "false") { pos += 5; return false; }
            pos++; return false;
        }

        private static object ParseNull(string json, ref int pos)
        {
            if (json.Substring(pos, 4) == "null") { pos += 4; return null; }
            pos++; return null;
        }

        private static object ParseNumber(string json, ref int pos)
        {
            var start = pos;
            bool isFloat = false;
            while (pos < json.Length && (char.IsDigit(json[pos]) || json[pos] == '.' || json[pos] == '-' || json[pos] == '+' || json[pos] == 'e' || json[pos] == 'E'))
            {
                if (json[pos] == '.' || json[pos] == 'e' || json[pos] == 'E' || json[pos] == '+' || json[pos] == '-') isFloat = true;
                pos++;
            }
            var numStr = json.Substring(start, pos - start);
            if (isFloat) return float.Parse(numStr);
            return int.Parse(numStr);
        }

        private static void SkipWhitespace(string json, ref int pos)
        {
            while (pos < json.Length && (json[pos] == ' ' || json[pos] == '\t' || json[pos] == '\n' || json[pos] == '\r')) pos++;
        }
    }

    internal static class DictExt
    {
        public static string GetStr(this Dictionary<string, object> d, string k)
        {
            if (d.TryGetValue(k, out var v) && v is string s) return s;
            return null;
        }

        public static float GetF(this Dictionary<string, object> d, string k)
        {
            if (d.TryGetValue(k, out var v))
            {
                if (v is float f) return f;
                if (v is double db) return (float)db;
                if (v is int i) return i;
            }
            return 0;
        }

        public static int GetI(this Dictionary<string, object> d, string k)
        {
            if (d.TryGetValue(k, out var v))
            {
                if (v is int i) return i;
                if (v is double db) return (int)db;
            }
            return 0;
        }

        public static List<float> GetListF(this Dictionary<string, object> d, string k)
        {
            if (d.TryGetValue(k, out var v) && v is List<object> list)
            {
                var result = new List<float>();
                foreach (var item in list)
                {
                    if (item is float f) result.Add(f);
                    else if (item is double db) result.Add((float)db);
                    else if (item is int i) result.Add(i);
                }
                return result;
            }
            return null;
        }
    }
}
