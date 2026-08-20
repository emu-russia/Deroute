# Collab Client Specification (Deroute/Collab)

## Overview

**Collab** is a real-time collaborative canvas module for the DerouteSharp application. It enables synchronization of vector graphics between multiple users via a WebSocket connection to the CollabMCP.Server. The module allows several users to simultaneously create, edit, move, and lock vector primitives on a shared canvas with instant synchronization.

**Technology Stack:**
- **Language:** C# (.NET Framework / .NET Core)
- **Protocol:** WebSocket (SignalR Protocol)
- **Data Format:** JSON (Newtonsoft.Json / Json.NET)
- **UI:** Windows Forms
- **Conversion:** Entity ↔ EntityData

## Build Notes (Newtonsoft.Json dependency)

`Newtonsoft.Json` 13.0.4 is declared in `Deroute/packages.config` and restored into the
(untracked) `Deroute/packages` folder — no DLL is committed to the repository.

- **Visual Studio:** packages.config is restored automatically on build.
- **Command line (MSBuild):**
  ```
  msbuild Deroute\DerouteSharp.sln /t:Restore /p:RestorePackagesConfig=true
  msbuild Deroute\DerouteSharp.sln /t:Build /p:Configuration=Debug
  ```

---

## Architecture

### Component Diagram

```
+------------------+     +------------------+
|  DerouteSharp    |     | CollabMCP.Server |
|  (Desktop Client)|     |  (Backend Server)|
|                  |     |                  |
|  +--------------+|-----||---> WebSocket  |
|  | CollabClient ||     |     (SignalR)    |
|  +--------------+|     |                  |
|  | EntityConverter||   |  +-----------+   |
|  +--------------+|     |  | SessionMgr|   |
|  | OfflineQueue ||     |  +-----------+   |
|  +--------------+|     |                  |
|  | CoordinateThrottler|   |  +-----------+   |
|  +--------------+|     |  | XmlStore  |   |
|  | FormMainCollab ||    |  +-----------+   |
|  +--------------+|     |                  |
+------------------+     +------------------+
          |                        |
          v                        v
   +-------------+         +-------------+
   |  Canvas UI  |         |  XML Files  |
   | (EntityBox) |         |  (Sessions) |
   +-------------+         +-------------+
```

### Data Flow

```
User A                      Server                      User B
     |                             |                             |
     |-- SendPrimitiveCreated --->|                             |
     |                             |-- OnPrimitiveCreated --->  |
     |                             |                             |-- Apply to canvas
     |                             |                             |
     |-- LockPrimitive ---------> |                             |
     |                             |-- OnPrimitiveLocked ---->  |
     |                             |                             |-- Show overlay
     |                             |                             |
     |                             |<--- Position Update --------|
     |<-- OnPositionUpdated ------ |                             |
     |-- Apply update ----------->|                             |
```

---

## Client Components

### 1. CollabSettings

Configuration class storing server connection parameters.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | bool | `false` | Whether collaboration mode is enabled |
| `ServerUrl` | string | `http://localhost:5000` | CollabMCP server URL |
| `ApiKey` | string | `""` | API key for authentication |
| `UserId` | string | auto-generated | Unique user ID (ticks) |
| `SessionId` | string | `""` | Current session ID |
| `Username` | string | `Environment.UserName` | System username |
| `ReconnectDelayMs` | int | `2000` | Reconnection delay (ms) |
| `MaxReconnectAttempts` | int | `50` | Max reconnection attempts |

### 2. CollabClient

Main client class managing the WebSocket connection and event handling.

#### Properties

| Property | Type | Description |
|-----------|------|-------------|
| `IsConnected` | bool | Connection status (`WebSocketState.Open`) |
| `ReconnectAttempts` | int | Current reconnection attempt count |
| `_userColors` | ConcurrentDictionary | User color dictionary (15-color palette) |

#### Events

| Event | Delegate | Description |
|-------|----------|-------------|
| `OnConnected` | `CollabEventHandler` | Successful server connection |
| `OnDisconnected` | `CollabEventHandler` | Connection lost |
| `OnUserJoined` | `CollabUserEventHandler` | User joined session |
| `OnUserLeft` | `CollabUserEventHandler` | User left session |
| `OnPrimitiveCreated` | `CollabPrimitiveEventHandler` | New primitive created |
| `OnPrimitiveUpdated` | `CollabPrimitiveEventHandler` | Existing primitive updated |
| `OnPrimitiveLocked` | `CollabLockEventHandler` | Primitive locked |
| `OnPrimitiveUnlocked` | `CollabLockEventHandler` | Primitive unlocked |
| `OnPrimitiveDeleted` | `CollabPrimitiveEventHandler` | Primitive deleted |
| `OnCanvasCleared` | `CollabEventHandler` | Canvas cleared |
| `OnSnapshotReceived` | `CollabEventHandler` | Full state snapshot received |
| `OnError` | `CollabErrorEventHandler` | Error occurred |

#### Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `ConnectAsync()` | `Task<bool>` | Establish WebSocket connection to server |
| `DisconnectAsync()` | `Task` | Close the connection |
| `JoinSessionAsync(sessionId)` | `Task` | Join a session |
| `SendPrimitiveCreatedAsync(type, points, ...)` | `Task` | Send primitive creation |
| `SendPrimitiveUpdatedAsync(primitiveId, points, ...)` | `Task` | Send primitive update |
| `SendPositionUpdateAsync(primitiveId, points)` | `Task` | Send position update |
| `LockPrimitiveAsync(primitiveId)` | `Task` | Lock a primitive |
| `UnlockPrimitiveAsync(primitiveId)` | `Task` | Unlock a primitive |
| `GetConnectedUsersAsync()` | `Task<List<string>>` | Get connected users list |
| `GetSessionStateAsync()` | `Task<Dictionary<string, object>>` | Get full session state |
| `GetUserColor(userId)` | `string` | Get user color (from palette) |

#### Internal Methods

| Method | Description |
|--------|-------------|
| `ReceiveLoop(token)` | WebSocket message reading loop |
| `ProcessMsg(json)` | JSON parsing, event dispatch |
| `SendStr(json)` | Send JSON string via WebSocket |
| `DescPrim(dict)` | Deserialize `Dictionary<string, object>` to `VectorPrimitiveData` |
| `InitColors()` | Initialize 15-color user palette |

### 3. VectorPrimitiveData

Client-side primitive data model (string/float types).

| Property | Type | Description |
|----------|------|-------------|
| `Id` | string | Primitive GUID |
| `Type` | string | Type: rectangle/polygon/ellipse/line/polyline |
| `Points` | List<float> | Point coordinates [x1, y1, x2, y2, ...] |
| `StrokeColor` | string | Stroke color (HEX, e.g. `#000000`) |
| `StrokeWidth` | float | Stroke width |
| `FillColor` | string | Fill color (`transparent` or HEX) |
| `CreatedBy` | string | Creator ID |
| `LockedBy` | string | Locking user ID |
| `LockedAt` | string | Lock date (ISO 8601) |
| `Version` | int | Primitive version |
| `CreatedAt` | string | Creation date (ISO 8601) |
| `UpdatedAt` | string | Update date (ISO 8601) |

### 4. LockData

Lock data model.

| Property | Type | Description |
|----------|------|-------------|
| `PrimitiveId` | string | Primitive GUID |
| `LockedBy` | string | User ID who locked the primitive |
| `IsLocked` | bool | Whether the primitive is locked |

### 5. EntityConverter

Static class for converting between client primitives and internal Deroute entities.

#### Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `ToEntity(prim, userId)` | `Entity` | Convert `VectorPrimitiveData` to `Entity` |
| `ToPrimitiveData(entity, userId)` | `VectorPrimitiveData` | Convert `Entity` to `VectorPrimitiveData` |
| `CreateEntityBoxRegion(x1, y1, x2, y2, color)` | `VectorPrimitiveData` | Create rectangular primitive for Region |

#### Type Mapping

| `VectorPrimitiveData.Type` | `EntityType` |
|---------------------------|-------------|
| `rectangle`, `polygon` | `EntityType.Region` |
| `ellipse` | `EntityType.Region` |
| `line`, `polyline` | `EntityType.WireInterconnect` |
| `null` / `""` / other | `EntityType.WireInterconnect` |

### 6. OfflineQueue

Class for storing changes made while disconnected from the server, to be sent after reconnection.

| Member | Type | Description |
|--------|------|-------------|
| `_queue` | `ConcurrentQueue<OfflineChange>` | Queue of pending changes |
| `Count` | int | Number of items in queue |
| `Add(change)` | void | Add a change to the queue |
| `Flush()` | `List<OfflineChange>` | Dequeue all changes and clear |
| `Clear()` | void | Clear queue without sending |

#### OfflineChange

| Property | Type | Description |
|----------|------|-------------|
| `Type` | string | Type: `created` / `updated` |
| `PrimitiveId` | string | Primitive GUID |
| `SessionId` | string | Session ID |
| `Points` | List<float> | Point coordinates |
| `StrokeColor` | string | Stroke color |
| `StrokeWidth` | float | Stroke width |
| `FillColor` | string | Fill color |
| `Timestamp` | DateTime | Creation time (UTC) |

### 7. CoordinateThrottler

Timer-based throttle for position updates, preventing excessive server requests.

| Member | Type | Description |
|--------|------|-------------|
| `OnFlush` | `Action<List<PositionUpdate>>` | Event fired when buffered updates are flushed |
| `_timer` | `Timer` | Internal timer (default interval 33ms = ~30 FPS) |
| `AddUpdate(primitiveId, points)` | void | Add update (deduplication by primitiveId) |
| `Stop()` | void | Stop the timer |

#### PositionUpdate

| Property | Type | Description |
|----------|------|-------------|
| `PrimitiveId` | string | Primitive GUID |
| `Points` | List<float> | New coordinates |
| `Timestamp` | DateTime | Update time (UTC) |

**How it works:**
1. On `AddUpdate` call, check if an update already exists for the same `primitiveId`
2. If found — replace it (deduplication)
3. On timer tick (33ms), all buffered updates are flushed via `OnFlush` event
4. Each update is sent to the server separately

---

## UI Integration (FormMainCollab)

### Fields

| Field | Type | Description |
|-------|------|-------------|
| `_collabClient` | `CollabClient` | Main collaboration client |
| `_positionThrottler` | `CoordinateThrottler` | Position updates throttle |
| `_offlineQueue` | `OfflineChangeQueue` | Offline changes queue |
| `_collabStatusTimer` | `Timer` | Status update timer (5 sec) |
| `_collabUserCount` | int | Number of users in session |
| `_isSyncing` | bool | Sync flag (blocks events during sync) |
| `_entityOriginalColors` | `Dictionary<string, Color>` | Original entity colors |
| `_entityLockOwners` | `Dictionary<string, string>` | Lock owners (entityId → userId) |

### Status Controls

| Control | Description |
|---------|-------------|
| `collabStatusIndicator` | `ToolStripStatusLabel` — connection state with color coding (replaces the old combo box, whose `DropDownList` could not display dynamic text) |
| `collabStatusMessage` | `ToolStripStatusLabel` (spring) — latest CollabMCP message/error with full text |
| `collabStatusContextMenu` | Right-click on the indicator: **Connect / Disconnect / Reconnect**, separator, **Session...**, **Users...** |

### Integration Methods

| Method | Description |
|--------|-------------|
| `InitializeCollab()` | Setup all event handlers, create throttler/queue/timer, auto-connect |
| `UpdateCollabStatus(status, userCount)` | Update the status indicator with color coding |
| `SetStatusMessage(message)` | Show a transient message/error in `collabStatusMessage` |
| `RefreshCollabStatus()` | Periodic status refresh (every 5 sec) |
| `ApplyRemoteEntity(data)` | Apply a remotely created entity (full model) to canvas |
| `ApplyRemoteUpdate(data)` | Apply a remotely updated entity (or a position delta) |
| `ApplyRemoteLock(lockData)` | Visualize lock (user color overlay) |
| `ApplyRemoteUnlock(lockData)` | Remove lock visualization |
| `ApplyRemoteDelete(data)` | Remove entity (and its subtree) from canvas |
| `InvokeOnUiThread(action)` | Thread-safe UI method invocation |
| `QueueOfflineChange(change)` | Add change to offline queue |
| `FlushOfflineChanges()` | Send accumulated offline changes |

### Status Color Coding

| Status | Color |
|--------|-------|
| `Connected` | `Green` |
| `Error` / `Disconnected` | `Red` |
| `Connecting` / reconnecting | `Orange` |
| `Disabled` | `Gray` |

### Lock Visualization

When an entity is locked:
1. Original color saved to `_entityOriginalColors`
2. Lock user's color retrieved from palette (`GetUserColor`)
3. Entity redrawn with `Color.FromArgb(150, lockColor)` — semi-transparent overlay
4. On unlock, original color is restored

---

## Exchange Protocol (SignalR JSON Protocol)

The client connects to the server's SignalR hub at `/collabhub` over a raw WebSocket and speaks the
**SignalR JSON protocol**:

1. **Handshake:** the first message is `{"protocol":"json","version":1}` terminated by `0x1E`.
   The server replies with `{}` on success.
2. **Framing:** every JSON record is terminated by the record separator byte `0x1E`.
   Several records may arrive in a single WebSocket frame.
3. **Client → server calls** are `type: 1` invocations:
   `{"type":1,"invocationId":"N","target":"<Method>","arguments":[...]}`.
   Void calls get a `type: 3` completion; request/response calls (e.g. `GetSessionState`)
   return their result in the matching completion.
4. **Server → client events** are `type: 1` messages with a `target` equal to the event name.
5. **Pings:** the server sends `{"type":6}` keep-alive pings; the client answers with `{"type":6}`.
6. **Close:** `{"type":7}` closes the connection gracefully.

### Client → Server (hub methods, `type:1` invocations)

| Target | Arguments | Description |
|----------|-----------|-------------|
| `JoinSession` | `[sessionId, userId]` | Join a session |
| `SendEntityCreated` | `[sessionId, entityDto, userId, parentId]` | Create an entity (full DTO; `parentId` empty for top-level) |
| `SendEntityUpdated` | `[sessionId, entityDto, userId]` | Update an entity (full DTO) |
| `SendEntityDeleted` | `[sessionId, entityId]` | Delete an entity (with its subtree) |
| `SendPositionUpdate` | `[sessionId, entityId, points, userId]` | Real-time position update (`points` — flat array) |
| `LockEntity` | `[sessionId, entityId]` | Lock an entity |
| `UnlockEntity` | `[sessionId, entityId]` | Unlock an entity |
| `GetConnectedUsers` | `[sessionId]` | Returns `List<string>` in the completion result |
| `GetSessionState` | `[sessionId]` | Returns session state in the completion result |
| `GetHistory` | `[sessionId, count]` | Returns operation history |

### Server → Client (events, `type:1` with target)

| Event | Arguments[0] | Description |
|----------|-----------|-------------|
| `OnUserJoined` | `{userId, snapshot}` (to joining caller) or `userId` (group broadcast) | User joined; the joining client receives the full snapshot |
| `OnUserLeft` | `userId` | User left session |
| `OnEntityCreated` | entity DTO | New entity created |
| `OnEntityUpdated` | entity DTO | Entity updated |
| `OnEntityLocked` | entity DTO | Entity locked (`lockedBy` set) |
| `OnEntityUnlocked` | entity DTO | Entity unlocked |
| `OnEntityDeleted` | `entityId` | Entity deleted |
| `OnCanvasCleared` | — | Canvas cleared |
| `OnPositionUpdated` | `{primitiveId, points}` | Real-time position update |
| `OnEntityError` | `error` | Entity operation error |
| `OnLockError` | `{entityId, error}` | Lock operation error |
| `OnSessionError` | `error` | Session-level error |

On `OnUserJoined` with a snapshot, the client fires `OnSnapshotReceived`; the UI then fetches the
full state via `GetSessionStateAsync()` (which correlates request/response by `invocationId`).

---

## Entity Data Format (JSON, server wire format)

The server transmits the full entity model (`EntityDto`). Keys are camelCase; `points` is a flat
array of coordinate pairs `[x1, y1, x2, y2, ...]`; children are nested recursively:

```json
{
  "id": "a1b2c3d4-e5f6-...",
  "type": "ViasInput",
  "label": "IN1",
  "lambdaX": 100.0,
  "lambdaY": 50.0,
  "lambdaEndX": 300.0,
  "lambdaEndY": 200.0,
  "lambdaWidth": 0.0,
  "lambdaHeight": 0.0,
  "priority": 0,
  "widthOverride": 2,
  "colorOverride": "#FF6B6B",
  "fontOverride": null,
  "labelAlignment": "GlobalSettings",
  "points": [100.0, 50.0, 300.0, 200.0],
  "traverseBlackList": [],
  "module": null,
  "visible": true,
  "children": [
    {
      "id": "...",
      "type": "ViasOutput",
      "label": "OUT1",
      "points": [300.0, 200.0, 300.0, 200.0],
      "children": []
    }
  ],
  "createdBy": "user_a",
  "lockedBy": "user_b",
  "lockedAt": "2025-01-15T10:30:00.0000000Z",
  "version": 5,
  "createdAt": "2025-01-15T10:00:00.0000000Z",
  "updatedAt": "2025-01-15T10:30:00.0000000Z"
}
```

---

## User Palette

15-color palette for user identification in collaboration:

| # | Color | HEX |
|---|-------|-----|
| 1 | 🔴 Red | `#FF6B6B` |
| 2 | 🟢 Teal | `#4ECDC4` |
| 3 | 🔵 Blue | `#45B7D1` |
| 4 | 🟢 Green | `#96CEB4` |
| 5 | 🟡 Yellow | `#FFEAA7` |
| 6 | 🟣 Pink | `#DDA0DD` |
| 7 | 🟢 Light Green | `#98D8C8` |
| 8 | 🟡 Lemon | `#F7DC6F` |
| 9 | 🟣 Purple | `#BB8FCE` |
| 10 | 🔵 Light Blue | `#85C1E9` |
| 11 | 🟠 Orange | `#F8C471` |
| 12 | 🟡 Light Green | `#82E0AA` |
| 13 | 🔴 Coral | `#F1948A` |
| 14 | ⚫ Blue-Gray | `#85929E` |
| 15 | 🟢 Mint | `#73C6B6` |

---

## Configuration (CollabMCP in FormSettings)

The "CollabMCP" tab of the application settings opens the dedicated **`FormCollabSettings`**
dialog (masked API key, connection test, session picker) instead of a raw PropertyGrid.

| Parameter | Description |
|-----------|-------------|
| `Enabled` | Enable collaboration |
| `ServerUrl` | Server URL |
| `ApiKey` | API key (masked, with "Show" toggle) |
| `UserId` | User ID (auto-generated) |
| `SessionId` | Session ID (editable or picked via `FormCollabSession`) |
| `Username` | Username |
| `ReconnectDelayMs` | Reconnection delay |
| `MaxReconnectAttempts` | Max reconnection attempts |

`FormCollabSession` lists the sessions available on the server (`GET /api/sessions`), lets you
join an existing one, create a new id, or copy it. Choosing a session reconnects without
restarting the application. `FormCollabUsers` shows connected participants with their palette
color and the 15-color legend.

---

## Reconnection Mechanism

1. On connection loss, the receive loop exits and `OnDisconnected` fires
2. `CollabClient` automatically retries: up to `MaxReconnectAttempts` times with
   `ReconnectDelayMs` delay between attempts (only while `Enabled` and not manually disconnected)
3. During reconnection `OnError` reports the attempt counter (`Reconnecting 2/50...`)
4. The user can also manually reconnect via the status bar "Reconnect" → `ConnectAsync()`
5. On a successful reconnection, the server sends the session snapshot inside
   `OnUserJoined`; `OnSnapshotReceived` fires and `GetSessionStateAsync()` (request/response
   correlated by `invocationId`) returns the full state, applied to the canvas

---

## Offline Changes Mechanism

1. While disconnected (and collaboration enabled), local canvas changes are added to `_offlineQueue`
2. On reconnection (`OnConnected`), `FlushOfflineChanges()` sends the accumulated changes:
   `created` → `SendPrimitiveCreatedAsync`, `updated` → `SendPrimitiveUpdatedAsync`,
   `deleted` → `SendPrimitiveDeletedAsync`
3. While connected, local additions/removals are published to the server immediately

---

## Security

| Mechanism | Description |
|-----------|-------------|
| API Key | All server requests include `X-Api-Key` header |
| Validation | Server validates key via `ApiKeyAuthMiddleware` |
| Locking | Primitive can only be modified by the locking user |
| Versioning | Each primitive has `Version` for change tracking |

---

## Dependencies

| Dependency | Purpose |
|------------|---------|
| `System.Net.WebSockets` | WebSocket client |
| `Newtonsoft.Json` | JSON parsing (Json.NET) |
| `System.Drawing` | Color operations (`ColorTranslator`) |
| `System.Windows.Forms.Timer` | UI timers |
| `System.Collections.Concurrent` | Thread-safe collections |

---

## Development Notes

1. **Thread Safety:** All operations on `_offlineQueue`, `_pendingUpdates` use `ConcurrentDictionary` / `ConcurrentQueue`
2. **UI Calls:** All UI updates go through `InvokeOnUiThread` to prevent cross-thread exceptions
3. **Throttling:** Position updates throttle (33ms = ~30 FPS) prevents excessive server load
4. **Deduplication:** CoordinateThrottler merges multiple updates of the same primitive into one
5. **Auto-unlock:** On user disconnect, server automatically releases all their locks
