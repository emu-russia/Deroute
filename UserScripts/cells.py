"""
	A script for automatic operations with cells.

	The netlist assumes horizontal cell placement.

	TBD: Add vertical placement option (PSXCPU).
	
"""

import os
import sys
import argparse
import json
import xml.etree.ElementTree as ET


"""
	Processing of deserialized XML by rows of cells.
"""
def ProcessCells (op, cells, netlist):
	# Для всех ячеек:

	TopMin = 0
	row_num = 0
	total_cells = 0

	found = True
	while found:
		row = []
		count = 0
		found = False

		# Find all cells in the netlist y > TopMin and:
		# - Get the topmost cell (TopCell) - this will be the beginning of the row (TopY)
		# - Get its height and calculate the gap for the next run (let's do it carefully - height/2)
		# - If something was found - this will be the next row, process it according to the mass operation. If not found - exit.

		TopY = float("inf")
		TopCell = None

		for entity in netlist:
			entityType = entity.find('Type').text
			y = float(entity.find('LambdaY').text)
			if entityType.startswith('Cell'):
				if y < TopY and y > TopMin:
					TopY = y
					TopCell = entity
					found = True

		if not found:
			break

		height = float(TopCell.find('LambdaHeight').text)
		Gap = height / 2

		# Select cells y >= TopMin && y < (TopY+height+Gap)

		for entity in netlist:
			entityType = entity.find('Type').text
			y = float(entity.find('LambdaY').text)
			if entityType.startswith('Cell'):
				if y >= TopMin and y < (TopY+height+Gap):
					row.append(entity)
					count = count + 1

		# Sort by X in ascending order (from left to right)

		row = sorted(row, key=lambda child: float(child.find('LambdaX').text) )

		# Perform an operation on a row of cells

		if count:
			if op == 'add_names':
				AddNames (cells, row, row_num)
			elif op == 'rem_names':
				RemNames (cells, row, row_num)
			elif op == 'classify':
				Classify (cells, row, row_num)
			elif op == 'unclassify':
				Unclassify (cells, row, row_num)
			elif op == 'add_ports':
				AddPorts (cells, row, row_num, netlist)
			elif op == 'rem_ports':
				RemPorts (cells, row, row_num, netlist)
			elif op == 'list_ports':
				ListPorts (cells, row, row_num, netlist)
			elif op == 'resize':
				Resize (cells, row, row_num)
			elif op == 'count':
				total_cells = total_cells + CountByRow (cells, row, row_num)

		TopMin = TopY + height
		row_num = row_num + 1

	if op == 'count':
		print (f"Total cells: {total_cells}")
"""
	ProcessCells end.
"""


"""
	A group operation on self-alignment of cells (dihedral group D2).
	The values in the `placement` section mean the following: 
	- e: normal position of the cell, GND on the bottom 
	- r: cell rotated 180 degrees, GND on top
	- f: cell flipped from left to right (horizontal), GND location is indicated by the first operation (e/r)
"""
def Vierergruppe(pos, w, h, word):
	res = pos
	for op in word:
		if op == 'e':
			continue
		if op == 'r':
			res[0] = w - res[0]
			res[1] = h - res[1]
		if op == 'f':
			res[0] = w - res[0]
	return res


"""
	Find all the ports in the specified area.
"""
def GetPorts(netlist, x, y, w, h):
	ports = []
	for entity in netlist:
		entityType = entity.find('Type').text
		ex = float(entity.find('LambdaX').text)
		ey = float(entity.find('LambdaY').text)
		if ex >= x and ex < (x + w) and ey >= y and ey < (y + h):
			if entityType == 'ViasInput' or entityType == 'ViasOutput' or entityType == 'ViasInout':
				ports.append(entity)
	return ports


"""
	Add vias to the netlist.
"""
def AddVias(netlist, name, x, y, type):
	vias_type = "ViasConnect"
	if type == "input":
		vias_type = "ViasInput"
	elif type == "output":
		vias_type = "ViasOutput"
	elif type == "inout":
		vias_type = "ViasInout" 
	entity = ET.Element("Entity")
	ET.SubElement(entity, 'Type').text = vias_type
	ET.SubElement(entity, 'Label').text = name
	ET.SubElement(entity, 'LambdaX').text = str(x)
	ET.SubElement(entity, 'LambdaY').text = str(y)
	ET.SubElement(entity, 'ColorOverride').text = "Black"
	ET.SubElement(entity, 'LabelAlignment').text = "GlobalSettings"
	ET.SubElement(entity, 'Visible').text = "true"
	ET.SubElement(entity, 'Priority').text = str(3)
	netlist.append (entity)


def AddNames(cells, row, row_num):
	row_name = cells['map']['row_names'][row_num]
	cell_num = 0
	for entity in row:
		cell_name = cells['map']['rows'][row_num][cell_num]
		entity.find('Label').text = row_name + str(cell_num) + "-" + cell_name
		cell_num = cell_num + 1


def RemNames(cells, row, row_num):
	for entity in row:
		entity.find('Label').text = None


def Classify(cells, row, row_num):
	cell_num = 0
	for entity in row:
		cell_name = cells['map']['rows'][row_num][cell_num]
		entity.find('Type').text = cells['cells'][cell_name]['type']
		cell_num = cell_num + 1


def Unclassify(cells, row, row_num):
	for entity in row:
		entity.find('Type').text = 'CellOther'


def AddPorts(cells, row, row_num, netlist):
	cell_num = 0
	for entity in row:
		cell_name = cells['map']['rows'][row_num][cell_num]
		ex = float(entity.find('LambdaX').text)
		ey = float(entity.find('LambdaY').text)
		ew = float(entity.find('LambdaWidth').text)
		eh = float(entity.find('LambdaHeight').text)
		if 'ports' in cells['cells'][cell_name]: 
			for port in cells['cells'][cell_name]['ports']:
				pos = [port['x'], port['y']]
				word = cells['map']['placement'][row_num][cell_num]
				pos = Vierergruppe (pos, ew, eh, word)
				AddVias(netlist, port['name'], ex + pos[0], ey + pos[1], port['type'])
		cell_num = cell_num + 1


def RemPorts(cells, row, row_num, netlist):
	cell_num = 0
	for entity in row:
		ex = float(entity.find('LambdaX').text)
		ey = float(entity.find('LambdaY').text)
		ew = float(entity.find('LambdaWidth').text)
		eh = float(entity.find('LambdaHeight').text)
		ports = GetPorts(netlist, ex, ey, ew, eh)
		for port in ports:
			netlist.remove(port)
		cell_num = cell_num + 1


def ListPorts(cells, row, row_num, netlist):
	cell_num = 0
	for entity in row:
		ex = float(entity.find('LambdaX').text)
		ey = float(entity.find('LambdaY').text)
		ew = float(entity.find('LambdaWidth').text)
		eh = float(entity.find('LambdaHeight').text)
		ports = GetPorts(netlist, ex, ey, ew, eh)
		if len(ports) == 0:
			continue
		print(entity.find('Label').text + " ports:")
		for port in ports:
			ptype = port.find('Type').text
			pname = port.find('Label').text
			px = float(port.find('LambdaX').text)
			py = float(port.find('LambdaY').text)
			pos = [px, py]
			word = cells['map']['placement'][row_num][cell_num]
			pos = Vierergruppe (pos, ew, eh, word[::-1])
			print (f"type: {ptype}, name: {pname}, x: {pos[0] - ex}, y: {pos[1] - ey}")
		cell_num = cell_num + 1


def Resize(cells, row, row_num):
	cell_num = 0
	for entity in row:
		cell_name = cells['map']['rows'][row_num][cell_num]
		cell = cells['cells'][cell_name]
		if 'width' in cell:
			entity.find('LambdaWidth').text = str(cell['width'])
		if 'height' in cell:
			entity.find('LambdaHeight').text = str(cell['height'])
		cell_num = cell_num + 1


def CountByRow(cells, row, row_num):
	count = len(row)
	print ("row " + str(row_num) + ", cells: " + str(count))
	return count


"""
	Deserialize JSON and XML. Make processing of the selected operation. Serialize the XML back (output result)
"""
def Main(args):
	with open(args.json_file, mode='r', encoding='UTF8') as f:
		text = f.read()
		cells = json.loads(text)
	netlist = ET.parse(args.xml_file)
	ProcessCells (args.operation, cells, netlist.getroot())
	if not (args.operation == "count" or args.operation == "list_ports"):
		out_xml = open(args.xml_file, "wb")
		xml_text = ET.tostring(netlist.getroot(), method='xml')
		out_xml.write(xml_text)
		out_xml.close()


if __name__ == '__main__':
	parser = argparse.ArgumentParser(description='To continue, specify one of the operations: count (count cells by row), add_names (add names), rem_names (remove names), classify (set types), unclassify (remove types), add_ports (add ports), rem_ports (remove ports), list_ports (display ports), resize (set sizes)')
	parser.add_argument('--op', dest='operation', help='Operation on cells')
	parser.add_argument('--json', dest='json_file', help='JSON with cell definitions')
	parser.add_argument('--xml', dest='xml_file', help='XML file from Deroute with netlist (BE CHANGED for all operations except `count` and `list_ports`')
	parser.add_argument('--lambda', dest='lambda', default=1.0, help='Topological cell factor (scale), in lambda. Default is 1.0')
	Main(parser.parse_args())
