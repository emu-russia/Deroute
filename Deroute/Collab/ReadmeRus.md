# Спецификация Collab клиента (Deroute/Collab)

## Обзор

**Collab** — это модуль реального времени для совместной работы с векторной графикой в приложении DerouteSharp. Он обеспечивает синхронизацию canvas между несколькими пользователями через WebSocket-соединение с сервером CollabMCP.Server. Модуль позволяет нескольким пользователям одновременно создавать, редактировать, перемещать и блокировать векторные примитивы на общем canvas с мгновенной синхронизацией.

**Технологический стек:**
- **Язык:** C# (.NET Framework / .NET Core)
- **Протокол:** WebSocket (SignalR Protocol)
- **Формат данных:** JSON (MiniJson парсер)
- **UI:** Windows Forms
- **Конвертация:** Entity ↔ VectorPrimitiveData

---

## Архитектура

### Компонентная диаграмма

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

### Поток данных

```
Пользователь A                    Сервер                    Пользователь B
     |                              |                              |
     |-- SendPrimitiveCreated --->|                              |
     |                              |-- OnPrimitiveCreated --->  |
     |                              |                              |-- Apply на canvas
     |                              |                              |
     |-- LockPrimitive ---------> |                              |
     |                              |-- OnPrimitiveLocked ---->  |
     |                              |                              |-- Показать overlay
     |                              |                              |
     |                              |<--- Position Update --------|
     |<-- OnPositionUpdated ------ |                              |
     |-- Apply update ----------->|                              |
```

---

## Компоненты клиента

### 1. CollabSettings

Класс конфигурации, хранящий параметры подключения к серверу.

| Свойство | Тип | Значение по умолчанию | Описание |
|----------|-----|----------------------|----------|
| `Enabled` | bool | `false` | Включён ли режим коллаборации |
| `ServerUrl` | string | `http://localhost:5000` | URL сервера CollabMCP |
| `ApiKey` | string | `""` | API-ключ для аутентификации |
| `UserId` | string | auto-generated | Уникальный ID пользователя (ticks) |
| `SessionId` | string | `""` | ID текущей сессии |
| `Username` | string | `Environment.UserName` | Имя пользователя системы |
| `ReconnectDelayMs` | int | `2000` | Задержка перед переподключением (мс) |
| `MaxReconnectAttempts` | int | `50` | Максимальное число попыток переподключения |

### 2. CollabClient

Основной класс клиента, управляющий WebSocket-соединением и обработкой событий.

#### Свойства

| Свойство | Тип | Описание |
|-----------|-----|----------|
| `IsConnected` | bool | Статус подключения (`WebSocketState.Open`) |
| `ReconnectAttempts` | int | Текущее число попыток переподключения |
| `_userColors` | ConcurrentDictionary | Словарь цветов пользователей (15-цветная палитра) |

#### События

| Событие | Делегат | Описание |
|----------|---------|----------|
| `OnConnected` | `CollabEventHandler` | Успешное подключение к серверу |
| `OnDisconnected` | `CollabEventHandler` | Разрыв соединения |
| `OnUserJoined` | `CollabUserEventHandler` | Пользователь присоединился к сессии |
| `OnUserLeft` | `CollabUserEventHandler` | Пользователь покинул сессию |
| `OnPrimitiveCreated` | `CollabPrimitiveEventHandler` | Создан новый примитив |
| `OnPrimitiveUpdated` | `CollabPrimitiveEventHandler` | Обновлён существующий примитив |
| `OnPrimitiveLocked` | `CollabLockEventHandler` | Примитив заблокирован |
| `OnPrimitiveUnlocked` | `CollabLockEventHandler` | Примитив разблокирован |
| `OnPrimitiveDeleted` | `CollabPrimitiveEventHandler` | Примитив удалён |
| `OnCanvasCleared` | `CollabEventHandler` | Canvas очищен |
| `OnSnapshotReceived` | `CollabEventHandler` | Получен полный снимок состояния |
| `OnError` | `CollabErrorEventHandler` | Произошла ошибка |

#### Основные методы

| Метод | Возвращаемый тип | Описание |
|-------|-----------------|----------|
| `ConnectAsync()` | `Task<bool>` | Установить WebSocket-соединение с сервером |
| `DisconnectAsync()` | `Task` | Закрыть соединение |
| `JoinSessionAsync(sessionId)` | `Task` | Присоединиться к сессии |
| `SendPrimitiveCreatedAsync(type, points, ...)` | `Task` | Отправить создание примитива |
| `SendPrimitiveUpdatedAsync(primitiveId, points, ...)` | `Task` | Отправить обновление примитива |
| `SendPositionUpdateAsync(primitiveId, points)` | `Task` | Отправить обновление позиции |
| `LockPrimitiveAsync(primitiveId)` | `Task` | Заблокировать примитив |
| `UnlockPrimitiveAsync(primitiveId)` | `Task` | Разблокировать примитив |
| `GetConnectedUsersAsync()` | `Task<List<string>>` | Получить список подключённых пользователей |
| `GetSessionStateAsync()` | `Task<Dictionary<string, object>>` | Получить полное состояние сессии |
| `GetUserColor(userId)` | `string` | Получить цвет пользователя (из палитры) |

#### Внутренние методы

| Метод | Описание |
|-------|----------|
| `ReceiveLoop(token)` | Цикл чтения сообщений из WebSocket |
| `ProcessMsg(json)` | Разбор JSON, dispatch по событиям |
| `SendStr(json)` | Отправка JSON-строки через WebSocket |
| `DescPrim(dict)` | Десериализация `Dictionary<string, object>` в `VectorPrimitiveData` |
| `InitColors()` | Инициализация 15-цветной палитры для пользователей |

### 3. VectorPrimitiveData

Клиентская модель данных примитива (string/float типы).

| Свойство | Тип | Описание |
|----------|-----|----------|
| `Id` | string | GUID примитива |
| `Type` | string | Тип: rectangle/polygon/ellipse/line/polyline |
| `Points` | List<float> | Координаты точек [x1, y1, x2, y2, ...] |
| `StrokeColor` | string | Цвет обводки (HEX, например `#000000`) |
| `StrokeWidth` | float | Ширина обводки |
| `FillColor` | string | Цвет заливки (`transparent` или HEX) |
| `CreatedBy` | string | ID создателя |
| `LockedBy` | string | ID блокирующего пользователя |
| `LockedAt` | string | Дата блокировки (ISO 8601) |
| `Version` | int | Версия примитива |
| `CreatedAt` | string | Дата создания (ISO 8601) |
| `UpdatedAt` | string | Дата обновления (ISO 8601) |

### 4. LockData

Модель данных блокировки.

| Свойство | Тип | Описание |
|----------|-----|----------|
| `PrimitiveId` | string | GUID примитива |
| `LockedBy` | string | ID пользователя, заблокировавшего примитив |
| `IsLocked` | bool | Заблокирован ли примитив |

### 5. EntityConverter

Статический класс для конвертации между клиентскими примитивами и внутренними сущностями Deroute.

#### Методы

| Метод | Возвращаемый тип | Описание |
|-------|-----------------|----------|
| `ToEntity(prim, userId)` | `Entity` | Конвертировать `VectorPrimitiveData` в `Entity` |
| `ToPrimitiveData(entity, userId)` | `VectorPrimitiveData` | Конвертировать `Entity` в `VectorPrimitiveData` |
| `CreateEntityBoxRegion(x1, y1, x2, y2, color)` | `VectorPrimitiveData` | Создать прямоугольный примитив для Region |

#### Маппинг типов

| `VectorPrimitiveData.Type` | `EntityType` |
|---------------------------|-------------|
| `rectangle`, `polygon` | `EntityType.Region` |
| `ellipse` | `EntityType.Region` |
| `line`, `polyline` | `EntityType.WireInterconnect` |
| `null` / `""` / другое | `EntityType.WireInterconnect` |

### 6. OfflineQueue

Класс для хранения изменений, сделанных при отключении от сервера, с последующей отправкой после переподключения.

| Элемент | Тип | Описание |
|---------|-----|----------|
| `_queue` | `ConcurrentQueue<OfflineChange>` | Очередь отложенных изменений |
| `Count` | int | Количество элементов в очереди |
| `Add(change)` | void | Добавить изменение в очередь |
| `Flush()` | `List<OfflineChange>` | Извлечь все изменения и очистить очередь |
| `Clear()` | void | Очистить очередь без отправки |

#### OfflineChange

| Свойство | Тип | Описание |
|----------|-----|----------|
| `Type` | string | Тип: `created` / `updated` |
| `PrimitiveId` | string | GUID примитива |
| `SessionId` | string | ID сессии |
| `Points` | List<float> | Координаты точек |
| `StrokeColor` | string | Цвет обводки |
| `StrokeWidth` | float | Ширина обводки |
| `FillColor` | string | Цвет заливки |
| `Timestamp` | DateTime | Время создания (UTC) |

### 7. CoordinateThrottler

Таймерный throttle для координатных обновлений, предотвращающий избыточную отправку.

| Элемент | Тип | Описание |
|---------|-----|----------|
| `OnFlush` | `Action<List<PositionUpdate>>` | Событие при сбросе накопленных обновлений |
| `_timer` | `Timer` | Внутренний таймер (интервал по умолчанию 33мс = ~30 FPS) |
| `AddUpdate(primitiveId, points)` | void | Добавить обновление (deduplication по primitiveId) |
| `Stop()` | void | Остановить таймер |

#### PositionUpdate

| Свойство | Тип | Описание |
|----------|-----|----------|
| `PrimitiveId` | string | GUID примитива |
| `Points` | List<float> | Новые координаты |
| `Timestamp` | DateTime | Время обновления (UTC) |

**Механизм работы:**
1. При вызове `AddUpdate` проверяется наличие обновления для того же `primitiveId`
2. Если найдено — заменяется (deduplication)
3. По таймеру (33мс) все накопленные обновления сбрасываются через событие `OnFlush`
4. Каждое обновление отправляется на сервер отдельно

---

## Интеграция с UI (FormMainCollab)

### Поля

| Поле | Тип | Описание |
|------|-----|----------|
| `_collabClient` | `CollabClient` | Основной клиент коллаборации |
| `_positionThrottler` | `CoordinateThrottler` | Throttle для position updates |
| `_offlineQueue` | `OfflineChangeQueue` | Очередь офлайн-изменений |
| `_collabStatusTimer` | `Timer` | Таймер обновления статуса (5 сек) |
| `_collabUserCount` | int | Количество пользователей в сессии |
| `_isSyncing` | bool | Флаг синхронизации (блокирует события) |
| `_entityOriginalColors` | `Dictionary<string, Color>` | Оригинальные цвета сущностей |
| `_entityLockOwners` | `Dictionary<string, string>` | Владельцы блокировок (entityId → userId) |

### Методы интеграции

| Метод | Описание |
|-------|----------|
| `InitializeCollab()` | Настройка всех обработчиков, создание throttler/queue/timer, автоподключение |
| `UpdateCollabStatus(status, userCount)` | Обновление статуса в UI с цветовой индикацией |
| `RefreshCollabStatus()` | Периодическое обновление статуса (каждые 5 сек) |
| `ApplyRemotePrimitive(data)` | Применение созданного удалённого примитива на canvas |
| `ApplyRemoteUpdate(data)` | Применение обновления удалённого примитива |
| `ApplyRemoteLock(lockData)` | Визуализация блокировки (overlay цветом пользователя) |
| `ApplyRemoteUnlock(lockData)` | Снятие визуализации блокировки |
| `ApplyRemoteDelete(data)` | Удаление примитива с canvas |
| `InvokeOnUiThread(action)` | Thread-safe вызов UI-методов |
| `QueueOfflineChange(change)` | Добавление изменения в offline-очередь |
| `FlushOfflineChanges()` | Отправка накопленных offline-изменений |

### Цветовая индикация статуса

| Статус | Цвет |
|--------|------|
| `Connected` | `Green` |
| `Error` / `Disconnected` | `Red` |
| Другое (Connecting, etc.) | `Orange` |

### Визуализация блокировки

При блокировке примитива:
1. Сохраняется оригинальный цвет в `_entityOriginalColors`
2. Цвет блокировщика берётся из палитры пользователя (`GetUserColor`)
3. Примитив перерисовывается с `Color.FromArgb(150, lockColor)` — полупрозрачный overlay
4. При разблокировке восстанавливается оригинальный цвет

---

## Протокол обмена (JSON-команды)

### Клиент → Сервер

| Команда | Параметры | Описание |
|----------|-----------|----------|
| `JoinSession` | `sessionId`, `userId` | Присоединиться к сессии |
| `SendPrimitiveCreated` | `sessionId`, `primitiveId`, `type`, `points`, `strokeColor`, `strokeWidth`, `fillColor`, `userId` | Создать примитив |
| `SendPrimitiveUpdated` | `sessionId`, `primitiveId`, `points`, `strokeColor`, `strokeWidth`, `fillColor`, `userId` | Обновить примитив |
| `SendPositionUpdate` | `sessionId`, `primitiveId`, `points`, `userId` | Обновить позицию |
| `LockPrimitive` | `sessionId`, `primitiveId` | Заблокировать примитив |
| `UnlockPrimitive` | `sessionId`, `primitiveId` | Разблокировать примитив |
| `GetConnectedUsers` | `sessionId` | Запросить список пользователей |
| `GetSessionState` | `sessionId` | Запросить состояние сессии |

### Сервер → Клиент

| Команда | Параметры | Описание |
|----------|-----------|----------|
| `OnUserJoined` | `userId`, `snapshot` | Пользователь присоединился (с snapshot) |
| `OnUserLeft` | `userId` | Пользователь покинул сессию |
| `OnPrimitiveCreated` | `primitive` | Создан новый примитив |
| `OnPrimitiveUpdated` | `primitive` | Обновлён примитив |
| `OnPrimitiveLocked` | `primitive` | Примитив заблокирован |
| `OnPrimitiveUnlocked` | `primitive` | Примитив разблокирован |
| `OnPrimitiveDeleted` | `primitiveId` | Примитив удалён |
| `OnCanvasCleared` | — | Canvas очищен |
| `OnPositionUpdated` | `primitiveId`, `points` | Обновление позиции |
| `OnSnapshot` | — | Полный снимок состояния |
| `OnError` | `error` | Ошибка |

---

## Формат данных примитива (JSON)

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

## Палитра пользователей

15-цветная палитра для идентификации пользователей в коллаборации:

| # | Цвет | HEX |
|---|------|-----|
| 1 | 🔴 Красный | `#FF6B6B` |
| 2 | 🟢 Бирюзовый | `#4ECDC4` |
| 3 | 🔵 Голубой | `#45B7D1` |
| 4 | 🟢 Зелёный | `#96CEB4` |
| 5 | 🟡 Жёлтый | `#FFEAA7` |
| 6 | 🟣 Розовый | `#DDA0DD` |
| 7 | 🟢 Светло-зелёный | `#98D8C8` |
| 8 | 🟡 Лимонный | `#F7DC6F` |
| 9 | 🟣 Фиолетовый | `#BB8FCE` |
| 10 | 🔵 Светло-синий | `#85C1E9` |
| 11 | 🟠 Оранжевый | `#F8C471` |
| 12 | 🟡 Светло-зелёный | `#82E0AA` |
| 13 | 🔴 Коралловый | `#F1948A` |
| 14 | ⚫ Серо-синий | `#85929E` |
| 15 | 🟢 Мятный | `#73C6B6` |

---

## Настройка (CollabMCP в FormSettings)

Настройки CollabMCP доступны во вкладке "CollabMCP" в настройках приложения через `PropertyGrid`.

| Параметр | Описание |
|----------|----------|
| `Enabled` | Включить коллаборацию |
| `ServerUrl` | URL сервера |
| `ApiKey` | API-ключ |
| `UserId` | ID пользователя |
| `SessionId` | ID сессии |
| `Username` | Имя пользователя |
| `ReconnectDelayMs` | Задержка переподключения |
| `MaxReconnectAttempts` | Макс. попыток переподключения |

---

## Механизм переподключения

1. При разрыве соединения клиент генерирует событие `OnError`
2. `FormMainCollab` обновляет статус в UI (красный цвет)
3. Пользователь может нажать правую кнопку мыши на статусной панели → "Reconnect"
4. Вызывается `ConnectAsync()` для повторного подключения
5. После подключения автоматически запрашивается `GetSessionStateAsync()` для синхронизации
6. Полученный snapshot применяется к canvas через `ApplyRemotePrimitive`

---

## Механизм offline-изменений

1. При отключении от сервера пользователь продолжает работать с canvas
2. Все изменения (создание, обновление примитивов) добавляются в `_offlineQueue`
3. При переподключении `FlushOfflineChanges()` отправляет накопленные изменения на сервер
4. Каждое изменение отправляется как соответствующая команда (`SendPrimitiveCreatedAsync` / `SendPrimitiveUpdatedAsync`)

---

## Безопасность

| Механизм | Описание |
|----------|----------|
| API-ключ | Все запросы к серверу содержат заголовок `X-Api-Key` |
| Валидация | Сервер проверяет ключ через `ApiKeyAuthMiddleware` |
| Блокировки | Примитив может быть изменён только заблокировавшим пользователем |
| Версионирование | Каждый примитив имеет `Version` для отслеживания изменений |

---

## Зависимости

| Зависимость | Назначение |
|-------------|-----------|
| `System.Net.WebSockets` | WebSocket-клиент |
| `MiniJson` | Парсинг JSON |
| `System.Drawing` | Работа с цветами (`ColorTranslator`) |
| `System.Windows.Forms.Timer` | Таймеры UI |
| `System.Collections.Concurrent` | Потокобезопасные коллекции |

---

## Примечания по разработке

1. **Потокобезопасность:** Все операции с `_offlineQueue`, `_pendingUpdates` используют `ConcurrentDictionary` / `ConcurrentQueue`
2. **UI-вызовы:** Все обновления UI выполняются через `InvokeOnUiThread` для предотвращения cross-thread exceptions
3. **Троттлинг:** Position updates throttle (33мс = ~30 FPS) предотвращает избыточную нагрузку на сервер
4. **Deduplication:** CoordinateThrottler объединяет несколько обновлений одного примитива в одно
5. **Auto-unlock:** При отключении пользователя сервер автоматически снимает все его блокировки
