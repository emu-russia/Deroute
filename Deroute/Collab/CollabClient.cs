using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    public delegate void CollabEntityEventHandler(object sender, EntityData e);
    public delegate void CollabUserEventHandler(object sender, string userId);
    public delegate void CollabLockEventHandler(object sender, LockData e);
    public delegate void CollabErrorEventHandler(object sender, string error);

    public class LockData
    {
        public string PrimitiveId { get; set; }
        public string LockedBy { get; set; }
        public bool IsLocked { get; set; }
    }

    /// <summary>
    /// CollabMCP client speaking the SignalR JSON protocol over a raw WebSocket:
    ///   * handshake {"protocol":"json","version":1} before any invocation
    ///   * every message is a JSON record terminated by 0x1E
    ///   * type 1 = invocation, type 3 = completion (request/response correlation via invocationId),
    ///     type 6 = ping (responded to, to keep the connection alive), type 7 = close
    /// The server is the SignalR hub at /collabhub (CollabMCP.Server); the payload is the full
    /// entity model (EntityData).
    /// </summary>
    public class CollabClient
    {
        private const byte RecordSeparator = 0x1E;
        private const int DefaultRequestTimeoutMs = 10000;

        private readonly CollabSettings _settings;
        private readonly ConcurrentDictionary<string, string> _userColors = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<JToken>> _pendingRequests =
            new ConcurrentDictionary<string, TaskCompletionSource<JToken>>();
        private readonly List<byte> _recvBuffer = new List<byte>();

        private ClientWebSocket _websocket;
        private CancellationTokenSource _cts;
        private bool _isRunning;
        private bool _manualDisconnect;
        private int _invocationCounter;
        private int _reconnectAttempts;

        /// <summary>JSON reader settings: keep date-like strings as plain ISO strings (dates are
        /// not converted to DateTime) and parse numbers as double/int.</summary>
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            Culture = CultureInfo.InvariantCulture
        };

        public event CollabEventHandler OnConnected;
        public event CollabEventHandler OnDisconnected;
        public event CollabUserEventHandler OnUserJoined;
        public event CollabUserEventHandler OnUserLeft;
        public event CollabEntityEventHandler OnEntityCreated;
        public event CollabEntityEventHandler OnEntityUpdated;
        public event CollabLockEventHandler OnEntityLocked;
        public event CollabLockEventHandler OnEntityUnlocked;
        public event CollabEntityEventHandler OnEntityDeleted;
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
            _manualDisconnect = false;
            try
            {
                _websocket = new ClientWebSocket();
                var uri = new Uri(_settings.ServerUrl.Replace("http://", "ws://").Replace("https://", "wss://") + "/collabhub");
                CollabDebug.Log($"Connecting to: {uri}");
                _websocket.Options.SetRequestHeader("X-Api-Key", _settings.ApiKey);
                await _websocket.ConnectAsync(uri, CancellationToken.None);
                _cts = new CancellationTokenSource();

                // SignalR handshake
                await SendStr("{\"protocol\":\"json\",\"version\":1}");
                var handshake = await ReadRecordAsync(_cts.Token);
                CollabDebug.LogIn($"Handshake response: {handshake}");
                var hsObj = ParseObject(handshake);
                if (hsObj != null && Get(hsObj, "error") != null && Get(hsObj, "error").Type != JTokenType.Null)
                    throw new Exception("Handshake failed: " + Get(hsObj, "error").ToString());

                _isRunning = true;
                _reconnectAttempts = 0;
                CollabDebug.Log("WebSocket connected, starting receive loop");
                Task.Run(() => ReceiveLoop(_cts.Token));

                if (!string.IsNullOrEmpty(_settings.SessionId))
                {
                    CollabDebug.LogOut($"JoinSession (sessionId={_settings.SessionId})");
                    await SendInvocationAsync("JoinSession", new object[] { _settings.SessionId, _settings.UserId });
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
            _manualDisconnect = true;
            _isRunning = false;
            _cts?.Cancel();
            try
            {
                if (_websocket?.State == WebSocketState.Open)
                {
                    await SendStr("{\"type\":7}"); // SignalR close message
                    await _websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnected", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                CollabDebug.LogErr($"DisconnectAsync: {ex.Message}");
            }
            OnDisconnected?.Invoke(this, EventArgs.Empty);
            CollabDebug.Log("DisconnectAsync complete");
        }

        // ---------------------------------------------------------------- receive

        private async Task ReceiveLoop(CancellationToken token)
        {
            CollabDebug.Log("ReceiveLoop started");
            try
            {
                while (_isRunning && !token.IsCancellationRequested)
                {
                    var record = await ReadRecordAsync(token);
                    await ProcessRecord(record, token);
                }
            }
            catch (OperationCanceledException)
            {
                CollabDebug.Log("ReceiveLoop cancelled");
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    CollabDebug.LogErr($"ReceiveLoop error: {ex.Message}");
                    OnError?.Invoke(this, "Connection error: " + ex.Message);
                }
            }
            finally
            {
                CollabDebug.Log("ReceiveLoop exited");
                if (!_manualDisconnect)
                {
                    OnDisconnected?.Invoke(this, EventArgs.Empty);
                    _ = Task.Run(() => TryReconnectAsync());
                }
            }
        }

        /// <summary>Reads one 0x1E-terminated JSON record, buffering partial chunks.</summary>
        private async Task<string> ReadRecordAsync(CancellationToken token)
        {
            var chunk = new byte[8192];
            while (true)
            {
                int sep = _recvBuffer.IndexOf(RecordSeparator);
                if (sep >= 0)
                {
                    var record = Encoding.UTF8.GetString(_recvBuffer.GetRange(0, sep).ToArray());
                    _recvBuffer.RemoveRange(0, sep + 1);
                    if (record.Length > 0) return record;
                    continue; // skip empty records
                }

                var result = await _websocket.ReceiveAsync(new ArraySegment<byte>(chunk), token);
                for (int i = 0; i < result.Count; i++)
                    _recvBuffer.Add(chunk[i]);
            }
        }

        private async Task ProcessRecord(string json, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            JObject obj = ParseObject(json);
            if (obj == null) return;

            var type = GetI(obj, "type");
            switch (type)
            {
                case 1: // server invocation (event)
                    var target = GetStr(obj, "target");
                    var args = Get(obj, "arguments") as JArray;
                    HandleServerInvocation(target, args);
                    break;

                case 3: // completion for one of our requests
                    HandleCompletion(obj);
                    break;

                case 6: // ping — respond to keep the connection alive
                    CollabDebug.LogIn("Ping");
                    await SendStr("{\"type\":6}");
                    break;

                case 7: // server close
                    CollabDebug.LogIn("Server close message");
                    _isRunning = false;
                    break;

                default:
                    CollabDebug.LogIn($"Unknown message type: {type}: {json}");
                    break;
            }
        }

        private void HandleCompletion(JObject obj)
        {
            var invId = GetStr(obj, "invocationId");
            if (string.IsNullOrEmpty(invId)) return;

            TaskCompletionSource<JToken> tcs;
            if (!_pendingRequests.TryRemove(invId, out tcs)) return;

            var err = Get(obj, "error");
            if (err != null && err.Type != JTokenType.Null)
            {
                tcs.TrySetException(new Exception("Server error: " + err.ToString()));
                return;
            }
            tcs.TrySetResult(Get(obj, "result"));
        }

        private void HandleServerInvocation(string target, JArray args)
        {
            if (string.IsNullOrEmpty(target)) return;
            CollabDebug.LogIn($"Server event: {target}");

            try
            {
                switch (target)
                {
                    case "OnUserJoined":
                        if (args == null || args.Count == 0) return;
                        var a0 = args[0];
                        if (a0 is JObject jo)
                        {
                            // Payload {userId, snapshot} sent to the joining caller.
                            var uid = GetStr(jo, "userId");
                            if (uid != null)
                            {
                                _userColors[uid] = GetUserColor(uid);
                                OnUserJoined?.Invoke(this, uid);
                            }
                            if (Get(jo, "snapshot") != null)
                                OnSnapshotReceived?.Invoke(this, EventArgs.Empty);
                        }
                        else if (a0.Type == JTokenType.String)
                        {
                            OnUserJoined?.Invoke(this, (string)a0);
                        }
                        break;

                    case "OnUserLeft":
                        if (args != null && args.Count > 0 && args[0].Type == JTokenType.String)
                            OnUserLeft?.Invoke(this, (string)args[0]);
                        break;

                    case "OnEntityCreated":
                        if (args != null && args.Count > 0 && args[0] is JObject)
                            OnEntityCreated?.Invoke(this, DescEntity((JObject)args[0]));
                        break;

                    case "OnEntityUpdated":
                        if (args != null && args.Count > 0 && args[0] is JObject)
                            OnEntityUpdated?.Invoke(this, DescEntity((JObject)args[0]));
                        break;

                    case "OnEntityLocked":
                        if (args != null && args.Count > 0 && args[0] is JObject locked)
                        {
                            var data = DescEntity(locked);
                            OnEntityLocked?.Invoke(this, new LockData { PrimitiveId = data.Id, LockedBy = data.LockedBy, IsLocked = !string.IsNullOrEmpty(data.LockedBy) });
                        }
                        break;

                    case "OnEntityUnlocked":
                        if (args != null && args.Count > 0 && args[0] is JObject unlocked)
                        {
                            var data = DescEntity(unlocked);
                            OnEntityUnlocked?.Invoke(this, new LockData { PrimitiveId = data.Id, LockedBy = null, IsLocked = false });
                        }
                        break;

                    case "OnEntityDeleted":
                        if (args != null && args.Count > 0 && args[0].Type == JTokenType.String)
                            OnEntityDeleted?.Invoke(this, new EntityData { Id = (string)args[0] });
                        break;

                    case "OnCanvasCleared":
                        OnCanvasCleared?.Invoke(this, EventArgs.Empty);
                        break;

                    case "OnPositionUpdated":
                        if (args != null && args.Count > 0 && args[0] is JObject pos)
                        {
                            var pid = GetStr(pos, "primitiveId");
                            var pts = Get(pos, "points") as JArray;
                            if (pid != null && pts != null)
                            {
                                var points = new List<float>();
                                foreach (var p in pts)
                                {
                                    var f = ToFloat(p);
                                    if (f.HasValue) points.Add(f.Value);
                                }
                                OnEntityUpdated?.Invoke(this, new EntityData { Id = pid, Points = points });
                            }
                        }
                        break;

                    case "OnEntityError":
                    case "OnSessionError":
                        if (args != null && args.Count > 0 && args[0].Type == JTokenType.String)
                            OnError?.Invoke(this, (string)args[0]);
                        break;

                    case "OnLockError":
                        if (args != null && args.Count > 0 && args[0] is JObject lockErr)
                        {
                            var err = Get(lockErr, "error") != null ? Get(lockErr, "error").ToString() : "Lock error";
                            OnError?.Invoke(this, err);
                        }
                        break;

                    default:
                        CollabDebug.LogIn($"Unknown server event: {target}");
                        break;
                }
            }
            catch (Exception ex)
            {
                CollabDebug.LogErr($"HandleServerInvocation error: {ex.Message}");
                OnError?.Invoke(this, "Event error: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------- send

        private async Task SendStr(string json)
        {
            if (_websocket?.State == WebSocketState.Open)
            {
                CollabDebug.LogOut($"Raw send ({json.Length} bytes)");
                var bytes = Encoding.UTF8.GetBytes(json);
                var payload = new byte[bytes.Length + 1];
                Array.Copy(bytes, payload, bytes.Length);
                payload[bytes.Length] = RecordSeparator;
                await _websocket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                CollabDebug.Log("SendStr failed: WebSocket not open, state=" + (_websocket?.State.ToString() ?? "null"));
            }
        }

        private async Task<string> SendInvocationAsync(string target, object[] args)
        {
            var id = Interlocked.Increment(ref _invocationCounter).ToString();
            var msg = new JObject
            {
                ["type"] = 1,
                ["invocationId"] = id,
                ["target"] = target,
                ["arguments"] = JArray.FromObject(args)
            };
            await SendStr(msg.ToString(Formatting.None));
            return id;
        }

        /// <summary>Sends a request and waits for its completion, correlating by invocationId.</summary>
        private async Task<JToken> SendRequestAsync(string target, object[] args, int timeoutMs = DefaultRequestTimeoutMs)
        {
            var id = Interlocked.Increment(ref _invocationCounter).ToString();
            var tcs = new TaskCompletionSource<JToken>();
            _pendingRequests[id] = tcs;
            try
            {
                var msg = new JObject
                {
                    ["type"] = 1,
                    ["invocationId"] = id,
                    ["target"] = target,
                    ["arguments"] = JArray.FromObject(args)
                };
                await SendStr(msg.ToString(Formatting.None));

                var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
                if (completed != tcs.Task)
                    throw new TimeoutException($"Request '{target}' timed out after {timeoutMs} ms");
                return await tcs.Task;
            }
            finally
            {
                _pendingRequests.TryRemove(id, out _);
            }
        }

        // ---------------------------------------------------------------- public API

        public async Task JoinSessionAsync(string sessionId)
        {
            CollabDebug.LogOut($"JoinSession (sessionId={sessionId})");
            _settings.SessionId = sessionId;
            if (IsConnected)
            {
                await SendInvocationAsync("JoinSession", new object[] { sessionId, _settings.UserId });
            }
            else
            {
                CollabDebug.LogErr("JoinSession failed: not connected");
            }
        }

        public async Task SendEntityCreatedAsync(EntityData entity, string parentId = "")
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return;
            CollabDebug.LogOut($"SendEntityCreated: id={entity?.Id}, type={entity?.Type}, session={_settings.SessionId}");
            await SendInvocationAsync("SendEntityCreated", new object[]
            {
                _settings.SessionId, SerializeEntity(entity), _settings.UserId, parentId ?? ""
            });
        }

        public async Task SendEntityUpdatedAsync(EntityData entity)
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return;
            CollabDebug.LogOut($"SendEntityUpdated: id={entity?.Id}, type={entity?.Type}, session={_settings.SessionId}");
            await SendInvocationAsync("SendEntityUpdated", new object[]
            {
                _settings.SessionId, SerializeEntity(entity), _settings.UserId
            });
        }

        public async Task SendEntityDeletedAsync(string entityId)
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return;
            CollabDebug.LogOut($"SendEntityDeleted: id={entityId}, session={_settings.SessionId}");
            await SendInvocationAsync("SendEntityDeleted", new object[] { _settings.SessionId, entityId });
        }

        public async Task SendPositionUpdateAsync(string entityId, List<float> points)
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return;
            CollabDebug.LogOut($"SendPositionUpdate: id={entityId}, points={points?.Count ?? 0}, session={_settings.SessionId}");
            await SendInvocationAsync("SendPositionUpdate", new object[]
            {
                _settings.SessionId, entityId, ToJArray(points), _settings.UserId
            });
        }

        public async Task LockEntityAsync(string entityId)
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return;
            CollabDebug.LogOut($"LockEntity: id={entityId}, session={_settings.SessionId}");
            await SendInvocationAsync("LockEntity", new object[] { _settings.SessionId, entityId });
        }

        public async Task UnlockEntityAsync(string entityId)
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return;
            CollabDebug.LogOut($"UnlockEntity: id={entityId}, session={_settings.SessionId}");
            await SendInvocationAsync("UnlockEntity", new object[] { _settings.SessionId, entityId });
        }

        public async Task<List<string>> GetConnectedUsersAsync()
        {
            var result = new List<string>();
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return result;
            CollabDebug.LogOut($"GetConnectedUsers (sessionId={_settings.SessionId})");
            try
            {
                var token = await SendRequestAsync("GetConnectedUsers", new object[] { _settings.SessionId });
                if (token is JArray arr)
                {
                    foreach (var u in arr)
                    {
                        if (u.Type == JTokenType.String)
                            result.Add((string)u);
                    }
                }
            }
            catch (Exception ex)
            {
                CollabDebug.LogErr($"GetConnectedUsers error: {ex.Message}");
            }
            CollabDebug.LogIn($"GetConnectedUsers result: {result.Count} users");
            return result;
        }

        public async Task<Dictionary<string, object>> GetSessionStateAsync()
        {
            if (!IsConnected || string.IsNullOrEmpty(_settings.SessionId)) return new Dictionary<string, object>();
            CollabDebug.LogOut($"GetSessionState (sessionId={_settings.SessionId})");
            try
            {
                var token = await SendRequestAsync("GetSessionState", new object[] { _settings.SessionId });
                var result = ToObjectDict(token as JObject) ?? new Dictionary<string, object>();
                CollabDebug.LogIn($"GetSessionState result: {result.Count} keys");
                return result;
            }
            catch (Exception ex)
            {
                CollabDebug.LogErr($"GetSessionState error: {ex.Message}");
                return new Dictionary<string, object>();
            }
        }

        // ---------------------------------------------------------------- HTTP helpers

        /// <summary>GET /api/sessions — lists session ids available on the server (REST, X-Api-Key).</summary>
        public async Task<List<string>> GetAvailableSessionsAsync()
        {
            var result = new List<string>();
            try
            {
                var url = _settings.ServerUrl.TrimEnd('/') + "/api/sessions";
                using (var client = new System.Net.WebClient())
                {
                    client.Headers[System.Net.HttpRequestHeader.Authorization] = "";
                    client.Headers["X-Api-Key"] = _settings.ApiKey;
                    var json = await client.DownloadStringTaskAsync(url);
                    var token = JsonConvert.DeserializeObject<JToken>(json, JsonSettings);
                    if (token is JArray arr)
                    {
                        foreach (var s in arr)
                        {
                            if (s.Type == JTokenType.String)
                                result.Add((string)s);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CollabDebug.LogErr($"GetAvailableSessionsAsync error: {ex.Message}");
            }
            return result;
        }

        /// <summary>GET /api/health — verifies the server is reachable (no API key required).</summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var url = _settings.ServerUrl.TrimEnd('/') + "/api/health";
                using (var client = new System.Net.WebClient())
                {
                    var json = await client.DownloadStringTaskAsync(url);
                    return !string.IsNullOrEmpty(json);
                }
            }
            catch (Exception ex)
            {
                CollabDebug.LogErr($"TestConnectionAsync error: {ex.Message}");
                return false;
            }
        }

        // ---------------------------------------------------------------- reconnect

        private async Task TryReconnectAsync()
        {
            if (_manualDisconnect || !_settings.Enabled) return;

            while (_reconnectAttempts < Math.Max(1, _settings.MaxReconnectAttempts))
            {
                _reconnectAttempts++;
                OnError?.Invoke(this, $"Connection lost. Reconnecting ({_reconnectAttempts}/{_settings.MaxReconnectAttempts})...");
                await Task.Delay(Math.Max(500, _settings.ReconnectDelayMs));
                if (_manualDisconnect) return;

                var ok = await ConnectAsync();
                if (ok) return;
            }

            OnError?.Invoke(this, "Reconnect failed. Use the status bar 'Reconnect' to retry.");
        }

        // ---------------------------------------------------------------- JSON helpers

        private static JObject ParseObject(string json)
        {
            try
            {
                var token = JsonConvert.DeserializeObject<JToken>(json, JsonSettings);
                return token as JObject;
            }
            catch (Exception ex)
            {
                CollabDebug.LogErr($"Parse error: {ex.Message}");
                return null;
            }
        }

        /// <summary>Case-insensitive property lookup: first exact match, then OrdinalIgnoreCase.</summary>
        private static JToken Get(JObject obj, string name)
        {
            if (obj == null) return null;
            JToken t;
            if (obj.TryGetValue(name, out t)) return t;
            foreach (var prop in obj.Properties())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                    return prop.Value;
            }
            return null;
        }

        private static string GetStr(JObject obj, string name)
        {
            var t = Get(obj, name);
            if (t == null || t.Type != JTokenType.String) return null;
            return (string)t;
        }

        private static float GetF(JObject obj, string name)
        {
            var t = Get(obj, name);
            if (t == null || t.Type == JTokenType.Null) return 0;
            switch (t.Type)
            {
                case JTokenType.Integer: return (float)(long)t;
                case JTokenType.Float: return (float)(double)t;
                case JTokenType.String:
                    float f;
                    if (float.TryParse((string)t, NumberStyles.Float, CultureInfo.InvariantCulture, out f)) return f;
                    return 0;
                default: return 0;
            }
        }

        private static int GetI(JObject obj, string name)
        {
            var t = Get(obj, name);
            if (t == null || t.Type == JTokenType.Null) return 0;
            switch (t.Type)
            {
                case JTokenType.Integer: return (int)(long)t;
                case JTokenType.Float: return (int)(double)t;
                case JTokenType.String:
                    int i;
                    if (int.TryParse((string)t, NumberStyles.Integer, CultureInfo.InvariantCulture, out i)) return i;
                    return 0;
                default: return 0;
            }
        }

        /// <summary>Parses an EntityDto JSON object into EntityData (flat points, nested children).</summary>
        private static EntityData DescEntity(JObject d)
        {
            var data = new EntityData
            {
                Id = GetStr(d, "Id"),
                Type = GetStr(d, "Type"),
                Label = GetStr(d, "Label"),
                LambdaX = GetF(d, "LambdaX"),
                LambdaY = GetF(d, "LambdaY"),
                LambdaEndX = GetF(d, "LambdaEndX"),
                LambdaEndY = GetF(d, "LambdaEndY"),
                LambdaWidth = GetF(d, "LambdaWidth"),
                LambdaHeight = GetF(d, "LambdaHeight"),
                Priority = GetI(d, "Priority"),
                WidthOverride = GetI(d, "WidthOverride"),
                ColorOverride = GetStr(d, "ColorOverride"),
                FontOverride = GetStr(d, "FontOverride"),
                LabelAlignment = GetStr(d, "LabelAlignment"),
                Module = GetStr(d, "Module"),
                Visible = GetBool(d, "Visible", true),
                CreatedBy = GetStr(d, "CreatedBy"),
                LockedBy = GetStr(d, "LockedBy"),
                LockedAt = GetStr(d, "LockedAt"),
                Version = GetI(d, "Version"),
                CreatedAt = GetStr(d, "CreatedAt"),
                UpdatedAt = GetStr(d, "UpdatedAt")
            };

            var pts = Get(d, "Points");
            if (pts is JArray ptsArr)
            {
                data.Points = new List<float>();
                foreach (var p in ptsArr)
                {
                    var f = ToFloat(p);
                    if (f.HasValue) data.Points.Add(f.Value);
                }
            }

            var bl = Get(d, "TraverseBlackList");
            if (bl is JArray blArr)
            {
                data.TraverseBlackList = new List<string>();
                foreach (var t in blArr)
                {
                    if (t.Type == JTokenType.String)
                        data.TraverseBlackList.Add((string)t);
                }
            }

            var children = Get(d, "Children");
            if (children is JArray chArr && chArr.Count > 0)
            {
                data.Children = new List<EntityData>();
                foreach (var child in chArr)
                {
                    if (child is JObject childObj)
                        data.Children.Add(DescEntity(childObj));
                }
            }

            return data;
        }

        private static bool GetBool(JObject obj, string name, bool def)
        {
            var t = Get(obj, name);
            if (t == null || t.Type == JTokenType.Null) return def;
            if (t.Type == JTokenType.Boolean) return (bool)t;
            bool b;
            return bool.TryParse(t.ToString(), out b) ? b : def;
        }

        /// <summary>Converts a numeric JToken (int/double/float, or numeric string) to float.</summary>
        private static float? ToFloat(JToken t)
        {
            if (t == null || t.Type == JTokenType.Null) return null;
            switch (t.Type)
            {
                case JTokenType.Integer: return (float)(long)t;
                case JTokenType.Float: return (float)(double)t;
                case JTokenType.String:
                    float f;
                    if (float.TryParse((string)t, NumberStyles.Float, CultureInfo.InvariantCulture, out f)) return f;
                    return null;
                default: return null;
            }
        }

        /// <summary>Serializes EntityData into the camelCase EntityDto JSON object.</summary>
        private static JObject SerializeEntity(EntityData e)
        {
            var obj = new JObject
            {
                ["id"] = e.Id,
                ["type"] = e.Type,
                ["label"] = e.Label,
                ["lambdaX"] = e.LambdaX,
                ["lambdaY"] = e.LambdaY,
                ["lambdaEndX"] = e.LambdaEndX,
                ["lambdaEndY"] = e.LambdaEndY,
                ["lambdaWidth"] = e.LambdaWidth,
                ["lambdaHeight"] = e.LambdaHeight,
                ["priority"] = e.Priority,
                ["widthOverride"] = e.WidthOverride,
                ["colorOverride"] = e.ColorOverride,
                ["fontOverride"] = e.FontOverride,
                ["labelAlignment"] = e.LabelAlignment ?? "GlobalSettings",
                ["points"] = ToJArray(e.Points),
                ["visible"] = e.Visible,
                ["module"] = e.Module,
                ["children"] = new JArray()
            };

            if (e.TraverseBlackList != null && e.TraverseBlackList.Count > 0)
            {
                var bl = new JArray();
                foreach (var t in e.TraverseBlackList)
                    bl.Add(t);
                obj["traverseBlackList"] = bl;
            }

            if (e.Children != null)
            {
                foreach (var child in e.Children)
                    ((JArray)obj["children"]).Add(SerializeEntity(child));
            }

            return obj;
        }

        private static JArray ToJArray(List<float> pts)
        {
            var arr = new JArray();
            if (pts != null)
            {
                foreach (var p in pts)
                    arr.Add(p);
            }
            return arr;
        }

        /// <summary>Recursively converts a JObject into plain CLR types (Dictionary/List/string/long/double/bool).</summary>
        private static Dictionary<string, object> ToObjectDict(JObject obj)
        {
            if (obj == null) return null;
            var result = new Dictionary<string, object>();
            foreach (var prop in obj.Properties())
            {
                result[prop.Name] = ToObjectValue(prop.Value);
            }
            return result;
        }

        private static object ToObjectValue(JToken t)
        {
            if (t == null || t.Type == JTokenType.Null) return null;
            switch (t.Type)
            {
                case JTokenType.Object: return ToObjectDict((JObject)t);
                case JTokenType.Array:
                    var list = new List<object>();
                    foreach (var item in (JArray)t)
                        list.Add(ToObjectValue(item));
                    return list;
                case JTokenType.String: return (string)t;
                case JTokenType.Integer: return (long)t;
                case JTokenType.Float: return (double)t;
                case JTokenType.Boolean: return (bool)t;
                default: return t.ToString();
            }
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
