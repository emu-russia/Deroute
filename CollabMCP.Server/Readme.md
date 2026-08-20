# CollabMCP Server Specification

## Overview

**CollabMCP Server** is a real-time collaborative canvas server built on .NET 10.0 that provides a Model Context Protocol (MCP) interface for multi-user vector graphics collaboration. It enables multiple users and AI agents to simultaneously create, edit, lock, and manage vector primitives on shared canvases with real-time synchronization via SignalR and persistent storage via XML files.

**Technology Stack:**
- **Framework:** ASP.NET Core 10.0 (.NET 10.0)
- **Real-time Communication:** Microsoft.AspNetCore.SignalR
- **Logging:** Serilog with console and daily rolling file sinks
- **Storage:** XML-based session persistence
- **Protocol:** MCP JSON-RPC 2.0 (protocol version 2024-11-05)
- **Authentication:** API key-based (X-Api-Key header)

---

## Architecture

### Component Diagram

```
+------------------+     +------------------+     +------------------+
|   MCP Clients    |     |  SignalR Clients |     |   REST Clients   |
|  (AI agents)     |     |  (Browsers)      |     |  (Admin/API)     |
+--------+---------+     +--------+---------+     +--------+---------+
         |                        |                        |
         v                        v                        v
+----------------------------------------------------------+
|                   CollabMCP Server                       |
|                                                          |
|  +------------------+  +-----------------------------+   |
|  | ApiKeyAuth       |  |     McpEndpoint Middleware   |   |
|  | Middleware       |->|  (JSON-RPC / SSE handling)   |   |
|  +------------------+  +-----------------------------+   |
|                                                          |
|  +------------------+  +-----------------------------+   |
|  | McpSessionManager|  |     Mcp Services            |   |
|  | (SSE sessions)   |  |  McpTools / Resources/      |   |
|  +------------------+  |  McpPrompts                  |   |
|                         +-----------------------------+   |
|                                                          |
|  +------------------+  +-----------------------------+   |
|  | SessionManager   |  |     XmlSessionStore         |   |
|  | (in-memory +     |  |  (XML persistence layer)    |   |
|  |  operation log)  |  +-----------------------------+   |
|  +------------------+           |                        |
|                                 v                        |
|  +-------------------------------------------------+    |
|  |              CollabHub (SignalR)                 |    |
|  |  - Real-time primitive events                    |    |
|  |  - User join/leave notifications                 |    |
|  |  - Position updates with delta buffering         |    |
|  +-------------------------------------------------+    |
+----------------------------------------------------------+
                              |
                              v
                    +------------------+
                    |   XML Storage    |
                    |   (./sessions/)  |
                    +------------------+
```

### Core Components

| Component | File | Responsibility |
|-----------|------|----------------|
| `Program.cs` | `Program.cs` | Application entry point, dependency injection, middleware pipeline configuration |
| `ServerConfig` | `Config/ServerConfig.cs` | Configuration model (URL, port, API key, storage paths, throttle interval) |
| `CollabHub` | `Hubs/CollabHub.cs` | SignalR hub for real-time collaboration (primitives, user events, position updates) |
| `SessionManager` | `Services/SessionManager.cs` | In-memory session state management, primitive CRUD, locking, history |
| `XmlSessionStore` | `Services/XmlSessionStore.cs` | XML serialization/deserialization for session persistence |
| `McpEndpoint` | `Mcp/McpEndpoint.cs` | MCP protocol middleware (JSON-RPC requests, SSE stream management) |
| `McpSessionManager` | `Mcp/McpSession.cs` | MCP SSE session lifecycle (creation, timeout, cleanup) |
| `McpTools` | `Mcp/McpTools.cs` | MCP tool definitions and handlers (add/update/delete primitives, canvas state) |
| `McpResources` | `Mcp/McpResources.cs` | MCP resource definitions (canvas state, operation history URIs) |
| `McpPrompts` | `Mcp/McpPrompts.cs` | MCP prompt templates (AnalyzeCanvas, GenerateLayout) |
| `ApiKeyAuthMiddleware` | `Middleware/ApiKeyAuthMiddleware.cs` | API key validation for all endpoints except health check |
| `EntityNode` | `Models/Entities.cs` | Full entity data model (application Entity mirror + lock state, version) |
| `SessionState` | `Models/Entities.cs` | Complete session state (metadata, primitives, history, connected users) |
| `SessionMetadata` | `Models/Entities.cs` | Session metadata (background image, dimensions, timestamps) |
| `OperationLogEntry` | `Models/Entities.cs` | Audit log entry (operation type, user, timestamp, details) |

---

## Configuration

### appsettings.json

```json
{
  "Server": {
    "Url": "http://0.0.0.0",
    "Port": 5000,
    "AdminApiKey": "change-me-admin-key",
    "XmlStoragePath": "./sessions",
    "LogPath": "./Logs",
    "ThrottleIntervalMs": 33
  }
}
```

### Configuration Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Url` | string | `http://0.0.0.0` | Bind address |
| `Port` | int | `5000` | HTTP port |
| `AdminApiKey` | string | *(empty)* | API key for authentication via `X-Api-Key` header. Must be set before production use. |
| `XmlStoragePath` | string | `./sessions` | Directory for XML session files |
| `LogPath` | string | `./Logs` | Directory for Serilog daily log files |
| `ThrottleIntervalMs` | int | `33` | Position update throttle interval in milliseconds (~30 FPS) |

### Environment Variables

Configuration can be overridden via environment variables using the standard ASP.NET Core convention (e.g., `Server__Port`, `Server__AdminApiKey`).

---

## Endpoints

### REST API

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/health` | None | Health check. Returns `{ "status": "ok", "uptime": "<ISO 8601>" }` |
| `GET` | `/api/sessions` | API Key | List all active session IDs |
| `GET` | `/api/sessions/{sessionId}` | API Key | Get session metadata, primitive count, and connected users |
| `DELETE` | `/api/sessions/{sessionId}` | API Key | Remove and persist a session |

### MCP Protocol Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/mcp/sse` | API Key | Start SSE session. Returns `{ "session": "<id>", "endpoint": "/mcp/events?session=<id>" }` |
| `GET` | `/mcp/events?session=<id>` | API Key | SSE event stream with heartbeat (15s interval) |
| `POST` | `/mcp` | API Key | JSON-RPC 2.0 request endpoint |
| `GET` | `/mcp/resources` | API Key | List available resources |
| `GET` | `/mcp/prompts` | API Key | List available prompts |
| `GET` | `/mcp/tools` | API Key | List available tools |

### Authentication

All endpoints (except `/api/health`) require the `X-Api-Key` header with the value matching `Server.AdminApiKey`.

| Status | Condition |
|--------|-----------|
| `401` | Missing `X-Api-Key` header |
| `403` | Invalid `X-Api-Key` value |
| `200/201` | Valid API key |

---

## MCP Protocol

### Initialization

Clients must call `initialize` before other operations:

```json
{
  "jsonrpc": "2.0",
  "method": "initialize",
  "id": "1",
  "params": {}
}
```

Response:

```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "result": {
    "protocolVersion": "2024-11-05",
    "serverInfo": { "name": "CollabMCP", "version": "1.0.0" },
    "capabilities": {
      "resources": { "listChanged": true },
      "prompts": { "listChanged": true },
      "tools": { "listChanged": true }
    }
  }
}
```

### MCP Tools

#### 1. `add_primitive`

Add a new vector primitive to a canvas session.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sessionId` | string | Yes | Target session ID |
| `type` | string | Yes | EntityType name: `ViasInput`, `ViasOutput`, `ViasInout`, `ViasConnect`, `ViasFloating`, `ViasPower`, `ViasGround`, `WireInterconnect`, `WirePower`, `WireGround`, `CellNot`..`CellOther`, `UnitRegfile`/`UnitMemory`/`UnitCustom`, `Beacon`, `Region`, `Layer` |
| `points` | number[] | Yes | Flat coordinate pairs: `[x1, y1, x2, y2, ...]` |
| `parentId` | string | No | Parent entity id (e.g. a `Layer`) to attach under |
| `label` | string | No | Display label |
| `priority` | number | No | Z-order priority |
| `widthOverride` | number | No | Line/entity width override |
| `colorOverride` | string | No | Hex color code |
| `fontOverride` | string | No | Font override |
| `labelAlignment` | string | No | TextAlignment name |
| `traverseBlackList` | string[] | No | Prohibited entity types for traverse |
| `module` | string | No | Shared Verilog module name |
| `visible` | boolean | No | Visibility (default `true`) |
| `children` | object[] | No | Nested entities (same schema) |

**Response:** Serialized primitive with success message.

#### 2. `update_primitive`

Update an existing entity's properties.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sessionId` | string | Yes | Target session ID |
| `primitiveId` | string | Yes | Entity to update |
| `points` | number[] | No | New flat coordinate pairs `[x1, y1, ...]` |
| `type` | string | No | New EntityType name |
| `label` | string | No | New display label |
| `priority` | number | No | New z-order priority |
| `widthOverride` | number | No | New width override |
| `colorOverride` | string | No | New hex color |
| `fontOverride` | string | No | New font override |
| `labelAlignment` | string | No | New TextAlignment name |
| `module` | string | No | New Verilog module name |
| `visible` | boolean | No | New visibility |

**Response:** `{ "content": [{ "type": "text", "text": "{\"message\":\"Entity updated successfully\"}" }] }`

#### 3. `delete_primitive`

Remove a primitive from the canvas.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sessionId` | string | Yes | Target session ID |
| `primitiveId` | string | Yes | Primitive to delete |

#### 4. `clear_canvas`

Remove all primitives from a session.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sessionId` | string | Yes | Target session ID |

#### 5. `get_canvas_state`

Retrieve the full current state of a canvas.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sessionId` | string | Yes | Target session ID |

**Response:** Canvas state including metadata, all primitives with full properties, connected users list, and timestamp.

#### 6. `list_sessions`

List all active session IDs.

**Parameters:** None.

**Response:** `{ "content": [{ "type": "text", "text": "{\"sessions\":[...],\"count\":N}" }] }`

### MCP Resources

Resources are addressed via `mcp://` URIs:

| URI Pattern | Description | MIME Type |
|-------------|-------------|-----------|
| `mcp://sessions/{sessionId}/canvas` | Current canvas state with all primitives and metadata | `application/json` |
| `mcp://sessions/{sessionId}/history` | Operation history log (last 200 entries) | `application/json` |

### MCP Prompts

#### 1. `AnalyzeCanvas`

Provides a structured analysis of the current canvas state.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sessionId` | string | Yes | Target session ID |

**Output:** Primitive count, breakdown by type, connected users, locked primitives list, background image info, image dimensions.

#### 2. `GenerateLayout`

Provides instructions for generating vector primitives from a text description.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sessionId` | string | Yes | Target session ID |
| `description` | string | Yes | Textual description of the desired layout |

**Output:** Suggested primitive types and step-by-step instructions for using `add_primitive` tool.

---

## SignalR Hub (CollabHub)

### Client-to-Server Methods

| Method | Parameters | Description |
|--------|------------|-------------|
| `JoinSession` | `sessionId`, `userId` | Join a canvas session; receives full snapshot |
| `SendEntityCreated` | `sessionId`, `entity` (EntityDto), `userId`, `parentId` | Create a new entity (full application entity model) |
| `SendEntityUpdated` | `sessionId`, `entity` (EntityDto), `userId` | Update an existing entity |
| `SendEntityDeleted` | `sessionId`, `entityId` | Delete an entity (broadcasts `OnEntityDeleted`) |
| `SendPositionUpdate` | `sessionId`, `entityId`, `points`, `userId` | Real-time position update (with delta buffering) |
| `LockEntity` | `sessionId`, `entityId` | Lock an entity for exclusive editing |
| `UnlockEntity` | `sessionId`, `entityId` | Unlock a previously locked entity |
| `GetSessionState` | `sessionId` | Retrieve current session state |
| `GetHistory` | `sessionId`, `count` (default 50) | Retrieve operation history |
| `GetConnectedUsers` | `sessionId` | List connected users |

### Server-to-Client Events

| Event | Payload | Description |
|-------|---------|-------------|
| `OnUserJoined` | `{ UserId, Snapshot }` | User joined with full canvas snapshot; also broadcast to group |
| `OnUserLeft` | `{ UserId }` | User disconnected; broadcast to group |
| `OnEntityCreated` | EntityDto | New entity created; broadcast to session group |
| `OnEntityUpdated` | EntityDto | Entity updated; broadcast to session group |
| `OnEntityLocked` | EntityDto | Entity locked; broadcast to session group |
| `OnEntityUnlocked` | EntityDto | Entity unlocked; broadcast to session group |
| `OnPositionUpdated` | `{ PrimitiveId, Points }` | Real-time position delta; broadcast to session group |
| `OnEntityError` | `{ error }` | Error response for entity operations |
| `OnLockError` | `{ EntityId, Error }` | Error response for lock operations |
| `OnSessionError` | `{ error }` | Session-level error |

### EntityDto (wire format)

```json
{
  "id": "guid",
  "type": "ViasInput",
  "label": "IN1",
  "lambdaX": 100, "lambdaY": 100, "lambdaEndX": 300, "lambdaEndY": 400,
  "lambdaWidth": 0, "lambdaHeight": 0,
  "priority": 0, "widthOverride": 2,
  "colorOverride": "#FF0000",
  "fontOverride": null,
  "labelAlignment": "GlobalSettings",
  "points": [100, 100, 300, 400],
  "traverseBlackList": [],
  "module": null,
  "visible": true,
  "children": [ { "…": "nested EntityDto" } ],
  "createdBy": "user-id",
  "lockedBy": "user-id or \"none\"",
  "lockedAt": "ISO 8601 or \"\"",
  "version": 1,
  "createdAt": "ISO 8601",
  "updatedAt": "ISO 8601"
}
```

---

## Data Model

The server stores the **full application entity model** (mirror of `EntityBox.Entity`), enriched
with collaboration fields.

### EntityNode

| Property | Type | Description |
|----------|------|-------------|
| `Id` | string | Unique identifier (GUID) |
| `Type` | string | EntityType enum name (`ViasInput`..`ViasGround`, `WireInterconnect`/`WirePower`/`WireGround`, `CellNot`..`CellOther`, `UnitRegfile`/`UnitMemory`/`UnitCustom`, `Beacon`, `Region`, `Layer`, `Root`) |
| `Label` | string\? | Display label |
| `LambdaX/Y/EndX/EndY`, `LambdaWidth/Height` | float | Geometry in lambda units |
| `Priority` | int | Z-order priority |
| `WidthOverride` | int | Line/entity width override |
| `ColorOverride` | string\? | Hex color `#RRGGBB` |
| `FontOverride` | string\? | FontXmlConverter string |
| `LabelAlignment` | string | TextAlignment enum name |
| `PathPoints` | List\<EntityPoint\>\? | Ordered list of 2D coordinates |
| `TraverseBlackList` | List\<string\>\? | Prohibited entity types for traverse |
| `Module` | string\? | Shared Verilog module name |
| `Visible` | bool | Visibility |
| `Children` | List\<EntityNode\> | Nested entities (layers) |
| `CreatedBy` | string | User ID who created the entity |
| `LockedBy` | string\? | User ID of current lock holder (null = unlocked) |
| `LockedAt` | DateTime\? | Timestamp of lock acquisition |
| `Version` | int | Optimistic concurrency version counter |
| `CreatedAt` | DateTime | Creation timestamp |
| `UpdatedAt` | DateTime | Last modification timestamp |

### SessionState

| Property | Type | Description |
|----------|------|-------------|
| `Metadata` | SessionMetadata | Session-level metadata |
| `Entities` | List\<EntityNode\> | Top-level entities (list order = z-order) |
| `EntityIndex` | ConcurrentDictionary\<string, EntityNode\> | O(1) lookup by entity id across the tree |
| `History` | List\<OperationLogEntry\> | Operation audit log (max 1000 entries, trimmed to 500 on overflow) |
| `ConnectedUsers` | HashSet\<string\> | Currently connected user IDs |
| `Version` | int | Session-level version counter |

### SessionMetadata

| Property | Type | Description |
|----------|------|-------------|
| `SessionId` | string | Unique session identifier |
| `BackgroundImageId` | string\? | Background image reference |
| `BackgroundImageUrl` | string\? | Background image URL |
| `ImageWidth` | int\? | Canvas width in pixels |
| `ImageHeight` | int\? | Canvas height in pixels |
| `CreatedAt` | DateTime | Session creation time |
| `LastActivity` | DateTime | Last activity timestamp |
| `Version` | int | Session version |

### OperationLogEntry

| Property | Type | Description |
|----------|------|-------------|
| `Operation` | string | Operation type: `Created`, `Updated`, `Deleted`, `Locked`, `Unlocked`, `Cleared`, `UserJoined`, `UserLeft` |
| `PrimitiveId` | string | Affected primitive ID (empty for non-primitive operations) |
| `UserId` | string | User who performed the operation |
| `Timestamp` | DateTime | Operation timestamp |
| `Details` | string\? | Additional context (e.g., version number, cleared count) |

---

## Session Persistence

### XML Storage Format

Sessions are persisted as XML files in the configured `XmlStoragePath` directory (default: `./sessions/`).

**File naming:** `{sessionId}.xml`

**Structure (format v2):**

```xml
<Session FormatVersion="2">
  <Metadata>
    <SessionId>sess-id</SessionId>
    <BackgroundImageId />
    <BackgroundImageUrl />
    <ImageWidth>0</ImageWidth>
    <ImageHeight>0</ImageHeight>
    <CreatedAt>2026-07-28T00:00:00.000Z</CreatedAt>
    <LastActivity>2026-07-28T00:00:00.000Z</LastActivity>
    <Version>1</Version>
  </Metadata>
  <Entities>
    <EntityNode>
      <Id>guid</Id>
      <Type>ViasInput</Type>
      <Label>IN1</Label>
      <LambdaX>100</LambdaX><LambdaY>100</LambdaY>
      <LambdaEndX>300</LambdaEndX><LambdaEndY>400</LambdaEndY>
      <LambdaWidth>0</LambdaWidth><LambdaHeight>0</LambdaHeight>
      <Priority>0</Priority>
      <WidthOverride>2</WidthOverride>
      <ColorOverride>#FF0000</ColorOverride>
      <FontOverride />
      <LabelAlignment>GlobalSettings</LabelAlignment>
      <PathPoints>
        <Point><X>100</X><Y>100</Y></Point>
        <Point><X>300</X><Y>400</Y></Point>
      </PathPoints>
      <TraverseBlackList />
      <Module />
      <Visible>true</Visible>
      <CreatedBy>user-id</CreatedBy>
      <LockedBy>user-id</LockedBy>
      <LockedAt>2026-07-28T00:00:00.000Z</LockedAt>
      <Version>1</Version>
      <CreatedAt>2026-07-28T00:00:00.000Z</CreatedAt>
      <UpdatedAt>2026-07-28T00:00:00.000Z</UpdatedAt>
      <Children>
        <EntityNode>…nested…</EntityNode>
      </Children>
    </EntityNode>
  </Entities>
  <History>
    <Entry>
      <Operation>Created</Operation>
      <PrimitiveId>guid</PrimitiveId>
      <UserId>user-id</UserId>
      <Timestamp>2026-07-28T00:00:00.000Z</Timestamp>
      <Details />
    </Entry>
  </History>
  <ConnectedUsers>
    <User>user-id</User>
  </ConnectedUsers>
</Session>
```

### Persistence Behavior

- Sessions are loaded from XML into memory on first access (lazy loading)
- Every state mutation (create/update/delete/lock/unlock/clear/user-join/user-leave) triggers immediate XML save
- Real-time position updates are applied in memory only (no version bump, no XML write); the next real mutation persists them
- Sessions are removed from memory and their XML file is deleted on explicit delete request
- New sessions are created in memory if no XML file exists

---

## MCP SSE Session Management

### Lifecycle

1. **Creation:** Client POSTs to `/mcp/sse` with `X-Api-Key` and `X-Mcp-Writer-Id` headers
2. **Response:** Server returns session ID and SSE endpoint URL (HTTP 201)
3. **Streaming:** Client connects to `/mcp/events?session=<id>` for SSE stream
4. **Heartbeat:** Server sends heartbeat every 15 seconds to keep connection alive
5. **Touch:** Each heartbeat updates the session's last access time
6. **Expiration:** Sessions expire after 30 minutes of inactivity
7. **Cleanup:** Expired sessions are cleaned up on each incoming request

### McpSession Model

| Property | Type | Description |
|----------|------|-------------|
| `SessionId` | string | GUID (short format) |
| `WriterId` | string | Client identifier |
| `CreatedAt` | DateTime | Creation timestamp |
| `LastAccess` | DateTime | Last heartbeat timestamp |

---

## Locking Mechanism

### Primitive Locking

- **Lock acquisition:** Only the current lock holder or the target user can lock a primitive
- **Lock conflict:** If a primitive is locked by user A, user B receives error: `"Primitive locked by user {A}"`
- **Automatic unlock:** When a user disconnects, all their locked primitives are automatically unlocked
- **Version increment:** Both lock and unlock operations increment the primitive's version counter

### Optimistic Concurrency

- Every mutation increments the primitive's `Version` counter
- Every mutation increments the session's `Version` counter
- This enables clients to detect stale data and prevent conflicting edits

---

## Logging

### Serilog Configuration

| Sink | Configuration |
|------|---------------|
| Console | All events at `Information` level and above |
| File | Daily rolling files: `collabmcp-YYYYMMDD.log`, retained for 30 days |

### Log Template

```
{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}
```

### Log Levels

| Component | Default Level |
|-----------|---------------|
| Application | `Information` |
| Microsoft.AspNetCore | `Warning` |
| Microsoft.Hosting.Lifetime | `Information` |
| Debug operations | `Debug` (primitive creation, locking) |

---

## Security

### API Key Authentication

- All endpoints require `X-Api-Key` header except `/api/health`
- Invalid or missing key returns `401` (missing) or `403` (invalid)
- The `AdminApiKey` must be configured before production deployment (default is placeholder `"change-me-admin-key"`)

### Network Exposure

- Default bind address is `0.0.0.0` (all interfaces)
- Default port is `5000`
- Should be placed behind a reverse proxy with TLS in production

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Newtonsoft.Json | 13.0.4 | JSON serialization (declared; runtime uses System.Text.Json) |
| Serilog | 4.2.0 | Structured logging |
| Serilog.Sinks.File | 6.0.0 | File-based log sink |
| Serilog.Sinks.Console | 6.0.0 | Console log sink |
| Microsoft.AspNetCore.OpenApi | 10.0.10 | OpenAPI/Swagger support |

> Note: SignalR is provided by the ASP.NET Core 10 shared framework; no separate package reference is required.

---

## Deployment Notes

1. **Set AdminApiKey:** Replace `"change-me-admin-key"` with a strong secret
2. **Storage path:** Ensure the `XmlStoragePath` directory is writable
3. **Firewall:** Open the configured port (default 5000)
4. **Reverse proxy:** Configure nginx/IIS/Apache for TLS termination
5. **Multiple instances:** This server uses in-memory state; for horizontal scaling, consider a distributed session store
6. **XML storage:** Sessions are stored as individual `.xml` files per session ID
