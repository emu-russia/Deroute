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

## Рисование проводов через Shift

В режиме ViasConnect можно рисовать проводами, если удерживать Shift. В этом случае вместо ViasConnect будет рисоваться сегмент провода (WireInterconnect).

## Массовое переименование сущностей

Tools -> Bulk Rename. Тут всё понятно.

![bulk_rename.png](imgstore/bulk_rename.png)

### Traverse Black List

Свойство `TraverseBlackList` позволяет указать список типов сущностей, к которым нельзя осуществлять traverse (переход/расширение выделения).

Это полезно для предотвращения некорректного соединения сущностей при выделении. Например, можно запретить проводу соединяться с определёнными типами виасов или ячеек.

Для настройки:
- Выберите сущность в редакторе
- В окне свойств (PropertyGrid) найдите свойство `TraverseBlackList`
- Нажмите на кнопку с троеточием `...` справа от поля
- Откроется диалог, где можно добавить или удалить типы сущностей из чёрного списка
- Доступны все типы сущностей, кроме `Root`

При работе с traverse (расширением выделения) система будет игнорировать сущности из чёрного списка, предотвращая некорректные соединения.

Пример: если добавить `ViasPower` в TraverseBlackList провода, то при попытке traverse провод не будет соединён с силовыми виасами.

## Подсветка пересекающихся сущностей

Tools -> Show Overlapped Entities.

Функция находит все сущности, которые пересекаются (накладываются) друг на друга, и подсвечивает их (устанавливает `Selected = true`).

### Типы обнаруживаемых пересечений
- Cell ↔ Cell (с учётом полигонов)
- Region ↔ Region
- Cell ↔ Region
- Wire ↔ Wire
- Via ↔ Via
- Cell/Region содержит Via
- Wire пересекает Cell/Region
- Via на Wire

### Как использовать
1. Загрузите XML scene
2. Tools → **Show Overlapped Entities**
3. Пересекающиеся сущности подсветятся зелёным цветом
4. Выделите подсветленные сущности и удалите дубликаты
