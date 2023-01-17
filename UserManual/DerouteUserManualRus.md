# Руководство по Deroute

## Обзор

Утилита Deroute предназначена для реверс инжиниринга микросхем и материнских плат.

XXX: Данная инструкция пока включает не все разделы. В принципе всё остальное самоописательное и можно просто потыкать кнопочки, чтобы разобраться.
Инструкция будет расширена со временем.

## Загрузка и сохранение данных

### Экспорт в Verilog

A script to convert XML to Verilog RTL. We get a kind of "disassembly" of the chip, with which you can work further as with the sources of HDL.

Principle of conversion:
- All cells (entities of `Cell` type) and custom blocks (entities of `Unit` type) become module instances. The direct definition of the cell/block logic is at the user's choice.
- All input/output/input vias within a cell/block become ports and wire connections are assigned by name (`.port_name(wire_xxx)`).
- If the port has no name, an error is output. All cell/block ports must have names.
- The instance name is taken from the `Label` property of the cell/block. The first word is the module name, the second word (if any) is the instance name. If there is no name, then a name of the form `g1`, `g2` and so on is generated. So it would be better to have a cell name too, to understand what kind of cell it is in the HDL listing.
- The ports for the top module are all input/output/inout vias NOT of cells. All ordinary vias become open-end wires and go into the HDL as is.
- Wires are obtained by combining segments by traverse. The wire name is taken by concatenating all segment names with a underscore (`_`), if the result is an empty string, then the wire name is generated as `w1`, `w2` and so on.
- If among all wire entities is ViasPower / ViasGround - then instead of wire connect to `1'b1` / `1'b0` constants

The script does not check connectivity and does not make any special checks at all. All errors can be checked later when using the generated HDL in your favorite CAD.

## Локатор сущностей

Для быстрого получения списка требуемых сущностей можно воспользоваться локатором сущностей (Tools -> Entity Locator).

![entity_locator](imgstore/entity_locator.png)

- Можно указать часть имени сущности (Label). Если поле ввода текста пустое, то имя сущности будет игнорироваться
- В списке можно выбрать типы требуемых сущностей. Кроме точного указания типа (напр. `ViasInout`) можно также указать "Все виасы", "Все провода", "Все ячейки" или "Все юниты".

При двойном клике сущность будет показана в главном окне.

## Машинное обучение

Нейросети применяются для распознавания элементов изучаемой микросхемы или PCB (виасы, провода, стандартные ячейки).

Весь функционал для работы с нейросетями производится через кнопку с мозгом.

![machine_learning_tools.png](imgstore/machine_learning_tools.png)

### Создать ML модель

Данный диалог используется для создания ML модели (нейросети).

![create_ML_model.png](imgstore/create_ML_model.png)

Модель включает в себя гиперпараметры нейросети (такие как `learningRate`), а также список фич, которые по сути являются выходами нейросети.

На вход нейросети подается небольшой кусочек основного изображения (16x16), а нейросеть на выходе выдает результат (производит классификацию).

На место найденной фичи ставится сущность, которая указана в столбце `Entities`.

Список свойств фичи:
- Name: имя фичи для отображения и обучения
- Description: просто описание для наглядности
- Image: просто картинка для наглядности. Можно выбрать любую иконку чисто в информативных целях. При нажатии открывается диалог выбора иконки
- Entites: сущности, которые необходимо добавить на место распознанной фичи. При нажатии на ячейку открывается MiniEntityBox, где нужно нарисовать сущности

Рекомендации по количеству фич:
- Нужно хотя бы 2 фичи, чтобы нейросеть могла делать корректную классификацию
- Фичи желательно выбирать максимально разными. Тут нужно воспользоваться своей интуицией - если человек однозначно отличает одну фичу от другой (например виас от провода), то этого
можно ожидать и от нейросети.

### Загрузить и сохранить ML модель

Самоописательные операции.

ML модель сохраняется в XML файл.

### Посмотреть текущую загруженную ML модель

Для того чтобы посмотреть текущую ML модель нужно нажать в строке состояние на пункт `Neural model`:

![show_ML_model.png](imgstore/show_ML_model.png)

### Обучение модели

Самая веселая часть.

Для обучения модели необходимо предварительно загрузить исходное изображение (File -> Load Image).

Диалог обучения выглядит следующим образом:

![train_ML_model.png](imgstore/train_ML_model.png)

- Neighbour field: участок исходного изображения откуда будет браться небольшой кусочек, 16x16 пикселей. Мышкой можно двигать зеленый квадратик.
- Feature: отображается кусочек которому нужно обучить нейросеть
- Guess: этой кнопкой можно проверить что думает нейросеть по выбранной фиче. Так можно проверять обученность нейросети. Над кнопкой Guess появится описание фичи (иконка, которую вы выбрали при создании модели (Image), описание (Description) и имя фичи (Name)).
Если нейросеть не знает что это за фича, она так и скажет.
- Список фич: в этом списке нужно выбрать фичу для обучения нейросети
- Next: пропустить обучение и выбрать следующую фичу. Это можно делать когда на изображении какой-то мусор или хочется обучить нейросеть на другом участке изображения
- Train: нейросеть обучается указанной из списка фичи

Рекомендации по обучению:
- Не следует задрачивать нейросеть одной фичей подряд. Такое обучение переобучит и разбалансирует нейросеть. Нужно стараться как можно чаще менять field и обучать нейросеть разным фичам.
- Практика показала, что натаскать сеть на простое распознавание (провода, виасы) достаточно обучить её примерно 100-200 раз.

### Запуск модели

Для запуска модели необходимо предварительно загрузить исходное изображение (File -> Load Image).

После запуска отображается modeless диалог с прогрессом сканирования:

![ML_Running.png](imgstore/ML_Running.png)

Распознавание исходного изображения производится зигзагом, с шагом 1 пиксель.

Процесс распознавания можно в любой момент остановить, закрыв диалог `Run Model`.

В результате распознавания получается примерно следующее:

![ML_results.jpg](imgstore/ML_results.jpg)

XXX: В настоящее время найденные сущности "наслаиваются" друг на друга, это будет исправлено в следующем релизе.

XXX: В настоящее время диалог не пишет процент готовности и не закрывается автоматически, это будет исправлено в следующем релизе.

Указанные недостатки не препятствуют обучению модели, поэтому ваша обученная модель может быть использована и потом, после устранения вышеуказанных недостатков.

### Рисование проводов через Shift

В режиме ViasConnect можно рисовать проводами, если удерживать Shift. В этом случае вместо ViasConnect будет рисоваться сегмент провода (WireInterconnect).

### Массовое переименование сущностей

Tools -> Bulk Rename. Тут всё понятно.

![bulk_rename.png](imgstore/bulk_rename.png)
