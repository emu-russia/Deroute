# User Scripts

This folder contains custom scripts.

Anything that the Deroute GUI doesn't allow you to do can be done by scripts using the source XML.

## Cells.py

A script for automating work with cells. Allows you to arrange their ports, display their names and much more.

Information about cells (their types and arrangement in rows) is taken from the `Cells.json` file. Cells.json for VDP PSG is attached as an example.

It seems that the port layout is glitchy, I need to check.

## GetVerilog.cs

A script to convert XML to Verilog RTL. We get a kind of "disassembly" of the chip, with which you can work further as with the sources of HDL.

Principle of conversion:
- All cells (entities of `Cell` type) and custom blocks (entities of `Unit` type) become module instances. The direct definition of the cell/block logic is at the user's choice.
- All input/output/input vias within a cell/block become ports and wire connections are assigned by name (`.port_name(wire_xxx)`).
- If the port has no name, an error is output. All cell/block ports must have names.
- The instance name is taken from the `Label` property of the cell/block. The first word is the module name, the second word (if any) is the instance name. If there is no name, then a name of the form `g1`, `g2` and so on is generated. So it would be better to have a cell name too, to understand what kind of cell it is in the HDL listing.
- The ports for the top module are all input/output/inout vias NOT of cells. All ordinary vias become open-end wires and go into the HDL as is.
- Wires are obtained by combining segments by traverse. The wire name is taken by concatenating all segment names with a space, if the result is an empty string, then the wire name is generated as `w1`, `w2` and so on.

The script does not check connectivity and does not make any special checks at all. All errors can be checked later when using the generated HDL in your favorite CAD.
