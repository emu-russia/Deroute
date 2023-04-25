# Deroute

Tool for untangling wires.

![sample](/Deroute/Build/sample.jpg)

It can be used for reverse engineering of integrated circuits and printed circuit boards.

## Features

At the center of the tool is a custom control called the EntityBox. Its task is to store and display a set of "Entities".

Entities are any connected elements that are used to build the topology of integrated circuits and printed circuit boards: wires, connecting contacts (vias), standard cells, register and memory blocks, other ICs and connectors.

## Coordinate system

Deroute operates in two coordinate systems:
- To store vector data, raster-independent Lambda coordinates are used. 1 Lambda = n pixels (set in settings)
- To display vector data, Lambda coordinates are converted to screen coordinates, taking into account scrolling and zoom.

## Output

The collection of entities can be saved and loaded in XML format.
