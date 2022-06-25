# Custom EntityBox control

An EntityBox is the key component for all our utilities.

This control is an infinite-sized container which contains various "entities".

Exclusively for the purpose of reverse engineering the set of entities has been chosen to correspond to the main components of the chip/PCB architecture:
- Wires
- Vias
- Standard cells of different classes (Cells)

Example of a control with several wires, vias and one standard cell:

![image](https://user-images.githubusercontent.com/5828819/59331925-c1f23000-8cfd-11e9-9242-4db70b51e9be.png)

Entities are stored as a tree.

## Agreement by text.

In the following _italic_ will be marked with EntityBox properties.

## How to use the control in your project

To do this, add an EntityBox.csproj project to your solution and add it to your application's dependencies (Reference)

After that an EntityBox will appear in the controls panel:

![image](https://user-images.githubusercontent.com/5828819/59332346-a89db380-8cfe-11e9-96cc-4b33babedf0c.png)

## Lambda

All measurements inside EntityBox are made in units - lambda.

In digital electronics 1 lambda unit is taken to be the smallest part of a chip that the chip manufacturing process is technically capable of (usually the width of a transistor gate).

EntityBox contains a _Lambda_ property, which specifies the number of screen pixels corresponding to 1 lambda unit. Since this property is of float type, you can set a non-integer number of pixels. Although usually the value of _Lambda_ = 5.0

## Composition of the EntityBox assembly

The assembly includes a number of modules that contain public methods for working with the control:

- EntityBox: basic module with initialization
- AddEntity: adding entities
- CopyPaste: copy and paste entities
- DeleteEntity: deleting entities
- Drawing: Drawing the controls
- Images: working with images on image layers
- KeyInput: keyboard input
- Math: mathematical conversions
- MouseInput: input with mouse
- Selection: selection of entities
- Serialize: serialization (saving as Xml)
- Sort: sort entities by priority (TODO: some improvements are needed here after hierarchy tree was introduced)

And also a few helper utilities:
- Traverse: trace wires
- WireExtend: extend or shorten wires at the ends
- WireMerger: a collinear stapler
- WireRecognizer: wire recognizer (this module will be removed and replaced by a neural networks)
- WireRouter: router of wires between two vias

## EntityBox properties

The control properties are mainly for controlling the workspace:
- Zoom: sets the overall zoom of all layers
- ScrollX,ScrollY: shift the display of all layers relative to the start (scrolling)
- HideImage: hide all image layers
- HideVias: hide all vias
- HideWires: hide all wires
- HideCells: hide all standard cells
- HideGrid: hide the grid
- HideLambdaMetrics: hide the scale
- HideRegions: hide all regions
- BeaconImage: sets a picture of a lighthouse (Beacon)
- Image0,1,2: sets the picture for the corresponding picture layer
- ImageOpacity0,1,2: sets the transparency (0-255) for the corresponding picture layer
- SelectEntitiesAfterAdd: automatically selects the entities to be added
- Grayscale: sets all the pictures to black and white
- Lambda: the number of pixels corresponding to 1 lambda unit
- Mode: mode of operation (see below)
- ScrollImage0,1,2: offset of the image relative to the origin
- LockScroll0,1,2: disables scrolling of corresponding picture layers
- LockZoom0,1,2: disabling zoom of corresponding picture layers
- SelectionBoxColor: color of the selection frame
- SelectionColor: highlighting color of the selected entities

Properties that define the default colors for the respective entity types:
- ViasInputColor: default color for entities of type ViasInput
- ViasOutputColor: default color for entities of type ViasOutput
- ViasInoutColor: default color for entities of type ViasInout
- ViasConnectColor: default color for entities of type ViasConnect
- ViasFloatingColor: default color for entities of type ViasFloating
- ViasPowerColor: default color for entities of type ViasPower
- ViasGroundColor: default color for entities of type ViasGround
- WireInterconnectColor: default color for entities of type WireInterconnect
- WirePowerColor: default color for entities of type WirePower
- WireGroundColor: default color for entities of type WireGround
- CellNotColor: default color for entities of type CellNot
- CellBufferColor: default color for entities of type CellBuffer
- CellMuxColor: default color for entities of type CellMux
- CellLogicColor: default color for entities of type CellLogic
- CellAdderColor: default color for entities of the CellAdder type
- CellBusSuppColor: default color for entities of type CellBusSupp
- CellFlipFlopColor: default color for entities of type CellFlipFlop
- CellLatchColor: default color for entities of type CellLatch
- CellOtherColor: default color for entities of type CellOther
- UnitRegfileColor: default color for entities of type UnitRegfile
- UnitMemoryColor: default color for entities of type UnitMemory
- UnitCustomColor: default color for UnitCustom entities
- ViasOverrideColor: if you make a vias a color different from Override, it will have a different color
- WireOverrideColor: if you make the wire a color different from Override, oh it will have a different color
- CellOverrideColor: if the standard cell is set to a color different from Override, it will have a different color
- RegionOverrideColor: if you make the region a color different from Override, it will have a different color.

When entities are added, the Override color is assigned to them. If this color is left as it is, the entity color will be determined by the environment settings (color list above). If the entity color is changed to a color other than Override, the entity will have the specified color.

Example:

![image](https://user-images.githubusercontent.com/5828819/59355410-98ea9300-8d2f-11e9-8e0a-ebc7330f6b4f.png)
![image](https://user-images.githubusercontent.com/5828819/59355592-e830c380-8d2f-11e9-9db8-c7d3c54d729a.png)

The wire in the first image has the color Black, which corresponds to the setting _WireOverrideColor_ = Black, so it is displayed with the default color _WireInterconnectColor_ = Blue. In the second image, the wire is set to Orange, which does not match the _WireOverrideColor_ = Black setting, so the wire is displayed in orange.

Other properties:
- ViasPriority: the priority that is given to all created vias
- WirePriority: the priority that is given to all wires that are created
- CellPriority: priority given to all created standard cells
- BeaconPriority: the priority that is given to all created beacons
- RegionPriority: the priority that is given to all created regions
- AutoPriority: do not remember what for
- ViasOpacity: transparency for vias (0-255)
- WireOpacity: transparency for wires (0-255)
- CellOpacity: transparency for standard cells (0-255)
- ViasShape: selects one of the ways to draw the vias (square or round)

## Control events

The parent application can set handlers for events that occur in the control:
- OnScrollChanged: Called after moving the entity layer
- OnZoomChanged: Called after the entity layer zoom is changed 
- OnEntityCountChanged: Called after the total number of entities is changed
- OnEntityLabelEdit: Called after editing the text label of the entity
- OnEntitySelect: Called after selecting an entity
- OnEntityAdd: Called after adding an entity
- OnEntityRemove: Called after deleting the entity
- OnEntityScroll: Called after changing the position of the entity on the entity layer
- OnDestinationNodeChanged: Called after the target entity has changed (see below what a target entity is)
- OnFrameDone: Called after a control is rendered

This is done in the usual way by the Properties panel:

![image](https://user-images.githubusercontent.com/5828819/59333558-3e3a4280-8d01-11e9-9708-86a4e7afc124.png)

## Entity type list

Types are defined in EntityBox.cs (enum EntityType).

Special types:
- Root: the root of all entities
- Beacon: A beacon which lets you quickly navigate through the entity layer
- Region: arbitrary region
- Layer: an invisible entity used as a container for other child entities. Acts as a Photoshop "layer". You can't add layers directly yet, so to add a layer you can add e.g. Vias and change its type to Layer.

Vias:
- ViasOutput: output contact.
- ViasInout: Bidirectional contact
- ViasConnect: The vias that connects the two wires
- ViasFloating: Vias not connected to anything ("floating")
- ViasPower: Connects to power (1)
- ViasGround: Connects to the ground (0)

Wires:
- WireInterconnect: normal connecting wire
- WirePower: wire for power
- WireGround: wire for ground

Standard Cells:
- CellNot: inverter
- CellBuffer: amplifier buffer
- CellMux: multiplexer
- CellLogic: logic element (NAND, NOR, etc.)
- CellAdder: ALU element
- CellBusSupp: auxiliary cells of BusKeeper type
- CellFlipFlop: edge triggers (e.g. DFF)
- CellLatch: triggers by level ("latches")
- CellOther: other cells

Custom blocks:
- UnitRegfile: register file
- UnitMemory: memory or other memory blocks (for storing parameters, for example)
- UnitCustom: other blocks

## Entity properties and methods

All properties are collected in EntityBox.cs

- UserData: arbitrary data (int)
- Label: the text label that is shown next to the entity
- LabelAlignment: the text label alignment
- LambdaWidth: the width of the entity in lambda
- LambdaHeight: the height of the entity in lambda 
- LambdaX: X coordinate in the entity plane
- LambdaY: the Y coordinate on the entity plane
- LambdaEndX: final X coordinate (e.g. for Wire) on the plane of entities
- LambdaEndY: final Y coordinate (e.g. for Wire) on the entity plane
- Type: the entity type (EntityType)
- Selected: a notice that the entity is selected
- Priority: the priority of the entity. It affects the drawing sequence: entities with higher priority are drawn over entities with lower priority
- PathPoints: a set of coordinates for a Region entity
- Children: list of child entities
- Visible: defines whether the entity is visible. If an entity is invisible then all child entities are invisible too
- ColorOverride: used to specify an entity color different from the color of the entities
- FontOverride: used to specify a personal font for an entity label (Label) that differs from the general font of the entity
- WidthOverride: used to override the width of an entity other than the width of the given entity type

Auxiliary properties (read-only):
- WireLengthLambda: returns the wire length in lambdas
- WireTangent: returns the slope of the wire

Auxiliary methods:
- SetParentControl: allows you to set the parent controller. It is used for auto-updating control, after changing properties of the entity
- IsWire: check if the entity is one of the wire types
- IsVias: check if the entity is one of the Vias types
- IsCell: check if the entity is one of the standard cell types (Cell)
- IsRegion: check if the entity is a region

## EntityBox mode

The operating mode (_Mode_ property) defines the way the user interacts with the keyboard and mouse.

List of modes (enum EntityMode):
- Selection: the layer with the entities is selected to select and move them, as well as scrolling and zooming the workspace
- ImageLayer0: the layer of pictures 0 (the uppermost layer of pictures) is selected
- ImageLayer1: picture layer 1 is selected
- ImageLayer2: picture layer 2 is selected
- ViasInput: mode of adding ViasInput entities
- ViasOutput: mode of adding ViasOutput entities
- ViasInout: Entity addition mode ViasInout
- ViasConnect: ViasConnect entity addition mode
- ViasFloating: to add ViasFloating entities
- ViasPower: ViasPower entity addition mode
- ViasGround: ViasGround entity addition mode
- WireInterconnect: Entity adding mode WireInterconnect
- WirePower: Entity adding mode WirePower
- WireGround: Entity adding mode WireGround
- CellNot: Entity adding mode CellNot
- CellBuffer: Entity adding mode CellBuffer
- CellMux: CellMux entity addition mode
- CellLogic: CellLogic entity addition mode
- CellAdder: CellAdder entity addition mode
- CellBusSupp: Entity addition mode CellBusSupp
- CellFlipFlop: Entity adding mode CellFlipFlop
- CellLatch: Entity adding mode CellLatch
- CellOther: Entity adding mode CellOther
- UnitRegfile: entity addition mode UnitRegfile
- UnitMemory: entity addition mode UnitMemory
- UnitCustom: entity addition mode UnitCustom
- Beacon: Entity addition mode Beacon

## Layers

EntityBox contains several display layers
- Entity Layer. The entity layer is placed over the picture layers.
- 3 layers for storing pictures (chips/PCBs). Layer 0 is the uppermost of the picture layers.

You can change transparency (_ImageOpacity_) for picture layers.

It is also possible to disable moving (_LockScroll_) and zooming (_LockZoom_) for picture layers. This is done in order to prevent accidental movement and zooming.

## Control rendering features

The control intercepts the OnPaint method and uses double buffering to speed up rendering.

Rendering sequence:
- The background is filled with the standard color (BackColor)
- The picture layers are drawn in the order 2, 1, 0
- A grid is drawn
- Visible entities are drawn in order of priority. Selected entities are highlighted with _SelectionColor_
- If necessary, a selection frame and a wire drawing animation are drawn

## Keyboard and mouse control features

The control extends the OnKeyUp method and handles the following keystrokes:
- Delete: delete selected entities
- Escape: deselect all entities
- Home: move screen to origin
- Arrows: move selected entities by 0.1 lambda in specified direction

The controller extends the OnKeyDown method and handles the following keystrokes:
- Ctrl+C: copy selected entities
- Ctrl+V: paste copied entities

The control extends the OnMouseDown, OnMouseUp and OnMouseMove methods to implement user interaction:
- The right button is used to scroll the screen (scrolling)
- The left button is used to move entities in _Mode_ = _Selected_ or to add new entities, with _Mode_ determining the type of entities to be added
- The left button in _Mode_ = _Selected_ is used to frame the entities
- the left button in the _Mode_ = _ImageLayer_ mode moves the beginning of an image in the corresponding image layer

Since the left button has quite a few operations, sometimes there is an unintended shift in the image layer. The _LockScroll_ property was added for this, so that the user doesn't accidentally move the picture.

The control extends the OnMouseWheel method to change the overall zoom (_Zoom_) or the zoom of individual picture layers (_ZoomImage_). The zoom method is chosen by the current mode (_Mode_).

## EntityBox methods

- EntityBox: constructor, defines control properties by default
- GetEntities: get list of all entities (the tree is converted to a list)
- SetDestinationNode: define a target entity where all child entities will be added
- EnsureVisible: move scrolling so that the specified entity is visible
- GetViasCount: get the number of vias
- GetWireCount: get the number of wires
- GetCellCount: get the number of standard cells
- GetBeaconCount: get the number of beacons
- GetBeacons: get list of beacons
- ScrollToBeacon: set scroll to the specified beacon (for fast navigation)
- LabelEdited: must be called after changing the text label of an entity
- LambdaScale: lambda transformation (change the scale of all selected entities en masse)

## AddEntity methods

- AddVias: add a vias using the screen coordinates
- AddWire: add a wire using screen coordinates
- AddWireOnImage: add a wire using coordinates of an image 
- AddCell: add a standard cell using screen coordinates.
- AddRegion: add a region using screen coordinates
- DrawRegionBetweenSelectedViases: add region using coordinates of selected viases
- DrawWireBetweenSelectedViases: add one or more wires using the coordinates of the selected vias

## CopyPaste methods

- Copy: copy the selected entities to the internal list copied
- Paste: paste entities from the internal list copied to the sight coordinates (at `X` mark)

## DeleteEntity methods

- DeleteAllEntites: delete all entities
- DeleteSelected: delete the selected entities
- RemoveSmallWires: remove wires shorter than a specified size
- RemoveNonOrthogonalWires: remove non-orthogonal wires
- RemoveEntity: remove a specified entity

## Drawing methods

Does not provide public methods.

## Images methods

- ResizeImage: resize the image
- SaveSceneAsImage: save the scene as an image to a specified file
- ColorToGrayscale: obtain a black and white image
- LoadImage: load the image in the current picture layer (the layer is chosen by _Mode_)
- UnloadImage: unload the image from the current picture layer (the _Mode_ layer is chosen). Forced to call the garbage collector (`GC.Collect`)

## KeyInput methods

Does not provide public methods.

## Math methods

- ScreenToLambda: convert screen coordinates to lambda coordinates
- ImageToLambda: convert image coordinates to lambda coordinates
- LambdaToScreen: transform the lambda coordinates into screen coordinates
- LambdaToImage: transform the lambda coordinates into picture coordinates
- LineIntersectsRect: define line and rectangle intersection (True/False)

## MouseInput methods

- GetLastRightMouseButton: get the last coordinates after the right mouse button was clicked (the coordinates of the sight)
- GetDragDistance: get the distance to which the entities were moved with the mouse

## Selection methods

- RemoveSelection: remove selection from all entities
- AssociateSelectionPropertyGrid: TBD
- GetSelected: get a list of selected entities
- GetSelectedVias: get a list of selected vias
- GetSelectedWires: Get a list of selected wires
- GetLastSelected: get the last selected entity
- SelectEntity: select an entity
- SelectAll: select all entities

## Serialize methods

- Serialize: save all entities to an Xml file
- Unserialize: load entities from an Xml file

## Sort methods

- SortEntities: sort entities in ascending priority order
- SortEntitiesReverse: sort entities in descending priority order

## Auxiliary utilities

### Traverse.

- TraversalSelection: highlights all wires that connect to each other and to standard cells.

The TierMax parameter allows you to set how deep to trace. If you specify a value of 1, the tracing stops at the first standard cell it has reached. The greater TierMax parameter value is, the deeper the tracing can be performed.

### WireExtend

- WireExtendHead: lengthen the beginning of the wire
- WireExtendTail: lengthen end of wire
- WireShortenHead: shorten the beginning of the wire
- WireShortenTail: shorten end of wire

Lengthening/shortening is done by a small lambda value.

### WireMerger.

- MergeSelectedWires: merges selected wires, making several wires into one

### WireRecognizer

This module will be replaced by neural networks.

You can read how the old dumb algorithm works here:
https://github.com/ogamespec/psxdev/blob/master/docs/wire_recognition.pdf

### WireRouter

- Route: conducts wires between two specified vias and a list of "walls" entities.

The AStar algorithm is used to lay wires.

![image](https://user-images.githubusercontent.com/5828819/59359508-21b8fd00-8d37-11e9-8b8e-3c8cc1743837.png)

Translated with DeepL.
