using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DerouteSharp.Collab
{
    internal static class CollabDebug
    {
#if DEBUG && (!__MonoCS__)
        public static void Log(string msg) => Console.WriteLine("[Collab] " + msg);
        public static void LogOut(string msg) => Console.WriteLine("[Collab->Srv] " + msg);
        public static void LogIn(string msg) => Console.WriteLine("[Collab<-Srv] " + msg);
        public static void LogErr(string msg) => Console.WriteLine("[Collab ERR] " + msg);
#else
        public static void Log(string msg) { }
        public static void LogOut(string msg) { }
        public static void LogIn(string msg) { }
        public static void LogErr(string msg) { }
#endif
    }

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
            CollabDebug.Log($"ConnectAsync start (session={_settings.SessionId}, userId={_settings.UserId})");
            try
            {
                _websocket = new ClientWebSocket();
                var uri = new Uri(_settings.ServerUrl.Replace("http://", "ws://").Replace("https://", "wss://") + "/collabhub");
                CollabDebug.Log($"Connecting to: {uri}");
                _websocket.Options.SetRequestHeader("X-Api-Key", _settings.ApiKey);
                await _websocket.ConnectAsync(uri, CancellationToken.None);
                _cts = new CancellationTokenSource();
                _isRunning = true;
                _reconnectAttempts = 0;
                CollabDebug.Log("WebSocket connected, starting receive loop");
                Task.Run(() => ReceiveLoop(_cts.Token));
                if (!string.IsNullOrEmpty(_settings.SessionId))
                {
                    var joinMsg = "{\"command\":\"JoinSession\",\"sessionId\":\"" + _settings.SessionId + "\",\"userId\":\"" + _settings.UserId + "\"}";
                    CollabDebug.LogOut($"JoinSession (sessionId={_settings.SessionId})");
                    await SendStr(joinMsg);
                }
                OnConnected?.Invoke(this, EventArgs.Empty);
                CollabDebug.Log("ConnectAsync success");
                return true;
            }
            catch (Exception ex)
            {
                CollabDebug.LogErr($"ConnectAsync failed: {ex.Message}");
                OnError?.Invoke(this, "Connection failed: " + ex.Message);
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            CollabDebug.Log("DisconnectAsync called");
            _isRunning = false;
            _cts?.Cancel();
            _websocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnected", CancellationToken.None);
            OnDisconnected?.Invoke(this, EventArgs.Empty);
            CollabDebug.Log("DisconnectAsync complete");
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[65536];
            CollabDebug.Log("ReceiveLoop started");
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
                    CollabDebug.LogIn($"Raw message ({json.Length} bytes)");
                    await ProcessMsg(json);
                }
                catch (OperationCanceledException) { CollabDebug.Log("ReceiveLoop cancelled"); break; }
                catch (Exception ex)
                {
                    if (_isRunning) { CollabDebug.LogErr($"ReceiveLoop error: {ex.Message}"); OnError?.Invoke(this, "Receive error: " + ex.Message); await Task.Delay(1000, token); }
                }
            }
            CollabDebug.Log("ReceiveLoop exited");
        }

        private async Task ProcessMsg(string json)
        {
            try
            {
                var dict = MiniJson.Parse(json);
                if (!dict.TryGetValue("command", out var cmdObj) || !(cmdObj is string cmd)) return;

                CollabDebug.LogIn($"Command: {cmd}");

                switch (cmd)
                {
                    case "OnUserJoined":
                        if (dict.TryGetValue("userId", out var uObj) && uObj is string uid)
                        {
                            CollabDebug.LogIn($"OnUserJoined: userId={uid}");
                            _userColors[uid] = GetUserColor(uid);
                            OnUserJoined?.Invoke(this, uid);
                        }
                        break;
                    case "OnUserLeft":
                        if (dict.TryGetValue("userId", out uObj) && uObj is string u2)
                        {
                            CollabDebug.LogIn($"OnUserLeft: userId={u2}");
                            OnUserLeft?.Invoke(this, u2);
                        }
                        break;
                    case "OnPrimitiveCreated":
                    case "OnPrimitiveUpdated":
                    case "OnPrimitiveLocked":
                        if (dict.TryGetValue("primitive", out var pObj) && pObj is Dictionary<string, object> pd)
                        {
                            var data = DescPrim(pd);
                            if (cmd == "OnPrimitiveLocked")
                            {
                                CollabDebug.LogIn($"OnPrimitiveLocked: primitiveId={data.Id}, lockedBy={data.LockedBy}");
                                OnPrimitiveLocked?.Invoke(this, new LockData { PrimitiveId = data.Id, LockedBy = data.LockedBy, IsLocked = data.LockedBy != null });
                            }
                            else if (cmd == "OnPrimitiveCreated")
                            {
                                CollabDebug.LogIn($"OnPrimitiveCreated: primitiveId={data.Id}, type={data.Type}, createdBy={data.CreatedBy}");
                                OnPrimitiveCreated?.Invoke(this, data);
                            }
                            else
                            {
                                CollabDebug.LogIn($"OnPrimitiveUpdated: primitiveId={data.Id}");
                                OnPrimitiveUpdated?.Invoke(this, data);
                            }
                        }
                        break;
                    case "OnPrimitiveUnlocked":
                        if (dict.TryGetValue("primitive", out pObj) && pObj is Dictionary<string, object> pd2)
                        {
                            var data = DescPrim(pd2);
                            CollabDebug.LogIn($"OnPrimitiveUnlocked: primitiveId={data.Id}");
                            OnPrimitiveUnlocked?.Invoke(this, new LockData { PrimitiveId = data.Id, LockedBy = null, IsLocked = false });
                        }
                        break;
                    case "OnPrimitiveDeleted":
                        if (dict.TryGetValue("primitiveId", out var pidObj) && pidObj is string pid)
                        {
                            CollabDebug.LogIn($"OnPrimitiveDeleted: primitiveId={pid}");
                            OnPrimitiveDeleted?.Invoke(this, new VectorPrimitiveData { Id = pid });
                        }
                        break;
                    case "OnCanvasCleared":
                        CollabDebug.LogIn("OnCanvasCleared");
                        OnCanvasCleared?.Invoke(this, EventArgs.Empty);
                        break;
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
                            CollabDebug.LogIn($"OnPositionUpdated: primitiveId={pid2}, pointsCount={points.Count}");
                            OnPrimitiveUpdated?.Invoke(this, new VectorPrimitiveData { Id = pid2, Points = points });
                        }
                        break;
                    case "OnSnapshot":
                        CollabDebug.LogIn("OnSnapshot");
                        OnSnapshotReceived?.Invoke(this, EventArgs.Empty);
                        break;
                    case "OnError":
                        if (dict.TryGetValue("error", out var eObj) && eObj is string err)
                        {
                            CollabDebug.LogErr($"OnError from server: {err}");
                            OnError?.Invoke(this, err);
                        }
                        break;
                    default:
                        CollabDebug.LogIn($"Unknown command: {cmd}");
                        break;
                }
            }
            catch (Exception ex)
            {
                CollabDebug.LogErr($"ProcessMsg error: {ex.Message}");
                OnError?.Invoke(this, "Parse error: " + ex.Message);
            }
        }

        private VectorPrimitiveData DescPrim(Dictionary<string, object> d)
        {
            var id = d.GetStr("Id");
            var type = d.GetStr("Type");
            CollabDebug.LogIn($"DescPrim: id={id}, type={type}, points={d.GetListF("Points")?.Count ?? 0}");
            return new VectorPrimitiveData
            {
                Id = id, Type = type,
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
                CollabDebug.LogOut($"Raw send ({json.Length} bytes)");
                var bytes = Encoding.UTF8.GetBytes(json);
                await _websocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                CollabDebug.Log("SendStr failed: WebSocket not open, state=" + (_websocket?.State.ToString() ?? "null"));
            }
        }

        public async Task JoinSessionAsync(string sessionId)
        {
            CollabDebug.LogOut($"JoinSession (sessionId={sessionId})");
            _settings.SessionId = sessionId;
            if (IsConnected)
            {
                await SendStr("{\"command\":\"JoinSession\",\"sessionId\":\"" + sessionId + "\",\"userId\":\"" + _settings.UserId + "\"}");
            }
            else
            {
                CollabDebug.LogErr("JoinSession failed: not connected");
            }
        }

        public async Task SendPrimitiveCreatedAsync(string type, List<float> points, string strokeColor = "#000000", float strokeWidth = 1f, string fillColor = "transparent")
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return;
            var id = Guid.NewGuid().ToString();
            CollabDebug.LogOut($"SendPrimitiveCreated: primitiveId={id}, type={type}, points={points?.Count ?? 0}, session={_settings.SessionId}");
            await SendStr("{\"command\":\"SendPrimitiveCreated\",\"sessionId\":\"" + _settings.SessionId + "\",\"primitiveId\":\"" + id + "\",\"type\":\"" + type + "\",\"points\":" + ArrF(points) + ",\"strokeColor\":\"" + strokeColor + "\",\"strokeWidth\":" + strokeWidth + ",\"fillColor\":\"" + fillColor + "\",\"userId\":\"" + _settings.UserId + "\"}");
        }

        public async Task SendPrimitiveUpdatedAsync(string primitiveId, List<float> points, string strokeColor = "#000000", float strokeWidth = 1f, string fillColor = "transparent")
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return;
            CollabDebug.LogOut($"SendPrimitiveUpdated: primitiveId={primitiveId}, points={points?.Count ?? 0}, session={_settings.SessionId}");
            await SendStr("{\"command\":\"SendPrimitiveUpdated\",\"sessionId\":\"" + _settings.SessionId + "\",\"primitiveId\":\"" + primitiveId + "\",\"points\":" + ArrF(points) + ",\"strokeColor\":\"" + strokeColor + "\",\"strokeWidth\":" + strokeWidth + ",\"fillColor\":\"" + fillColor + "\",\"userId\":\"" + _settings.UserId + "\"}");
        }

        public async Task SendPositionUpdateAsync(string primitiveId, List<float> points)
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return;
            CollabDebug.LogOut($"SendPositionUpdate: primitiveId={primitiveId}, points={points?.Count ?? 0}, session={_settings.SessionId}");
            await SendStr("{\"command\":\"SendPositionUpdate\",\"sessionId\":\"" + _settings.SessionId + "\",\"primitiveId\":\"" + primitiveId + "\",\"points\":" + ArrF(points) + ",\"userId\":\"" + _settings.UserId + "\"}");
        }

        public async Task LockPrimitiveAsync(string primitiveId)
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return;
            CollabDebug.LogOut($"LockPrimitive: primitiveId={primitiveId}, session={_settings.SessionId}");
            await SendStr("{\"command\":\"LockPrimitive\",\"sessionId\":\"" + _settings.SessionId + "\",\"primitiveId\":\"" + primitiveId + "\"}");
        }

        public async Task UnlockPrimitiveAsync(string primitiveId)
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return;
            CollabDebug.LogOut($"UnlockPrimitive: primitiveId={primitiveId}, session={_settings.SessionId}");
            await SendStr("{\"command\":\"UnlockPrimitive\",\"sessionId\":\"" + _settings.SessionId + "\",\"primitiveId\":\"" + primitiveId + "\"}");
        }

        public async Task<List<string>> GetConnectedUsersAsync()
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return new List<string>();
            CollabDebug.LogOut($"GetConnectedUsers (sessionId={_settings.SessionId})");
            try
            {
                var r = await SendReq("GetConnectedUsers", _settings.SessionId);
                var users = r != null && r.ContainsKey("users") ? r["users"] as List<string> : new List<string>();
                CollabDebug.LogIn($"GetConnectedUsers result: {users?.Count ?? 0} users");
                return users;
            }
            catch (Exception ex)
            {
                CollabDebug.LogErr($"GetConnectedUsers error: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<Dictionary<string, object>> GetSessionStateAsync()
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return new Dictionary<string, object>();
            CollabDebug.LogOut($"GetSessionState (sessionId={_settings.SessionId})");
            try
            {
                var result = await SendReq("GetSessionState", _settings.SessionId) ?? new Dictionary<string, object>();
                CollabDebug.LogIn($"GetSessionState result: {result.Count} keys");
                return result;
            }
            catch (Exception ex)
            {
                CollabDebug.LogErr($"GetSessionState error: {ex.Message}");
                return new Dictionary<string, object>();
            }
        }

        private async Task<Dictionary<string, object>> SendReq(string cmd, string sid)
        {
            if (!IsConnected) return null;
            CollabDebug.LogOut($"SendReq: cmd={cmd}, sessionId={sid}");
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
}
