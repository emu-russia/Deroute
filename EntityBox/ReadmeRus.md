# Кастомный контрол EntityBox

EntityBox ("ящик с сущностями") является ключевым компонентом для всех наших утилит.

Данный контрол представляет собой контейнер бесконечного размера, в котором содержатся различные "сущности" (entity).

Конкретно для целей реверса микросхем набор сущностей подобран таким образом, чтобы они соответствовали основным компонентам архитектуры микросхем:
- Провода (Wires)
- Виасы (Vias)
- Стандартные ячейки различных классов (Cells)

Пример контрола с несколькими проводами, виасами и одной стандартной ячейкой:

![image](https://user-images.githubusercontent.com/5828819/59331925-c1f23000-8cfd-11e9-9242-4db70b51e9be.png)

Сущности хранятся в виде дерева.

## Соглашения по тексту

В дальнейшем _италиком_ будут отмечены свойства EntityBox.

## Как использовать контрол в своем проекте

Для этого необходимо добавить в ваше решение проект EntityBox.csproj и добавить его в зависимости вашего приложение (Reference)

После чего в панели контролов появится EntityBox:

![image](https://user-images.githubusercontent.com/5828819/59332346-a89db380-8cfe-11e9-96cc-4b33babedf0c.png)

## Лямбда

Все измерения внутри EntityBox производятся в единицах измерения - лямбда.

В цифровой электронике за 1 единицу лямбда принято считать самую мелкую деталь микросхемы, на которую способен технически процесс её изготовления (обычно это ширина затвора транзистора).

EntityBox содержит свойство _Lambda_, которое задает количество пикселей экрана, соответствующее 1 единице лямбда. Так как это свойство имеет тип float, можно задавать нецелое число пикселей. Хотя обычно значение _Lambda_ = 5.0

## Состав сборки EntityBox

В сборку входит ряд модулей, в которых содержатся публичные методы для работы с контролом:

- EntityBox: основной модуль с инициализацией
- AddEntity: добавление сущностей
- CopyPaste: копирование и вставка сущностей
- DeleteEntity: удаление сущностей
- Drawing: отрисовка контрола
- Images: работа с изображениями на слоях картинок
- KeyInput: ввод с клавиатуры
- Math: математические преобразования
- MouseInput: ввод с мыши
- Selection: выделение сущностей
- Serialize: сериализация (сохранение как Xml)
- Sort: сортировка сущностей по приоритету (TODO: после введения древовидной иерархии тут требуются исправления)

А также несколько вспомогательных утилит:
- Traverse: трассировка проводов
- WireExtend: увеличение или укорачивание проводов на концах
- WireMerger: сшиватель коллинеарных проводов
- WireRecognizer: распознаватель проводов (этот модуль будет выпилен и заменен нейросетью)
- WireRouter: укладчик проводов между двумя виасами

## Свойства EntityBox

Свойства контрола предназначены в основном для управления рабочей средой:
- Zoom: задает общий зум всем слоям
- ScrollX,ScrollY: смещение отображения всех слоев относительно начала (прокрутка)
- HideImage: скрыть все слои картинок
- HideVias: скрыть все виасы
- HideWires: скрыть все провода
- HideCells: скрыть все стандартные ячейки
- HideGrid: скрыть сетку
- HideLambdaMetrics: скрыть шкалу масштаба
- HideRegions: скрыть все регионы
- BeaconImage: задает картинку маяка (Beacon)
- Image0,1,2: задает картинку для соответствующего слоя картинок
- ImageOpacity0,1,2: задает прозрачность (0-255) для соответствующего слоя картинки
- SelectEntitiesAfterAdd: автоматически выделает добавляемые сущности
- Grayscale: устанавливает все картинки в черно-белый формат
- OptimizeTilemap: использовать ускоренный рендеринг изображений используя разбиение на тайлы
- Lambda: сколько пикселей соответствует 1 единице лямбда
- Mode: режим работы (см. далее)
- ScrollImage0,1,2: смещение картинки относительно начала координат
- LockScroll0,1,2: запрещает прокрутку соответствующих слоев картинок
- LockZoom0,1,2: запрещает зум соответствующих слоев картинок
- SelectionBoxColor: цвет рамки выделения
- SelectionColor: цвет подсветки выделенных сущностей

Свойства, определяющие цвета по умолчанию для соответствующих типов сущностей:
- ViasInputColor: цвет по умолчанию для сущностей типа ViasInput
- ViasOutputColor: цвет по умолчанию для сущностей типа ViasOutput
- ViasInoutColor: цвет по умолчанию для сущностей типа ViasInout
- ViasConnectColor: цвет по умолчанию для сущностей типа ViasConnect
- ViasFloatingColor: цвет по умолчанию для сущностей типа ViasFloating
- ViasPowerColor: цвет по умолчанию для сущностей типа ViasPower
- ViasGroundColor: цвет по умолчанию для сущностей типа ViasGround
- WireInterconnectColor: цвет по умолчанию для сущностей типа WireInterconnect
- WirePowerColor: цвет по умолчанию для сущностей типа WirePower
- WireGroundColor: цвет по умолчанию для сущностей типа WireGround
- CellNotColor: цвет по умолчанию для сущностей типа CellNot
- CellBufferColor: цвет по умолчанию для сущностей типа CellBuffer
- CellMuxColor: цвет по умолчанию для сущностей типа CellMux
- CellLogicColor: цвет по умолчанию для сущностей типа CellLogic
- CellAdderColor: цвет по умолчанию для сущностей типа CellAdder
- CellBusSuppColor: цвет по умолчанию для сущностей типа CellBusSupp
- CellFlipFlopColor: цвет по умолчанию для сущностей типа CellFlipFlop
- CellLatchColor: цвет по умолчанию для сущностей типа CellLatch
- CellOtherColor: цвет по умолчанию для сущностей типа CellOther
- UnitRegfileColor: цвет по умолчанию для сущностей типа UnitRegfile
- UnitMemoryColor: цвет по умолчанию для сущностей типа UnitMemory
- UnitCustomColor: цвет по умолчанию для сущностей типа UnitCustom
- ViasOverrideColor: если виасу сделать цвет отличный от Override, о он будет иметь другой цвет
- WireOverrideColor: если проводу сделать цвет отличный от Override, о он будет иметь другой цвет
- CellOverrideColor: если стандартной ячейке сделать цвет отличный от Override, о он будет иметь другой цвет
- RegionOverrideColor: если региону сделать цвет отличный от Override, о он будет иметь другой цвет

При добавлении сущностей им назначается цвет Override. Если данный цвет оставить как есть, то цвет сущности будет определяться настройками среды (список цветов выше). Если цвет сущности поменять на цвет отличный от значения Override, то сущность будет иметь указанный цвет.

Пример:

![image](https://user-images.githubusercontent.com/5828819/59355410-98ea9300-8d2f-11e9-8e0a-ebc7330f6b4f.png)
![image](https://user-images.githubusercontent.com/5828819/59355592-e830c380-8d2f-11e9-9db8-c7d3c54d729a.png)

Провод на первом изображении имеет цвет Black, который соответствует настройкам _WireOverrideColor_ = Black, поэтому он отображается цветом по умолчанию _WireInterconnectColor_ = Blue. На втором изображении проводу установили цвет Orange, который не соответствует настройкам _WireOverrideColor_ = Black, поэтому провод отображается оранжевым цветом.

Другие свойства:
- ViasPriority: приоритет который задается всем создаваемым виасам
- WirePriority: приоритет который задается всем создаваемым проводам
- CellPriority: приоритет который задается всем создаваемым стандартным ячейкам
- BeaconPriority: приоритет который задается всем создаваемым маякам
- RegionPriority: приоритет который задается всем создаваемым регионам
- AutoPriority: не помню для чего
- ViasOpacity: прозрачность для висасов (0-255)
- WireOpacity: прозрачность для проводов (0-255)
- CellOpacity: прозрачность для стандартных ячеек (0-255)
- ViasShape: выбирает один из способов отрисовки виасов (квадрартный или круглый)

## События контрола

Родительское приложение может установить обработчики на события, которые происходят в контроле:
- OnScrollChanged: Вызывается после перемещения слоя сущностей
- OnZoomChanged: Вызывается после изменения зума слоя сущностей 
- OnEntityCountChanged: Вызывается после изменения общего количетсва сущностей
- OnEntityLabelEdit: Вызывается после редактирования текстовой пометки (Label) сущности
- OnEntitySelect: Вызывается после выделения сущности
- OnEntityAdd: Вызывается после добавления сущности
- OnEntityRemove: Вызывается после удаления сущности
- OnEntityScroll: Вызывается после изменения положения сущности на слое сущностей
- OnDestinationNodeChanged: Вызывается после изменения целевой сущности (см. далее что такое целевая сущность)
- OnFrameDone: Вызывается после отрисовки контрола

Делается это обычным образом через панель Properties:

![image](https://user-images.githubusercontent.com/5828819/59333558-3e3a4280-8d01-11e9-9708-86a4e7afc124.png)

## Список типов сущностей

Типы определены в EntityBox.cs (enum EntityType).

Специальные типы:
- Root: корень всех сущностей
- Beacon: маяк для быстрой навигации по слою сущностей
- Region: регион произвольной формы
- Layer: невидимая сущность, используемая как контейнер для остальных дочерних сущностей. Выполняет роль "слоя" из фотошопа. Пока нет возможности добавлять непосредственно слои, поэтому для добавления слоя можно добавить например Vias и поменяет его тип на Layer.

Виасы:
- ViasOutput: выходной контакт
- ViasInout: двунаправленный контакт
- ViasConnect: виас соединяющий два провода
- ViasFloating: виас не соединенный ни с чем ("плавающий")
- ViasPower: соединяет с питанием (1)
- ViasGround: соединяет с землей (0)

Провода:
- WireInterconnect: обычный соединительный провод
- WirePower: провод для питания
- WireGround: провода для земли

Стандартные ячейки:
- CellNot: инвертор
- CellBuffer: усилтельный буфер
- CellMux: мультиплексор
- CellLogic: логический элемент (NAND, NOR итп)
- CellAdder: элемент ALU
- CellBusSupp: вспомогательные ячейки типа BusKeeper
- CellFlipFlop: триггеры по фронту (например DFF)
- CellLatch: триггеры по уровню ("защелки")
- CellOther: прочие ячейки

Кастомные блоки:
- UnitRegfile: регистровый файл
- UnitMemory: память или другие подобные блоки (например для хранения параметров)
- UnitCustom: прочие блоки

## Свойства и методы сущностей

Все свойства собраны в EntityBox.cs

- UserData: данные произвольного содержания (int)
- Label: текстовая пометка которая отображается рядом с сущностью
- LabelAlignment: выравнивание текстовой пометки
- LambdaWidth: ширина сущности в лямбдах
- LambdaHeight: высота сущности в лямбдах 
- LambdaX: координата X на плоскости сущностей
- LambdaY: координата Y на плоскости сущностей
- LambdaEndX: конечная координата X (например для Wire) на плоскости сущностей
- LambdaEndY: конечная координата Y (например для Wire) на плоскости сущностей
- Type: тип сущности (EntityType)
- Selected: пометка что сущность является выделенной
- Priority: приоритет сущности. Влияет на последовательность отрисовки: сущности с большим приоритетом рисуются поверх сущностей с более низким приоритетом
- PathPoints: набор координат для сущности типа Region
- Children: список дочерних сущностей
- Visible: определяет видимость сущности. Если сущность невидима, то все дочерние сущности также невидимы
- ColorOverride: используется для персонального задания цвета сущности, отличного от общего цвета для данных типов сущностей
- FontOverride: используется для персонального задания шрифта для пометки сущности (Label), отличного от общего шрифта контрола
- WidthOverride: используется для переопределения ширины сущности, отличной от настроек ширина данного типа сущностей

Вспомогательные свойства (только для чтения):
- WireLengthLambda: возвращает длину провода в лямбдах
- WireTangent: возвращает наклон провода

Вспомогательные методы:
- SetParentControl: позволяет задать родительский контрол. Используется при авто-обновлении контрола, после изменения свойств сущности
- IsWire: проверить является ли сущность одним из типов проводов (Wire)
- IsVias: проверить является ли сущность одним из типов виасов (Vias)
- IsCell: проверить является ли сущность одним из типов стандартных ячеек (Cell)
- IsRegion: проверить является ли сущность регионом

## Режим работы EntityBox

Режим работы (свойство _Mode_) определяет способ взаимодействия с пользователем посредством клавиатуры и мыши.

Список режимов (enum EntityMode):
- Selection: выбран слой с сущностями для их выделения и перемещения, а также прокрутки и зума рабочей среды
- ImageLayer0: выбран слой картинок 0 (самый верхний слой картинок)
- ImageLayer1: выбран слой картинок 1
- ImageLayer2: выбран слой картинок 2
- ViasInput: режим добавления сущностей ViasInput
- ViasOutput: режим добавления сущностей ViasOutput
- ViasInout: режим добавления сущностей ViasInout
- ViasConnect: режим добавления сущностей ViasConnect
- ViasFloating: режим добавления сущностей ViasFloating
- ViasPower: режим добавления сущностей ViasPower
- ViasGround: режим добавления сущностей ViasGround
- WireInterconnect: режим добавления сущностей WireInterconnect
- WirePower: режим добавления сущностей WirePower
- WireGround: режим добавления сущностей WireGround
- CellNot: режим добавления сущностей CellNot
- CellBuffer: режим добавления сущностей CellBuffer
- CellMux: режим добавления сущностей CellMux
- CellLogic: режим добавления сущностей CellLogic
- CellAdder: режим добавления сущностей CellAdder
- CellBusSupp: режим добавления сущностей CellBusSupp
- CellFlipFlop: режим добавления сущностей CellFlipFlop
- CellLatch: режим добавления сущностей CellLatch
- CellOther: режим добавления сущностей CellOther
- UnitRegfile: режим добавления сущностей UnitRegfile
- UnitMemory: режим добавления сущностей UnitMemory
- UnitCustom: режим добавления сущностей UnitCustom
- Beacon: режим добавления сущностей Beacon

## Слои

EntityBox содержит несколько слоев отображения
- Слой с сущностями. Слой с сущностями размещается над слоями картинок.
- 3 слоя для хранения картинок (микросхем). Слой 0 является самым верхним из слоев картинок.

Для слоев картинок можно менять прозрачность (_ImageOpacity_).

Также для слоев картинок можно запретить их перемещение (_LockScroll_) и зум (_LockZoom_). Сделано это в целях предотвращения случайного перемещения и зума.

## Особенности отрисовки контрола

Контрол перехватывает метод OnPaint и использует двойную буферизацию для ускорения отрисовки.

Последовательность отрисовки:
- Задний фон заливается стандартным цветом (BackColor)
- Отрисовываются слои картинок в порядке 2, 1, 0
- Рисуется сетка
- Рисуются видимые сущности в порядке приоритета. Выделенные сущности подсвечиваются цветом _SelectionColor_
- При необходимости рисуется рамка выделения и анимация рисования провода

## Особенности управления с клавиатуры и мыши

Контрол расширяет метод OnKeyUp и обрабатывает следующие нажатия клавиш:
- Delete: удалить выделенные сущности
- Escape: снять выделение со всех сущностей
- Home: переместить экран на начало координат
- Стрелки: перемещает выделенные сущности на 0.1 лямбду в указанную сторону

Контрол расширяет метод OnKeyDown и обрабатывает следующие нажатия клавиш:
- Ctrl+C: скопировать выделенные сущности
- Ctrl+V: вставить скопированные сущности

Контрол расширяет методы OnMouseDown, OnMouseUp и OnMouseMove, чтобы реализовать взаимодействие с пользователем:
- Правая кнопка используется для прокрутки экрана (скроллинг)
- Левая кнопка используется для перемещения сущностей в режиме _Mode_ = _Selected_ или для добавления новых сущностей, при этом режим _Mode_ определяет тип добавляемых сущностей
- Левая кнопка в режиме _Mode_ = _Selected_ используется для выделения сущностей рамкой
- Левая кнопка в режиме _Mode_ = _ImageLayer_ производит перемещение начала изображения в соответствующем слое картинок

Так как на левую кнопку навешано достаточно много операций, иногда происходит непреднамеренное смещение в слое картинок. Для этого было добавлено свойство _LockScroll_, чтобы пользователь случайно не сдвинул картинку.

Контрол расширяет метод OnMouseWheel для изменения общего зума (_Zoom_) или зума отдельных слоев картинок (_ZoomImage_). Способ зума выбирается текущим режимом (_Mode_).

## Методы EntityBox

- EntityBox: конструктор, определяет свойства контрола по умолчанию
- GetEntities: получить список всех сущностей (дерево преобразуется в список)
- SetDestinationNode: задать целевую сущность, куда будут добавляться все дочерние сущности
- EnsureVisible: переместить прокрутку таким образом, чтобы указанную сущность было видно
- GetViasCount: получить количество виасов
- GetWireCount: получить количество проводов
- GetCellCount: получить количество стандартных ячеек
- GetBeaconCount: получить количество маяков
- GetBeacons: получить список маяков
- ScrollToBeacon: установить прокрутку на указанный маяк (для быстрой навигации)
- LabelEdited: необходимо вызывать после изменения текстовой пометки сущности
- LambdaScale: лямбда трансформация (массово изменить масштаб всех выделенных сущностей)

## Методы AddEntity

- AddVias: добавить виас, используя координаты экрана
- AddWire: добавить провод, используя координаты экрана
- AddWireOnImage: добавить провод используя координаты картинки 
- AddCell: добавить стандартную ячейку, используя координаты экрана
- AddRegion: добавить регион, используя координаты экрана
- DrawRegionBetweenSelectedViases: добавить регион, используя координаты выделенных виасов
- DrawWireBetweenSelectedViases: добавить один или несколько проводов, используя координаты выделенных виасов

## Методы CopyPaste

- Copy: скопировать выделенные сущности во внутренний список copied
- Paste: вставить сущности из внутреннего списка copied в координаты прицела

## Методы DeleteEntity

- DeleteAllEntites: удалить все сущности
- DeleteSelected: удалить выделенные сущности
- RemoveSmallWires: удалить провода короче указанного размера
- RemoveNonOrthogonalWires: удалить не ортогональные провода
- RemoveEntity: удалить указанную сущность

## Методы Drawing

Не предоставляет публичных методов.

## Методы Images

- ResizeImage: изменить размер изображения
- SaveSceneAsImage: сохранить сцену как изображение в указанный файл
- ColorToGrayscale: получить черно-белое изображение
- LoadImage: загрузить изображение в текущий слой картинки (слой выбирается режимом _Mode_)
- UnloadImage: выгрузить изображение из текущего слоя картинки (слой выбирается режимом _Mode_). Принудительно вызывается сборщик мусора (GC.Collect)

## Методы KeyInput

Не предоставляет публичных методов.

## Методы Math

- ScreenToLambda: преобразовать координаты экрана в координаты лямбда
- ImageToLambda: преобразовать координаты картинки в координаты лямбда
- LambdaToScreen: преобразовать координаты лямбда в координаты экрана
- LambdaToImage: преобразовать координаты лямбда в координаты картинки
- LineIntersectsRect: определить пересечение линии и прямоугольника (True/False)

## Методы MouseInput

- GetLastRightMouseButton: получить последние координаты после нажатия правой кнопки мыши (координаты прицела)
- GetDragDistance: получить дистанцию на которую были перемещены мышкой сущности

## Методы Selection

- RemoveSelection: снять выделение со всех сущностей
- AssociateSelectionPropertyGrid: TBD
- GetSelected: получить список выделенных сущностей
- GetSelectedVias: получить список выделенных виасов
- GetSelectedWires: получить список выделенных проводов
- GetLastSelected: получить последнюю выделенную сущность
- SelectEntity: выделить сущность
- SelectAll: выделить все сущности

## Методы Serialize

- Serialize: сохранить все сущности в Xml файл
- Unserialize: загрузить сущности из Xml файла

## Методы Sort

- SortEntities: сортировать сущности в порядке возрастания приоритета
- SortEntitiesReverse: сортировать сущности в порядке убывания приоритета

## Вспомогательные утилиты

### Traverse

- TraversalSelection: подсвечивает все провода, которые соединяются друг с другом и со стандартными ячейками.

Параметр TierMax позволяет задать на какую глубину проводить трассировку. Если указано значение 1, то трассировка провода останавливается на первой стандартной ячейке до которой он дошёл. Чем больше значение параметра TierMax, тем на большую глубину можно провести трассировку.

### WireExtend

- WireExtendHead: удлинить начало провода
- WireExtendTail: удлинить конец провода
- WireShortenHead: укоротить начало провода
- WireShortenTail: укоротить конец провода

Удлинение/укорочение производится на небольшое значение лямбда.

### WireMerger

- MergeSelectedWires: производит слияние выделенных проводов, делая из нескольких проводов один

### WireRecognizer

Данный модуль будет заменен нейросетями.

Как работает старый дубовый алгоритм можно почитать тут:
https://github.com/ogamespec/psxdev/blob/master/docs/wire_recognition.pdf

### WireRouter

- Route: проводит провода между двумя указанными виасами и списком сущностей-"стенами"

Для укладки проводов применяется алгоритм AStar.

![image](https://user-images.githubusercontent.com/5828819/59359508-21b8fd00-8d37-11e9-8b8e-3c8cc1743837.png)
