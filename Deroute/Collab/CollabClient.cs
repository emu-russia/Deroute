using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DerouteSharp.Collab
{
    public delegate void CollabEventHandler(object sender, EventArgs e);
    public delegate void CollabPrimitiveEventHandler(object sender, VectorPrimitiveData e);
    public delegate void CollabUserEventHandler(object sender, string userId);
    public delegate void CollabLockEventHandler(object sender, LockData e);
    public delegate void CollabErrorEventHandler(object sender, string error);

    public class VectorPrimitiveData
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public List<float> Points { get; set; }
        public string StrokeColor { get; set; }
        public float StrokeWidth { get; set; }
        public string FillColor { get; set; }
        public string CreatedBy { get; set; }
        public string LockedBy { get; set; }
        public string LockedAt { get; set; }
        public int Version { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
    }

    public class LockData
    {
        public string PrimitiveId { get; set; }
        public string LockedBy { get; set; }
        public bool IsLocked { get; set; }
    }

    public class CollabClient
    {
        private readonly CollabSettings _settings;
        private readonly ConcurrentDictionary<string, string> _userColors = new ConcurrentDictionary<string, string>();
        private int _reconnectAttempts = 0;
        private ClientWebSocket _websocket;
        private CancellationTokenSource _cts;
        private bool _isRunning;

        public event CollabEventHandler OnConnected;
        public event CollabEventHandler OnDisconnected;
        public event CollabUserEventHandler OnUserJoined;
        public event CollabUserEventHandler OnUserLeft;
        public event CollabPrimitiveEventHandler OnPrimitiveCreated;
        public event CollabPrimitiveEventHandler OnPrimitiveUpdated;
        public event CollabLockEventHandler OnPrimitiveLocked;
        public event CollabLockEventHandler OnPrimitiveUnlocked;
        public event CollabPrimitiveEventHandler OnPrimitiveDeleted;
        public event CollabEventHandler OnCanvasCleared;
        public event CollabErrorEventHandler OnError;
        public event CollabEventHandler OnSnapshotReceived;

        public bool IsConnected => _websocket?.State == WebSocketState.Open;
        public int ReconnectAttempts => _reconnectAttempts;

        public CollabClient(CollabSettings settings)
        {
            _settings = settings;
            InitColors();
        }

        private void InitColors()
        {
            var colors = new[] { "#FF6B6B","#4ECDC4","#45B7D1","#96CEB4","#FFEAA7","#DDA0DD","#98D8C8","#F7DC6F","#BB8FCE","#85C1E9","#F8C471","#82E0AA","#F1948A","#85929E","#73C6B6" };
            for (int i = 0; i < 100; i++)
                _userColors["user_" + i] = colors[i % colors.Length];
        }

        public string GetUserColor(string userId)
        {
            string c;
            if (_userColors.TryGetValue(userId, out c)) return c;
            return _colors[Math.Abs(userId.GetHashCode()) % 15];
        }

        private static readonly string[] _colors = new[] { "#FF6B6B","#4ECDC4","#45B7D1","#96CEB4","#FFEAA7","#DDA0DD","#98D8C8","#F7DC6F","#BB8FCE","#85C1E9","#F8C471","#82E0AA","#F1948A","#85929E","#73C6B6" };

        public async Task<bool> ConnectAsync()
        {
            if (IsConnected) return true;
            try
            {
                _websocket = new ClientWebSocket();
                var uri = new Uri(_settings.ServerUrl.Replace("http://", "ws://").Replace("https://", "wss://") + "/collabhub");
                _websocket.Options.SetRequestHeader("X-Api-Key", _settings.ApiKey);
                await _websocket.ConnectAsync(uri, CancellationToken.None);
                _cts = new CancellationTokenSource();
                _isRunning = true;
                _reconnectAttempts = 0;
                Task.Run(() => ReceiveLoop(_cts.Token));
                if (!string.IsNullOrEmpty(_settings.SessionId))
                    await SendStr("{\"command\":\"JoinSession\",\"sessionId\":\"" + _settings.SessionId + "\",\"userId\":\"" + _settings.UserId + "\"}");
                OnConnected?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, "Connection failed: " + ex.Message);
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            _isRunning = false;
            _cts?.Cancel();
            _websocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnected", CancellationToken.None);
            OnDisconnected?.Invoke(this, EventArgs.Empty);
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[65536];
            while (_isRunning && !token.IsCancellationRequested)
            {
                try
                {
                    var ms = new System.IO.MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _websocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var reader = new System.IO.StreamReader(ms, Encoding.UTF8);
                    var json = reader.ReadToEnd();
                    reader.Dispose();
                    await ProcessMsg(json);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    if (_isRunning) { OnError?.Invoke(this, "Receive error: " + ex.Message); await Task.Delay(1000, token); }
                }
            }
        }

        private async Task ProcessMsg(string json)
        {
            try
            {
                var dict = MiniJson.Parse(json);
                if (!dict.TryGetValue("command", out var cmdObj) || !(cmdObj is string cmd)) return;

                switch (cmd)
                {
                    case "OnUserJoined":
                        if (dict.TryGetValue("userId", out var uObj) && uObj is string uid)
                        { _userColors[uid] = GetUserColor(uid); OnUserJoined?.Invoke(this, uid); }
                        break;
                    case "OnUserLeft":
                        if (dict.TryGetValue("userId", out uObj) && uObj is string u2) OnUserLeft?.Invoke(this, u2);
                        break;
                    case "OnPrimitiveCreated":
                    case "OnPrimitiveUpdated":
                    case "OnPrimitiveLocked":
                        if (dict.TryGetValue("primitive", out var pObj) && pObj is Dictionary<string, object> pd)
                        {
                            var data = DescPrim(pd);
                            if (cmd == "OnPrimitiveLocked")
                                OnPrimitiveLocked?.Invoke(this, new LockData { PrimitiveId = data.Id, LockedBy = data.LockedBy, IsLocked = data.LockedBy != null });
                            else if (cmd == "OnPrimitiveCreated")
                                OnPrimitiveCreated?.Invoke(this, data);
                            else
                                OnPrimitiveUpdated?.Invoke(this, data);
                        }
                        break;
                    case "OnPrimitiveUnlocked":
                        if (dict.TryGetValue("primitive", out pObj) && pObj is Dictionary<string, object> pd2)
                        {
                            var data = DescPrim(pd2);
                            OnPrimitiveUnlocked?.Invoke(this, new LockData { PrimitiveId = data.Id, LockedBy = null, IsLocked = false });
                        }
                        break;
                    case "OnPrimitiveDeleted":
                        if (dict.TryGetValue("primitiveId", out var pidObj) && pidObj is string pid)
                            OnPrimitiveDeleted?.Invoke(this, new VectorPrimitiveData { Id = pid });
                        break;
                    case "OnCanvasCleared": OnCanvasCleared?.Invoke(this, EventArgs.Empty); break;
                    case "OnPositionUpdated":
                        if (dict.TryGetValue("primitiveId", out pidObj) && pidObj is string pid2 &&
                            dict.TryGetValue("points", out var ptsObj) && ptsObj is List<object> pts)
                        {
                            var points = new List<float>();
                            foreach (var p in pts)
                            {
                                if (p is float f) points.Add(f);
                                else if (p is double d) points.Add((float)d);
                                else if (p is int i) points.Add(i);
                            }
                            OnPrimitiveUpdated?.Invoke(this, new VectorPrimitiveData { Id = pid2, Points = points });
                        }
                        break;
                    case "OnSnapshot": OnSnapshotReceived?.Invoke(this, EventArgs.Empty); break;
                    case "OnError":
                        if (dict.TryGetValue("error", out var eObj) && eObj is string err) OnError?.Invoke(this, err);
                        break;
                }
            }
            catch (Exception ex) { OnError?.Invoke(this, "Parse error: " + ex.Message); }
        }

        private VectorPrimitiveData DescPrim(Dictionary<string, object> d)
        {
            return new VectorPrimitiveData
            {
                Id = d.GetStr("Id"), Type = d.GetStr("Type"),
                Points = d.GetListF("Points"),
                StrokeColor = d.GetStr("StrokeColor"), StrokeWidth = d.GetF("StrokeWidth"),
                FillColor = d.GetStr("FillColor"), CreatedBy = d.GetStr("CreatedBy"),
                LockedBy = d.GetStr("LockedBy"), LockedAt = d.GetStr("LockedAt"),
                Version = d.GetI("Version"), CreatedAt = d.GetStr("CreatedAt"), UpdatedAt = d.GetStr("UpdatedAt")
            };
        }

        private async Task SendStr(string json)
        {
            if (_websocket?.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                await _websocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public async Task JoinSessionAsync(string sessionId)
        {
            _settings.SessionId = sessionId;
            if (IsConnected) await SendStr("{\"command\":\"JoinSession\",\"sessionId\":\"" + sessionId + "\",\"userId\":\"" + _settings.UserId + "\"}");
        }

        public async Task SendPrimitiveCreatedAsync(string type, List<float> points, string strokeColor = "#000000", float strokeWidth = 1f, string fillColor = "transparent")
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return;
            var id = Guid.NewGuid().ToString();
            await SendStr("{\"command\":\"SendPrimitiveCreated\",\"sessionId\":\"" + _settings.SessionId + "\",\"primitiveId\":\"" + id + "\",\"type\":\"" + type + "\",\"points\":" + ArrF(points) + ",\"strokeColor\":\"" + strokeColor + "\",\"strokeWidth\":" + strokeWidth + ",\"fillColor\":\"" + fillColor + "\",\"userId\":\"" + _settings.UserId + "\"}");
        }

        public async Task SendPrimitiveUpdatedAsync(string primitiveId, List<float> points, string strokeColor = "#000000", float strokeWidth = 1f, string fillColor = "transparent")
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return;
            await SendStr("{\"command\":\"SendPrimitiveUpdated\",\"sessionId\":\"" + _settings.SessionId + "\",\"primitiveId\":\"" + primitiveId + "\",\"points\":" + ArrF(points) + ",\"strokeColor\":\"" + strokeColor + "\",\"strokeWidth\":" + strokeWidth + ",\"fillColor\":\"" + fillColor + "\",\"userId\":\"" + _settings.UserId + "\"}");
        }

        public async Task SendPositionUpdateAsync(string primitiveId, List<float> points)
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return;
            await SendStr("{\"command\":\"SendPositionUpdate\",\"sessionId\":\"" + _settings.SessionId + "\",\"primitiveId\":\"" + primitiveId + "\",\"points\":" + ArrF(points) + ",\"userId\":\"" + _settings.UserId + "\"}");
        }

        public async Task LockPrimitiveAsync(string primitiveId)
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return;
            await SendStr("{\"command\":\"LockPrimitive\",\"sessionId\":\"" + _settings.SessionId + "\",\"primitiveId\":\"" + primitiveId + "\"}");
        }

        public async Task UnlockPrimitiveAsync(string primitiveId)
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return;
            await SendStr("{\"command\":\"UnlockPrimitive\",\"sessionId\":\"" + _settings.SessionId + "\",\"primitiveId\":\"" + primitiveId + "\"}");
        }

        public async Task<List<string>> GetConnectedUsersAsync()
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return new List<string>();
            try
            {
                var r = await SendReq("GetConnectedUsers", _settings.SessionId);
                return r != null && r.ContainsKey("users") ? r["users"] as List<string> : new List<string>();
            }
            catch { return new List<string>(); }
        }

        public async Task<Dictionary<string, object>> GetSessionStateAsync()
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return new Dictionary<string, object>();
            try { return await SendReq("GetSessionState", _settings.SessionId) ?? new Dictionary<string, object>(); }
            catch { return new Dictionary<string, object>(); }
        }

        private async Task<Dictionary<string, object>> SendReq(string cmd, string sid)
        {
            if (!IsConnected) return null;
            await SendStr("{\"command\":\"" + cmd + "\",\"parameters\":{\"sessionId\":\"" + sid + "\"}}");
            return null;
        }

        private static string ArrF(List<float> pts)
        {
            if (pts == null || pts.Count == 0) return "[]";
            return "[" + string.Join(",", pts) + "]";
        }

        public class SessionMetadata
        {
            public string SessionId { get; set; }
            public string BackgroundImageId { get; set; }
            public string BackgroundImageUrl { get; set; }
            public int? ImageWidth { get; set; }
            public int? ImageHeight { get; set; }
            public string CreatedAt { get; set; }
            public string LastActivity { get; set; }
        }
    }

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
