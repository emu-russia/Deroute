# Deroute User Manual

## Table of Contents

1. [Overview](#overview)
2. [Getting Started](#getting-started)
3. [Main Workspace](#main-workspace)
   - [Navigation](#navigation)
   - [Zoom](#zoom)
   - [Grid and Scale](#grid-and-scale)
4. [Entity Types](#entity-types)
   - [Vias](#vias)
   - [Wires](#wires)
   - [Standard Cells](#standard-cells)
   - [Custom Units](#custom-units)
   - [Special Entities](#special-entities)
5. [Creating Entities](#creating-entities)
6. [Selection](#selection)
7. [Entity Layers](#entity-layers)
8. [Hierarchy Tree View](#hierarchy-tree-view)
9. [Entity Properties](#entity-properties)
10. [Wire Manipulation](#wire-manipulation)
    - [Extend/Shorten Wires](#extendshorten-wires)
    - [Merge Collinear Wires](#merge-collinear-wires)
    - [Remove Small Wires](#remove-small-wires)
    - [Remove Non-Orthogonal Wires](#remove-non-orthogonal-wires)
    - [A* Wire Routing](#a-wire-routing)
11. [Traverse (Selection Expansion)](#traverse-selection-expansion)
12. [Lambda Scale Transform](#lambda-scale-transform)
13. [Copy and Paste](#copy-and-paste)
14. [Entity Locator](#entity-locator)
15. [Bulk Rename](#bulk-rename)
16. [Traverse Black List](#traverse-black-list)
17. [Cell Library Management](#cell-library-management)
18. [Overlap Detection](#overlap-detection)
19. [Verilog Export](#verilog-export)
20. [Minimap](#minimap)
21. [Settings](#settings)
21. [Key Bindings](#key-bindings)
22. [Saving and Loading](#saving-and-loading)

---

## Overview

Deroute is a utility for **reverse engineering of integrated circuits (ICs) and printed circuit boards (PCBs)**, built on **.NET Framework 4.8**. Because of this, it can run not only on Windows but also on other platforms with .NET support.

Deroute provides a visual editor where users can map out the topology of chip/PCB layouts using entities like wires, vias, and standard cells -- essentially "disassembling" a chip's internal structure into a workable HDL (Hardware Description Language) representation.

The tool allows you to:
- Load a background image (chip/PCB photo) via File -> Load Image
- Draw and annotate wires, vias, and standard cells on top of that image
- Traverse and trace connected wire paths
- Export the captured topology as **Verilog RTL code**

> **Note:** This manual is being expanded over time. Sections not yet covered are generally self-descriptive -- feel free to explore the menus and buttons.

---

## Getting Started

1. Launch Deroute
2. Load a background image (chip/PCB photo) via File -> Load Image
3. Start creating entities using the mode selector or key bindings
4. Use the hierarchy tree view to manage your entity structure

![main_window](imgstore/main_window.png)

---

## Main Workspace

### Navigation

The main workspace is an **infinite canvas** where all entities are placed. You can navigate using:

- **Right mouse button + drag**: Pan the view (scroll the canvas)
- **Mouse wheel**: Zoom in/out
- **Home key**: Reset view to origin (0, 0)

### Zoom

Zoom affects all layers equally. The zoom level can be adjusted via the mouse wheel or programmatically through the `Zoom` property.

### Grid and Scale

Deroute uses a **Lambda coordinate system** -- a raster-independent coordinate system where:

- **1 Lambda** = the smallest feature size the chip manufacturing process can produce (typically the width of a transistor gate)
- The `Lambda` property defines how many screen pixels correspond to 1 lambda unit
- The grid is drawn in lambda units and can be toggled on/off

> **Tip:** A typical `Lambda` value is 5.0, but this can be adjusted based on your design's scale.

![lambda_scale](imgstore/lambda_scale.png)

---

## Entity Types

Entities are the building blocks of your design. They are organized into the following categories:

### Vias

Vias represent contact points on the chip/PCB:

| Type | Description |
|------|-------------|
| `ViasInput` | Input contact |
| `ViasOutput` | Output contact |
| `ViasInout` | Bidirectional contact |
| `ViasConnect` | Via that connects two wires |
| `ViasFloating` | Unconnected ("floating") via |
| `ViasPower` | Connected to power supply (logic 1) |
| `ViasGround` | Connected to ground (logic 0) |

### Wires

Wires represent electrical connections:

| Type | Description |
|------|-------------|
| `WireInterconnect` | Standard connecting wire |
| `WirePower` | Power supply wire |
| `WireGround` | Ground wire |

### Standard Cells

Standard cells represent logic elements:

| Type | Description |
|------|-------------|
| `CellNot` | Inverter (NOT gate) |
| `CellBuffer` | Buffer (non-inverting amplifier) |
| `CellMux` | Multiplexer |
| `CellLogic` | Logic element (NAND, NOR, XOR, etc.) |
| `CellAdder` | ALU element (adder/subtractor) |
| `CellBusSupp` | BusKeeper (bus keeper) |
| `CellFlipFlop` | Edge-triggered flip-flop (e.g., DFF) |
| `CellLatch` | Level-triggered latch |
| `CellOther` | Other/undefined cell type |

### Custom Units

Custom blocks for grouping functionality:

| Type | Description |
|------|-------------|
| `UnitRegfile` | Register file |
| `UnitMemory` | Memory block |
| `UnitCustom` | Other custom block |

### Special Entities

| Type | Description |
|------|-------------|
| `Beacon` | Navigation marker for quick jumping to locations |
| `Region` | Arbitrary closed shape (used for grouping/annotation) |
| `Layer` | Invisible container for child entities (like a Photoshop layer) |
| `Root` | Root of the entity hierarchy tree |

---

## Creating Entities

Entities are created by selecting the appropriate **mode** from the mode selector, then clicking on the canvas:

1. Select the desired entity type mode (e.g., `ViasInput`, `WireInterconnect`, `CellNot`, etc.)
2. Click on the canvas to place the entity
3. For wires: drawing is done with the mouse. If you are in `ViasConnect` mode, pressing **Shift + Left Click** draws an additional wire segment from the last selected via
4. For regions: click on existing vias to define the region boundary

You can also:
- **Draw a region between selected vias**: Use the "Draw Region Between Selected Vias" command
- **Draw wires between selected vias**: Use the "Draw Wire Between Selected Vias" command

![creating_entities](imgstore/creating_entities.png)

---

## Selection

Deroute supports multiple selection modes:

- **Single selection**: Click on an entity
- **Box selection**: Click and drag in Selection mode to框选 multiple entities
- **Select all of type**: Select all entities of a specific type at once
- **Traverse selection**: Expand selection along connected wires (see [Traverse](#traverse-selection-expansion))

Selected entities are highlighted with the `SelectionColor` (default: green).

---

## Entity Layers

In Deroute, entities can be grouped into **layers** (groups) to organize the project:

### Grouping Entities

1. Select the entities you want to group into a layer
2. Press **Group**
3. The hierarchy is modified -- the parent of all selected entities becomes the new layer
4. The layer is created at the insertion node position (the current entity for adding new entities, shown in the status bar)

### Ungrouping

1. Select the layer
2. Press **Ungroup**
3. The layer is deleted, and the layer's parent becomes the parent of the layer's entities

### Creating an Empty Layer

Creating an empty layer is also available via a button (previously in the Edit menu).

> **Tip:** Layers help organize large projects with thousands of entities.

![entity_layers](imgstore/entity_layers.png)

---

## Hierarchy Tree View

The **Hierarchy Tree View** displays all entities in a tree structure:

- **Root** is at the top of the tree
- Child entities are nested under their parent
- Each entity has a **checkbox** to toggle visibility
- **Drag and drop** to reparent entities (change their parent)
- Use **Ctrl+Up/Down** to reorder sibling entities
- **Double-click** an entity to navigate to it in the main view

The tree view is useful for:
- Managing visibility of large designs
- Understanding the hierarchy of your entities
- Quick navigation to specific entities
- Reorganizing the entity structure

![hierarchy_tree](imgstore/hierarchy_tree.png)

---

## Entity Properties

Each entity has properties that can be viewed and edited in the **PropertyGrid** panel:

### Common Properties

| Property | Description |
|----------|-------------|
| `Label` | Text label displayed next to the entity |
| `LabelAlignment` | Alignment of the text label |
| `Type` | Entity type (EntityType enum) |
| `Selected` | Whether the entity is selected |
| `Visible` | Whether the entity is visible |
| `Priority` | Drawing priority (higher = drawn on top) |
| `ColorOverride` | Custom color (if different from default for this type) |
| `FontOverride` | Custom font for the entity's label |
| `UserData` | Arbitrary integer data field |

### Coordinate Properties

| Property | Description |
|----------|-------------|
| `LambdaX` | X coordinate in lambda units |
| `LambdaY` | Y coordinate in lambda units |
| `LambdaWidth` | Width in lambda units |
| `LambdaHeight` | Height in lambda units |
| `LambdaEndX` | End X coordinate (for wires) |
| `LambdaEndY` | End Y coordinate (for wires) |

### Read-only Properties

| Property | Description |
|----------|-------------|
| `WireLengthLambda` | Length of the wire in lambdas (wires only) |
| `WireTangent` | Slope of the wire (wires only) |

### Editing Labels

Double-click on an entity's label to edit it inline. After editing, the control automatically updates.

---

## Wire Manipulation

### Extend/Shorten Wires

You can extend or shorten the ends of wire entities:

- **Extend Head**: Lengthen the beginning of the wire
- **Extend Tail**: Lengthen the end of the wire
- **Shorten Head**: Shorten the beginning of the wire
- **Shorten Tail**: Shorten the end of the wire

Each operation extends/shortens by a small lambda increment.

### Merge Collinear Wires

Select multiple collinear wires and use **Merge Selected Wires** to combine them into a single wire. This is useful for cleaning up designs with fragmented wire segments.

### Remove Small Wires

Use **Remove Small Wires** to delete all wires shorter than a specified threshold. This helps clean up noise from imported or auto-generated designs.

### Remove Non-Orthogonal Wires

Use **Remove Non-Orthogonal Wires** to delete any wires that are not perfectly horizontal or vertical. Real PCB/chip wiring is typically orthogonal.

### A* Wire Routing

Deroute includes an **A* pathfinding algorithm** for automatic wire routing:

1. Select two vias that you want to connect
2. Run the **Wire Router** command
3. The algorithm will find a path between the vias, avoiding cells and regions as "walls"

![wire_routing](imgstore/wire_routing.png)

> **Note:** Cells and regions act as obstacles for the router. Use them strategically to guide wire placement.

---

## Traverse (Selection Expansion)

**Traverse** expands the current selection along connected wires and through standard cells:

1. Select one or more entities
2. Run the Traverse command (or use key binding F10-F12)
3. The selection expands to include all connected entities

### TierMax Parameter

The `TierMax` parameter controls how deep the traversal goes:

| TierMax | Behavior |
|---------|----------|
| 1 | Stop at the first standard cell reached |
| 2 | Traverse through one level of cells |
| 3-5 | Traverse deeper through the circuit |

Higher values allow traversal through more levels of the circuit, but may select unintended entities.

### How Traverse Works

- Traces along connected wire segments
- Passes through standard cells to the other side
- Stops at cells defined in the `TraverseBlackList` (see below)

---

## Lambda Scale Transform

The **Lambda Scale Transform** allows you to scale selected entities by a factor:

1. Select the entities to scale
2. Open the Lambda Scale dialog (Ctrl+T)
3. Enter the scale factor (e.g., 2.0 to double the size)
4. Apply the transformation

This is useful for:
- Adjusting entity sizes to match a different lambda scale
- Enlarging small details for easier editing
- Compressing large designs to fit a smaller area

---

## Copy and Paste

Deroute has an **internal clipboard** for entities:

- **Ctrl+C**: Copy selected entities to the internal clipboard
- **Ctrl+V**: Paste entities from the clipboard at the crosshair position

Pasted entities are placed at the current cursor (crosshair) coordinates.

---

## Entity Locator

The **Entity Locator** provides a quick way to find and navigate to specific entities:

**Menu:** Tools -> Entity Locator

![entity_locator](imgstore/entity_locator.png)

### Usage

1. Open Entity Locator from the Tools menu
2. Enter part of the entity name (Label) -- leave empty to ignore name filtering
3. Select entity types to include in the search:
   - Specific types (e.g., `ViasInout`)
   - "All Vias"
   - "All Wires"
   - "All Cells"
   - "All Units"
4. The filtered list appears
5. **Double-click** an entity in the list to navigate to it in the main view

---

## Bulk Rename

The **Bulk Rename** tool allows batch renaming of entities:

**Menu:** Tools -> Bulk Rename

![bulk_rename](imgstore/bulk_rename.png)

### Usage

1. Open Bulk Rename from the Tools menu
2. Select the entity type to rename
3. Choose the renaming mode:
   - **Append**: Add a suffix to existing names
   - **Replace**: Replace existing names with a pattern
   - **Keep first word**: When replacing, keep the first word of the original name
4. Enter the pattern/append text
5. Apply to rename all matching entities

---

## Traverse Black List

The `TraverseBlackList` property on each entity defines which entity types the traverse operation should **skip**:

### Configuration

1. Select an entity in the editor
2. In the PropertyGrid, find the `TraverseBlackList` property
3. Click the `...` button to open the configuration dialog
4. Add or remove entity types from the blacklist
5. All entity types are available except `Root`

### Use Cases

- Prevent wires from connecting to power/ground vias during traverse
- Block traversal through specific cell types
- Control which entity types can be reached during selection expansion

### Example

If you add `ViasPower` to a wire's `TraverseBlackList`, the wire will **not** connect to power vias during traverse operations.

---

## Cell Library Management

Deroute includes a **Cell Library Editor** for creating and managing cell definitions:

### Opening the Cell Library

**Menu:** Tools -> Cell Library (or similar)

![cell_library](imgstore/cell_library.png)

### Creating Cells

1. Open the Cell Library editor
2. Create a new cell definition
3. Set the cell's bitmap image (icon)
4. Define the cell's entities (vias, wires, standard cells)
5. Save the cell library as an XML file (supports .xmlz compression)

### Using Cells

- **Double-click** a cell in the library to place it at the crosshair position
- Cells can be edited and repositioned after placement
- Cell libraries can be saved and loaded from XML files

### Cell Library Format

Cells are stored as XML with:
- Bitmap image data
- Entity collection (all child entities)
- Port definitions

---

## Overlap Detection

Deroute can detect and highlight **overlapping entities**:

**Menu:** Tools -> Show Overlapped Entities

### Types of Detected Overlaps

| Overlap Type | Description |
|--------------|-------------|
| Cell ↔ Cell | Two cells overlapping (with polygon support) |
| Region ↔ Region | Two regions overlapping |
| Cell ↔ Region | Cell inside or overlapping a region |
| Wire ↔ Wire | Two wires crossing |
| Via ↔ Via | Two vias at the same position |
| Cell/Region contains Via | Via inside a cell or region |
| Wire intersects Cell/Region | Wire crossing a cell or region boundary |
| Via on Wire | Via placed on top of a wire |

### Usage

1. Load your XML scene
2. Go to **Tools -> Show Overlapped Entities**
3. All overlapping entities are highlighted in **green** (marked as `Selected = true`)
4. Review the highlighted entities
5. Select and remove duplicates or fix overlaps manually

---

## Verilog Export

Deroute can export your design as **Verilog RTL code** -- a form of "disassembly" of the chip:

**Menu:** Tools -> Export to Verilog

### Conversion Principles

| Source | Verilog Output |
|--------|----------------|
| Cells (`Cell` type) | Module instances |
| Units (`Unit` type) | Module instances |
| Vias within cells/units | Ports with named connections (`.port_name(wire_xxx)`) |
| Top-level vias (not in cells) | Ports for the top module |
| Ordinary vias | Open-end wires |
| Connected wires | Net names (segments joined by traverse) |
| `ViasPower` | `1'b1` constant |
| `ViasGround` | `1'b0` constant |

### Instance Naming

- The instance name comes from the cell's `Label` property
- First word = module name, second word (if any) = instance name
- If no name is provided, names like `g1`, `g2`, etc. are generated
- **Recommendation:** Always name your cells for clarity in the HDL output

### Wire Naming

- Wire names are formed by concatenating all segment names with underscores (`_`)
- If the result is empty, names like `w1`, `w2`, etc. are generated

### Validation

The Verilog export includes **sanity checks**:

- **Unnamed ports**: Errors if any cell/block port has no name
- **Conflicting drives**: Checks for multiple drivers on the same net
- **Floating wires**: Reports wires with no connections

> **Note:** The script does not perform full connectivity checking. All errors should be verified in your preferred CAD tool after export.

---

## Minimap

The **Minimap** is a viewport overview widget displayed in the corner of the EntityBox canvas. It shows a scaled-down version of the loaded background image (or tilemap) with a rectangle indicating the current visible area.

### Enabling the Minimap

The Minimap can be enabled via the **Settings dialog** or programmatically through the `EntityBoxProperties`:

**Settings dialog:**
- **Menu:** Tools -> Settings

**EntityBox properties:**
- `MinimapEnabled` — Enable/disable the minimap
- `MinimapSizePercent` — Size of the minimap as a fraction of the EntityBox width (0.0 to 1.0, default: 0.15)
- `MinimapPosition` — Position in the viewport (`TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`, default: `TopRight`)
- `MinimapViewportColor` — Color of the viewport rectangle on the minimap (default: Red)
- `MinimapViewportOpacity` — Opacity of the viewport rectangle (0-255, default: 128)
- `MinimapMinSize` — Minimum size of the minimap in pixels (default: 50)

### Image Mode

When a background image is loaded, the Minimap displays a scaled thumbnail of the entire image. The current viewport is shown as a colored rectangle overlay.

### Tilemap Mode

When `OptimizeTilemap` is enabled, the Minimap switches to tilemap mode, rendering the tilemap at a reduced scale instead of the full image. This is useful for tile-based designs.

### Click Navigation

The Minimap supports **click navigation** — clicking on the minimap area will pan the main view to the clicked location, allowing quick navigation to distant parts of the canvas.

---

## Settings

Deroute's settings can be configured via the **Settings dialog**:

**Menu:** Tools -> Settings

### Configurable Options

| Setting | Description |
|---------|-------------|
| Grid size | Size of the display grid |
| Snap to grid | Whether entities snap to grid points |
| Default colors | Default colors for each entity type |
| Lambda value | Pixels per lambda unit |
| Opacity | Default opacity for vias, wires, and cells |
| Via shape | Square or round vias |

### Saving Settings

Settings can be saved to and loaded from an XML file, allowing you to maintain different configuration profiles.

---

## Key Bindings

Deroute supports the following **keyboard shortcuts**:

| Key | Action |
|-----|--------|
| **F1** | Selection mode |
| **F2** | Via Connect mode |
| **F3** | Wire Interconnect mode |
| **F10** | Traverse tier 1 |
| **F11** | Traverse tier 2 |
| **F12** | Traverse tier 3 |
| **Ctrl+T** | Lambda Scale Transform |
| **Ctrl+A** | Select all entities |
| **Ctrl+C** | Copy selected entities |
| **Ctrl+V** | Paste entities |
| **Ctrl+S** | Save scene |
| **Ctrl+R** | Rotate cell 90 degrees |
| **Ctrl+F** | Flip cell horizontally |
| **Delete** | Delete selected entities |
| **Escape** | Deselect all entities |
| **Home** | Reset view to origin |
| **Arrow keys** | Move selected entities by 0.1 lambda |
| **Ctrl+Up/Down** | Reorder siblings in tree view |

---

## Saving and Loading

### Saving Entities

Deroute entities are saved as **XML files**:

1. **Menu:** File -> Save entities (Ctrl+S)
2. Entities can also be saved as **compressed XML** (`.xmlz`) to reduce file size

### Loading Entities

1. **Menu:** File -> Add entities
2. Select an XML or .xmlz file
3. Entities are loaded and added to the current project

### Cell Library Files

Cell libraries are also saved as XML files and can be loaded/saved independently from entities.

---

## Appendix: EntityBox Control

Deroute is built on the **EntityBox** control -- a reusable infinite canvas component. For developers interested in the underlying control, see:

- [EntityBox README](../EntityBox/Readme.md)
- [EntityBox README (Russian)](../EntityBox/ReadmeRus.md)
