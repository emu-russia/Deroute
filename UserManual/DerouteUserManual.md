# Deroute Manual

## Overview

The Deroute utility is designed for reverse engineering chips and motherboards.

XXX: This manual does not yet include all sections. Basically everything else is self-descriptive and you can just poke the buttons to figure it out.
The instructions will be expanded over time.

## Loading and saving data

### Export to Verilog

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

## Entity Locator

You can use the entity locator (Tools -> Entity Locator) to quickly get a list of your desired entities.

![entity_locator](imgstore/entity_locator.png)

- You can specify part of the entity name (Label). If the text field is empty, the entity name will be ignored.
- In the list you can select the types of entities required. Besides specifying the exact type (e.g. `ViasInout`) you can also specify "All Vias", "All Wires", "All Cells" or "All Units".

When you double-click the entity will be shown in the main window.

## Drawing Wires with Shift

In ViasConnect mode, you can draw wires by holding down Shift. In this case a wire segment (WireInterconnect) will be drawn instead of ViasConnect.

## Bulk renaming of entities

Tools -> Bulk Rename. Everything is straightforward here.

![bulk_rename.png](imgstore/bulk_rename.png)

### Traverse Black List

The `TraverseBlackList` property allows you to specify a list of entity types that cannot be traversed to (i.e., selection expansion will not reach them).

This is useful for preventing incorrect connections between entities during selection. For example, you can prevent a wire from connecting to certain types of vias or cells.

To configure:
- Select an entity in the editor
- In the Properties window (PropertyGrid), find the `TraverseBlackList` property
- Click the `...` button to the right of the field
- A dialog will open where you can add or remove entity types from the blacklist
- All entity types are available except `Root`

When using traverse (selection expansion), the system will skip entities in the blacklist, preventing incorrect connections.

Example: if you add `ViasPower` to a wire's TraverseBlackList, the wire will not connect to power vias during traverse.

## Highlighting Overlapped Entities

Tools -> Show Overlapped Entities.

The function finds all entities that overlap each other and highlights them (`Selected = true`).

### Types of detected overlaps
- Cell ↔ Cell (with polygon support)
- Region ↔ Region
- Cell ↔ Region
- Wire ↔ Wire
- Via ↔ Via
- Cell/Region contains Via
- Wire intersects Cell/Region
- Via on Wire

### How to use
1. Load XML scene
2. Tools → **Show Overlapped Entities**
3. Overlapping entities will be highlighted in green
4. Select the highlighted entities and remove duplicates
