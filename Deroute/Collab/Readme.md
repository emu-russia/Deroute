# Collab Client Specification (Deroute/Collab)

## Overview

**Collab** is a real-time collaborative canvas module for the DerouteSharp application. It enables synchronization of vector graphics between multiple users via a WebSocket connection to the CollabMCP.Server. The module allows several users to simultaneously create, edit, move, and lock vector primitives on a shared canvas with instant synchronization.

**Technology Stack:**
- **Language:** C# (.NET Framework / .NET Core)
- **Protocol:** WebSocket (SignalR Protocol)
- **Data Format:** JSON (MiniJson parser)
- **UI:** Windows Forms
- **Conversion:** Entity ↔ VectorPrimitiveData

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

### Integration Methods

| Method | Description |
|--------|-------------|
| `InitializeCollab()` | Setup all event handlers, create throttler/queue/timer, auto-connect |
| `UpdateCollabStatus(status, userCount)` | Update status in UI with color coding |
| `RefreshCollabStatus()` | Periodic status refresh (every 5 sec) |
| `ApplyRemotePrimitive(data)` | Apply remotely created primitive to canvas |
| `ApplyRemoteUpdate(data)` | Apply remotely updated primitive |
| `ApplyRemoteLock(lockData)` | Visualize lock (user color overlay) |
| `ApplyRemoteUnlock(lockData)` | Remove lock visualization |
| `ApplyRemoteDelete(data)` | Remove primitive from canvas |
| `InvokeOnUiThread(action)` | Thread-safe UI method invocation |
| `QueueOfflineChange(change)` | Add change to offline queue |
| `FlushOfflineChanges()` | Send accumulated offline changes |

### Status Color Coding

| Status | Color |
|--------|-------|
| `Connected` | `Green` |
| `Error` / `Disconnected` | `Red` |
| Other (Connecting, etc.) | `Orange` |

### Lock Visualization

When a primitive is locked:
1. Original color saved to `_entityOriginalColors`
2. Lock user's color retrieved from palette (`GetUserColor`)
3. Primitive redrawn with `Color.FromArgb(150, lockColor)` — semi-transparent overlay
4. On unlock, original color is restored

---

## Exchange Protocol (JSON Commands)

### Client → Server

| Command | Parameters | Description |
|----------|-----------|-------------|
| `JoinSession` | `sessionId`, `userId` | Join a session |
| `SendPrimitiveCreated` | `sessionId`, `primitiveId`, `type`, `points`, `strokeColor`, `strokeWidth`, `fillColor`, `userId` | Create primitive |
| `SendPrimitiveUpdated` | `sessionId`, `primitiveId`, `points`, `strokeColor`, `strokeWidth`, `fillColor`, `userId` | Update primitive |
| `SendPositionUpdate` | `sessionId`, `primitiveId`, `points`, `userId` | Update position |
| `LockPrimitive` | `sessionId`, `primitiveId` | Lock primitive |
| `UnlockPrimitive` | `sessionId`, `primitiveId` | Unlock primitive |
| `GetConnectedUsers` | `sessionId` | Request connected users |
| `GetSessionState` | `sessionId` | Request session state |

### Server → Client

| Command | Parameters | Description |
|----------|-----------|-------------|
| `OnUserJoined` | `userId`, `snapshot` | User joined (with snapshot) |
| `OnUserLeft` | `userId` | User left session |
| `OnPrimitiveCreated` | `primitive` | New primitive created |
| `OnPrimitiveUpdated` | `primitive` | Primitive updated |
| `OnPrimitiveLocked` | `primitive` | Primitive locked |
| `OnPrimitiveUnlocked` | `primitive` | Primitive unlocked |
| `OnPrimitiveDeleted` | `primitiveId` | Primitive deleted |
| `OnCanvasCleared` | — | Canvas cleared |
| `OnPositionUpdated` | `primitiveId`, `points` | Position update |
| `OnSnapshot` | — | Full state snapshot |
| `OnError` | `error` | Error |

---

## Primitive Data Format (JSON)

```json
{
  "Id": "a1b2c3d4-e5f6-...",
  "Type": "rectangle",
  "Points": [
    { "X": 100.0, "Y": 50.0 },
    { "X": 300.0, "Y": 200.0 }
  ],
  "StrokeColor": "#FF6B6B",
  "StrokeWidth": 2.0,
  "FillColor": "transparent",
  "CreatedBy": "user_a",
  "LockedBy": "user_b",
  "LockedAt": "2025-01-15T10:30:00.0000000Z",
  "Version": 5,
  "CreatedAt": "2025-01-15T10:00:00.0000000Z",
  "UpdatedAt": "2025-01-15T10:30:00.0000000Z"
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

CollabMCP settings are available in the "CollabMCP" tab of application settings via `PropertyGrid`.

| Parameter | Description |
|-----------|-------------|
| `Enabled` | Enable collaboration |
| `ServerUrl` | Server URL |
| `ApiKey` | API key |
| `UserId` | User ID |
| `SessionId` | Session ID |
| `Username` | Username |
| `ReconnectDelayMs` | Reconnection delay |
| `MaxReconnectAttempts` | Max reconnection attempts |

---

## Reconnection Mechanism

1. On connection loss, client fires `OnError` event
2. `FormMainCollab` updates status in UI (red color)
3. User can right-click on status bar → "Reconnect"
4. `ConnectAsync()` is called for reconnection
5. After connection, `GetSessionStateAsync()` is automatically requested for synchronization
6. Received snapshot is applied to canvas via `ApplyRemotePrimitive`

---

## Offline Changes Mechanism

1. When disconnected, user continues working on canvas
2. All changes (primitive creation, updates) are added to `_offlineQueue`
3. On reconnection, `FlushOfflineChanges()` sends accumulated changes to server
4. Each change is sent as corresponding command (`SendPrimitiveCreatedAsync` / `SendPrimitiveUpdatedAsync`)

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
| `MiniJson` | JSON parsing |
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
