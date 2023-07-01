using System.Collections.Generic;
using System.Windows.Forms;

//# Vias Syntax:
//# vias pattern_name, vias_name, offset_x, offset_y, type, reserved

namespace DerouteSharp
{
	public partial class FormCellPorts : Form
	{
		public FormCellPorts(Entity cell, List<Entity> ports)
		{
			InitializeComponent();
			textBox1.Text = PortsToText(cell, ports);
		}

		private string PortsToText (Entity cell, List<Entity> ports)
		{
			string text = "";

			foreach (var port in ports)
			{
				text += "vias ";
				text += cell.Label.Split(' ')[0] + ", ";
				text += port.Label + ", ";
				text += (port.LambdaX - cell.LambdaX).ToString("0.00") + ", ";
				text += (port.LambdaY - cell.LambdaY).ToString("0.00") + ", ";
				text += port.Type.ToString() + ", ";
				text += "0";
				text += "\r\n";
			}

			return text;
		}
	}
}
