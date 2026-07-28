# Спецификация CollabMCP Server

## Обзор

**CollabMCP Server** — сервер реального времени для совместной работы с холстом, реализованный на .NET 10.0, предоставляющий интерфейс Model Context Protocol (MCP) для многопользовательской совместной работы с векторной графикой. Позволяет нескольким пользователям и ИИ-агентам одновременно создавать, редактировать, блокировать и управлять векторными примитивами на общих холстах с синхронизацией в реальном времени через SignalR и сохранением состояния в XML-файлы.

**Технологический стек:**
- **Фреймворк:** ASP.NET Core 10.0 (.NET 10.0)
- **Коммуникация в реальном времени:** Microsoft.AspNetCore.SignalR
- **Логирование:** Serilog с консольным и ежедневным файловым выводом
- **Хранение:** XML-персистентность сессий
- **Протокол:** MCP JSON-RPC 2.0 (версия протокола 2024-11-05)
- **Аутентификация:** По API-ключу (заголовок `X-Api-Key`)

---

## Архитектура

### Диаграмма компонентов

```
+------------------+     +------------------+     +------------------+
|   MCP Клиенты    |     |  SignalR Клиенты |     |   REST Клиенты   |
|  (ИИ-агенты)     |     |  (Браузеры)      |     |  (Админ/API)     |
+--------+---------+     +--------+---------+     +--------+---------+
         |                        |                        |
         v                        v                        v
+----------------------------------------------------------+
|                   CollabMCP Server                       |
|                                                          |
|  +------------------+  +-----------------------------+   |
|  | ApiKeyAuth       |  |     McpEndpoint Middleware   |   |
|  | Middleware       |->|  (Обработка JSON-RPC / SSE)  |   |
|  +------------------+  +-----------------------------+   |
|                                                          |
|  +------------------+  +-----------------------------+   |
|  | McpSessionManager|  |     MCP Сервисы             |   |
|  | (SMP сессии)     |  |  McpTools / Resources/      |   |
|  +------------------+  |  McpPrompts                  |   |
|                         +-----------------------------+   |
|                                                          |
|  +------------------+  +-----------------------------+   |
|  | SessionManager   |  |     XmlSessionStore         |   |
|  | (in-memory +     |  |  (XML-слой персистентности)  |   |
|  |  журнал операций)|  +-----------------------------+   |
|  +------------------+           |                        |
|                                 v                        |
|  +-------------------------------------------------+    |
|  |              CollabHub (SignalR)                 |    |
|  |  - События примитивов в реальном времени         |    |
|  |  - Уведомления о присоединении/выходе            |    |
|  |  - Позиционные обновления с буферизацией дельт   |    |
|  +-------------------------------------------------+    |
+----------------------------------------------------------+
                              |
                              v
                    +------------------+
                    |   XML Хранилище  |
                    |   (./sessions/)  |
                    +------------------+
```

### Основные компоненты

| Компонент | Файл | Назначение |
|-----------|------|------------|
| `Program.cs` | `Program.cs` | Точка входа приложения, DI, конфигурация конвейера middleware |
| `ServerConfig` | `Config/ServerConfig.cs` | Модель конфигурации (URL, порт, API-ключ, пути хранения, интервал throttle) |
| `CollabHub` | `Hubs/CollabHub.cs` | SignalR хаб для совместной работы в реальном времени |
| `SessionManager` | `Services/SessionManager.cs` | Управление состоянием сессий in-memory, CRUD примитивов, блокировки, история |
| `XmlSessionStore` | `Services/XmlSessionStore.cs` | XML-сериализация/десериализация персистентности сессий |
| `McpEndpoint` | `Mcp/McpEndpoint.cs` | Middleware MCP-протокола (JSON-RPC запросы, SSE потоки) |
| `McpSessionManager` | `Mcp/McpSession.cs` | Жизненный цикл MCP SSE сессий (создание, таймаут, очистка) |
| `McpTools` | `Mcp/McpTools.cs` | Определения и обработчики MCP-инструментов (add/update/delete примитивы, состояние холста) |
| `McpResources` | `Mcp/McpResources.cs` | Определения MCP-ресурсов (URI состояния холста, журнала операций) |
| `McpPrompts` | `Mcp/McpPrompts.cs` | Шаблоны MCP-подсказок (AnalyzeCanvas, GenerateLayout) |
| `ApiKeyAuthMiddleware` | `Middleware/ApiKeyAuthMiddleware.cs` | Валидация API-ключа для всех endpoints кроме health check |
| `VectorPrimitive` | `Models/Entities.cs` | Модель данных векторного примитива |
| `SessionState` | `Models/Entities.cs` | Полное состояние сессии |
| `SessionMetadata` | `Models/Entities.cs` | Метаданные сессии |
| `OperationLogEntry` | `Models/Entities.cs` | Запись журнала аудита |

---

## Конфигурация

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

### Параметры конфигурации

| Параметр | Тип | По умолчанию | Описание |
|----------|------|--------------|----------|
| `Url` | string | `http://0.0.0.0` | Адрес привязки |
| `Port` | int | `5000` | HTTP порт |
| `AdminApiKey` | string | *(пусто)* | API-ключ для аутентификации через заголовок `X-Api-Key`. Должен быть установлен перед использованием в production. |
| `XmlStoragePath` | string | `./sessions` | Каталог для XML-файлов сессий |
| `LogPath` | string | `./Logs` | Каталог для файлов логов Serilog |
| `ThrottleIntervalMs` | int | `33` | Интервал throttle для обновлений позиции (мс, ~30 FPS) |

### Переменные окружения

Конфигурация может быть переопределена через переменные окружения по стандартному соглашению ASP.NET Core (например, `Server__Port`, `Server__AdminApiKey`).

---

## Эндпоинты

### REST API

| Метод | Путь | Auth | Описание |
|-------|------|------|----------|
| `GET` | `/api/health` | Нет | Проверка работоспособности. Возвращает `{ "status": "ok", "uptime": "<ISO 8601>" }` |
| `GET` | `/api/sessions` | API-ключ | Список всех активных ID сессий |
| `GET` | `/api/sessions/{sessionId}` | API-ключ | Метаданные сессии, количество примитивов, подключённые пользователи |
| `DELETE` | `/api/sessions/{sessionId}` | API-ключ | Удаление и сохранение сессии |

### MCP Протокол

| Метод | Путь | Auth | Описание |
|-------|------|------|----------|
| `POST` | `/mcp/sse` | API-ключ | Запуск SSE сессии. Возвращает `{ "session": "<id>", "endpoint": "/mcp/events?session=<id>" }` |
| `GET` | `/mcp/events?session=<id>` | API-ключ | SSE поток событий с heartbeat (каждые 15 сек) |
| `POST` | `/mcp` | API-ключ | JSON-RPC 2.0 endpoint для запросов |
| `GET` | `/mcp/resources` | API-ключ | Список доступных ресурсов |
| `GET` | `/mcp/prompts` | API-ключ | Список доступных подсказок |
| `GET` | `/mcp/tools` | API-ключ | Список доступных инструментов |

### Аутентификация

Все endpoints (кроме `/api/health`) требуют заголовок `X-Api-Key` со значением, совпадающим с `Server.AdminApiKey`.

| Статус | Условие |
|--------|---------|
| `401` | Отсутствует заголовок `X-Api-Key` |
| `403` | Неверное значение `X-Api-Key` |
| `200/201` | Валидный API-ключ |

---

## MCP Протокол

### Инициализация

Клиенты должны вызвать `initialize` перед другими операциями:

```json
{
  "jsonrpc": "2.0",
  "method": "initialize",
  "id": "1",
  "params": {}
}
```

Ответ:

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

### MCP Инструменты

#### 1. `add_primitive`

Добавить новый векторный примитив в сессию холста.

**Параметры:**

| Имя | Тип | Обяз. | Описание |
|-----|------|-------|----------|
| `sessionId` | string | Да | ID целевой сессии |
| `type` | string | Да | Тип примитива: `rectangle`, `polygon`, `line`, `ellipse`, `polyline` |
| `points` | number[] | Да | Координаты: `[x1, y1, x2, y2, ...]` |
| `strokeColor` | string | Нет | HEX-цвет (по умолчанию: `#000000`) |
| `strokeWidth` | number | Нет | Толщина линии (по умолчанию: `1`) |
| `fillColor` | string | Нет | Цвет заливки (по умолчанию: `transparent`) |

**Ответ:** Сериализованный примитив с сообщением об успехе.

#### 2. `update_primitive`

Обновить свойства существующего примитива.

**Параметры:**

| Имя | Тип | Обяз. | Описание |
|-----|------|-------|----------|
| `sessionId` | string | Да | ID целевой сессии |
| `primitiveId` | string | Да | ID обновляемого примитива |
| `points` | number[] | Нет | Новые координаты |
| `type` | string | Нет | Новый тип |
| `strokeColor` | string | Нет | Новый цвет обводки |
| `strokeWidth` | number | Нет | Новая толщина |
| `fillColor` | string | Нет | Новый цвет заливки |

#### 3. `delete_primitive`

Удалить примитив с холста.

**Параметры:**

| Имя | Тип | Обяз. | Описание |
|-----|------|-------|----------|
| `sessionId` | string | Да | ID целевой сессии |
| `primitiveId` | string | Да | ID удаляемого примитива |

#### 4. `clear_canvas`

Удалить все примитивы из сессии.

**Параметры:**

| Имя | Тип | Обяз. | Описание |
|-----|------|-------|----------|
| `sessionId` | string | Да | ID целевой сессии |

#### 5. `get_canvas_state`

Получить полное текущее состояние холста.

**Параметры:**

| Имя | Тип | Обяз. | Описание |
|-----|------|-------|----------|
| `sessionId` | string | Да | ID целевой сессии |

**Ответ:** Состояние холста включая метаданные, все примитивы с полными свойствами, список подключённых пользователей, метка времени.

#### 6. `list_sessions`

Список всех активных ID сессий.

**Параметры:** Нет.

**Ответ:** `{ "content": [{ "type": "text", "text": "{\"sessions\":[...],\"count\":N}" }] }`

### MCP Ресурсы

Ресурсы доступны по `mcp://` URI:

| URI Шаблон | Описание | MIME Type |
|-------------|----------|-----------|
| `mcp://sessions/{sessionId}/canvas` | Текущее состояние холста со всеми примитивами | `application/json` |
| `mcp://sessions/{sessionId}/history` | Журнал операций (последние 200 записей) | `application/json` |

### MCP Подсказки

#### 1. `AnalyzeCanvas`

Структурированный анализ текущего состояния холста.

**Параметры:**

| Имя | Тип | Обяз. | Описание |
|-----|------|-------|----------|
| `sessionId` | string | Да | ID целевой сессии |

**Результат:** Количество примитивов, разбивка по типам, подключённые пользователи, заблокированные примитивы, фоновое изображение, размеры холста.

#### 2. `GenerateLayout`

Инструкции для генерации векторных примитивов по текстовому описанию.

**Параметры:**

| Имя | Тип | Обяз. | Описание |
|-----|------|-------|----------|
| `sessionId` | string | Да | ID целевой сессии |
| `description` | string | Да | Текстовое описание желаемой компоновки |

**Результат:** Рекомендуемые типы примитивов и пошаговые инструкции для использования инструмента `add_primitive`.

---

## SignalR Hub (CollabHub)

### Методы клиент → сервер

| Метод | Параметры | Описание |
|-------|-----------|----------|
| `JoinSession` | `sessionId`, `userId` | Присоединение к сессии; получение полного снимка холста |
| `SendPrimitiveCreated` | `sessionId`, `primitiveId`, `type`, `points`, `strokeColor`, `strokeWidth`, `fillColor`, `userId` | Создание нового примитива |
| `SendPrimitiveUpdated` | `sessionId`, `primitiveId`, `type`, `points`, `strokeColor`, `strokeWidth`, `fillColor`, `userId` | Обновление существующего примитива |
| `SendPositionUpdate` | `sessionId`, `primitiveId`, `points`, `userId` | Обновление позиции в реальном времени (с буферизацией дельт) |
| `LockPrimitive` | `sessionId`, `primitiveId` | Блокировка примитива для эксклюзивного редактирования |
| `UnlockPrimitive` | `sessionId`, `primitiveId` | Разблокировка заблокированного примитива |
| `GetSessionState` | `sessionId` | Получение текущего состояния сессии |
| `GetHistory` | `sessionId`, `count` (по умолч. 50) | Получение журнала операций |
| `GetConnectedUsers` | `sessionId` | Список подключённых пользователей |

### События сервер → клиент

| Событие | Полезная нагрузка | Описание |
|---------|-------------------|----------|
| `OnUserJoined` | `{ UserId, Snapshot }` | Пользователь присоединился со снимком холста; трансляция группе |
| `OnUserLeft` | `{ UserId }` | Пользователь отключился; трансляция группе |
| `OnPrimitiveCreated` | Сериализованный примитив | Новый примитив создан; трансляция группе сессии |
| `OnPrimitiveUpdated` | Сериализованный примитив | Примитив обновлён; трансляция группе сессии |
| `OnPrimitiveLocked` | Сериализованный примитив | Примитив заблокирован; трансляция группе сессии |
| `OnPrimitiveUnlocked` | Сериализованный примитив | Примитив разблокирован; трансляция группе сессии |
| `OnPositionUpdated` | `{ PrimitiveId, Points }` | Дельта позиции в реальном времени; трансляция группе сессии |
| `OnPrimitiveError` | `{ error }` | Ответ об ошибке операции с примитивом |
| `OnLockError` | `{ PrimitiveId, Error }` | Ответ об ошибке блокировки |
| `OnSessionError` | `{ error }` | Ошибка уровня сессии |

### Сериализованный объект примитива

```json
{
  "id": "guid",
  "type": "rectangle",
  "points": [{ "x": 100, "y": 100 }, { "x": 300, "y": 400 }],
  "strokeColor": "#FF0000",
  "strokeWidth": 2,
  "fillColor": "transparent",
  "createdBy": "user-id",
  "lockedBy": "user-id или \"none\"",
  "lockedAt": "ISO 8601 или \"\"",
  "version": 1,
  "createdAt": "ISO 8601",
  "updatedAt": "ISO 8601"
}
```

---

## Модель данных

### VectorPrimitive

| Свойство | Тип | Описание |
|----------|------|----------|
| `Id` | string | Уникальный идентификатор (GUID) |
| `Type` | string | Тип примитива (`rectangle`, `polygon`, `line`, `ellipse`, `polyline`) |
| `Points` | List\<Point\> | Упорядоченный список 2D-координат |
| `StrokeColor` | string | HEX-цвет обводки (по умолчанию `#000000`) |
| `StrokeWidth` | double | Толщина линии в пикселях (по умолчанию `1.0`) |
| `FillColor` | string | Цвет заливки (по умолчанию `transparent`) |
| `CreatedBy` | string | ID пользователя, создавшего примитив |
| `LockedBy` | string\? | ID текущего владельца блокировки (null = разблокирован) |
| `LockedAt` | DateTime\? | Метка времени блокировки |
| `Version` | int | Счётчик оптимистичной блокировки |
| `CreatedAt` | DateTime | Время создания |
| `UpdatedAt` | DateTime | Время последнего изменения |

### SessionState

| Свойство | Тип | Описание |
|----------|------|----------|
| `Metadata` | SessionMetadata | Метаданные сессии |
| `Primitives` | ConcurrentDictionary\<string, VectorPrimitive\> | Все примитивы сессии |
| `History` | List\<OperationLogEntry\> | Журнал операций (макс. 1000 записей, обрезка до 500 при переполнении) |
| `ConnectedUsers` | HashSet\<string\> | Подключённые пользовательские ID |
| `Version` | int | Счётчик версии сессии |

### SessionMetadata

| Свойство | Тип | Описание |
|----------|------|----------|
| `SessionId` | string | Уникальный ID сессии |
| `BackgroundImageId` | string\? | Ссылка на фоновое изображение |
| `BackgroundImageUrl` | string\? | URL фонового изображения |
| `ImageWidth` | int\? | Ширина холста в пикселях |
| `ImageHeight` | int\? | Высота холста в пикселях |
| `CreatedAt` | DateTime | Время создания сессии |
| `LastActivity` | DateTime | Время последней активности |
| `Version` | int | Версия сессии |

### OperationLogEntry

| Свойство | Тип | Описание |
|----------|------|----------|
| `Operation` | string | Тип операции: `Created`, `Updated`, `Deleted`, `Locked`, `Unlocked`, `Cleared`, `UserJoined`, `UserLeft` |
| `PrimitiveId` | string | ID затронутого примитива (пусто для не-примитивных операций) |
| `UserId` | string | ID пользователя, выполнившего операцию |
| `Timestamp` | DateTime | Метка времени операции |
| `Details` | string\? | Дополнительный контекст (версия, количество очищенных примитивов и т.д.) |

---

## Персистентность сессий

### Формат XML-хранилища

Сессии сохраняются как XML-файлы в каталоге `XmlStoragePath` (по умолчанию: `./sessions/`).

**Именование файлов:** `{sessionId}.xml`

**Структура:**

```xml
<Session>
  <Metadata>
    <BackgroundImageId />
    <BackgroundImageUrl />
    <ImageWidth>0</ImageWidth>
    <ImageHeight>0</ImageHeight>
    <CreatedAt>2026-07-28T00:00:00.000Z</CreatedAt>
    <LastActivity>2026-07-28T00:00:00.000Z</LastActivity>
  </Metadata>
  <Primitives>
    <Primitive>
      <Id>guid</Id>
      <Type>rectangle</Type>
      <Points>
        <Point><X>100</X><Y>100</Y></Point>
        <Point><X>300</X><Y>400</Y></Point>
      </Points>
      <StrokeColor>#FF0000</StrokeColor>
      <StrokeWidth>2</StrokeWidth>
      <FillColor>transparent</FillColor>
      <CreatedBy>user-id</CreatedBy>
      <LockedBy>user-id</LockedBy>
      <LockedAt>2026-07-28T00:00:00.000Z</LockedAt>
      <Version>1</Version>
      <CreatedAt>2026-07-28T00:00:00.000Z</CreatedAt>
      <UpdatedAt>2026-07-28T00:00:00.000Z</UpdatedAt>
    </Primitive>
  </Primitives>
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

### Поведение персистентности

- Сессии загружаются из XML в память при первом обращении (ленивая загрузка)
- Каждая мутация состояния (создание/обновление/удаление/блокировка/разблокировка/очистка/присоединение/выход пользователя) вызывает немедленное сохранение в XML
- Сессии удаляются из памяти и сохраняются в XML при явном запросе удаления
- Новые сессии создаются в памяти, если XML-файл не существует

---

## Управление MCP SSE сессиями

### Жизненный цикл

1. **Создание:** Клиент отправляет POST на `/mcp/sse` с заголовками `X-Api-Key` и `X-Mcp-Writer-Id`
2. **Ответ:** Сервер возвращает ID сессии и URL SSE endpoint (HTTP 201)
3. **Потоковая передача:** Клиент подключается к `/mcp/events?session=<id>` для SSE-потока
4. **Heartbeat:** Сервер отправляет heartbeat каждые 15 секунд для поддержания соединения
5. **Touch:** Каждый heartbeat обновляет время последнего доступа сессии
6. **Истечение:** Сессии истекают через 30 минут бездействия
7. **Очистка:** Истекшие сессии очищаются при каждом входящем запросе

### Модель McpSession

| Свойство | Тип | Описание |
|----------|------|----------|
| `SessionId` | string | GUID (короткий формат) |
| `WriterId` | string | Идентификатор клиента |
| `CreatedAt` | DateTime | Метка времени создания |
| `LastAccess` | DateTime | Метка времени последнего heartbeat |

---

## Механизм блокировки

### Блокировка примитивов

- **Приобретение блокировки:** Только текущий владелец блокировки или целевой пользователь может заблокировать примитив
- **Конфликт блокировки:** Если примитив заблокирован пользователем A, пользователь B получает ошибку: `"Primitive locked by user {A}"`
- **Автоматическая разблокировка:** При отключении пользователя все его заблокированные примитивы автоматически разблокируются
- **Инкремент версии:** Операции блокировки и разблокировки увеличивают счётчик версии примитива

### Оптимистичная блокировка

- Каждая мутация увеличивает `Version` примитива
- Каждая мутация увеличивает `Version` сессии
- Это позволяет клиентам обнаруживать устаревшие данные и предотвращать конфликтующие правки

---

## Логирование

### Конфигурация Serilog

| Sink | Конфигурация |
|------|--------------|
| Console | Все события уровня `Information` и выше |
| File | Ежедневные ротации: `collabmcp-YYYYMMDD.log`, хранение 30 дней |

### Шаблон логов

```
{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}
```

### Уровни логирования

| Компонент | Уровень по умолчанию |
|-----------|----------------------|
| Приложение | `Information` |
| Microsoft.AspNetCore | `Warning` |
| Microsoft.Hosting.Lifetime | `Information` |
| Отладочные операции | `Debug` (создание примитивов, блокировки) |

---

## Безопасность

### Аутентификация по API-ключу

- Все endpoints требуют заголовок `X-Api-Key`, кроме `/api/health`
- Неверный или отсутствующий ключ возвращает `401` (отсутствует) или `403` (неверный)
- `AdminApiKey` должен быть настроен перед развёртыванием в production (по умолчанию — заглушка `"change-me-admin-key"`)

### Сетевое развёртывание

- Адрес по умолчанию: `0.0.0.0` (все интерфейсы)
- Порт по умолчанию: `5000`
- В production рекомендуется размещение за reverse-proxy с TLS

---

## Зависимости

| Пакет | Версия | Назначение |
|-------|--------|------------|
| Microsoft.AspNetCore.SignalR | 1.2.0 | WebSocket-коммуникация в реальном времени |
| Newtonsoft.Json | 13.0.4 | JSON-сериализация |
| Serilog | 4.2.0 | Структурированное логирование |
| Serilog.Sinks.File | 6.0.0 | Файловый sink для логов |
| Serilog.Sinks.Console | 6.0.0 | Консольный sink для логов |
| Microsoft.AspNetCore.OpenApi | 10.0.10 | Поддержка OpenAPI/Swagger |

---

## Примечания по развёртыванию

1. **Установите AdminApiKey:** Замените `"change-me-admin-key"` на надёжный секрет
2. **Путь хранения:** Убедитесь, что каталог `XmlStoragePath` доступен для записи
3. **Фаервол:** Откройте конфигурируемый порт (по умолчанию 5000)
4. **Reverse-proxy:** Настройте nginx/IIS/Apache для TLS-терминации
5. **Масштабирование:** Данный сервер использует in-memory состояние; для горизонтального масштабирования рассмотрите распределённое хранилище сессий
6. **XML-хранилище:** Сессии сохраняются как отдельные `.xml`-файлы на каждый ID сессии
