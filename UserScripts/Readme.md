# User Scripts

This folder contains custom scripts.

Anything that the Deroute GUI doesn't allow you to do can be done by scripts using the source XML.

## Cells.py

A script for automating work with cells. Allows you to arrange their ports, display their names and much more.

Information about cells (their types and arrangement in rows) is taken from the `Cells.json` file. Cells.json for VDP PSG is attached as an example.

It seems that the port layout is glitchy, I need to check.

Example use: `py -3 cells.py --xml PSG.xml --json cells.json --op count`
