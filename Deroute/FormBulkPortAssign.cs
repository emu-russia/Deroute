using System;
using System.Windows.Forms;

namespace DerouteSharp
{
	public partial class FormBulkPortAssign : Form
	{
		EntityBox parentBox;

		public FormBulkPortAssign(EntityBox ebox)
		{
			parentBox = ebox;
			InitializeComponent();
		}

		private void Process()
		{
			var cells = parentBox.GetCells();
			var ents = parentBox.GetEntities();

			bool nots = checkBox1.Checked;
			bool bufs = checkBox2.Checked;
			bool simple_comb = checkBox3.Checked;
			int naming_rule = 0;

			if (radioButton1.Checked)
			{
				naming_rule = 0;
			}
			else if (radioButton2.Checked)
			{
				naming_rule = 1;
			}
			else if (radioButton3.Checked)
			{
				naming_rule = 2;
			}

			foreach (var cell in cells)
			{
				var label = cell.Label.ToLower();
				bool process_cell =
					label.Contains("not") && nots ||
					label.Contains("inv") && nots ||        // Some chip researchers refer to inverters as INVs (inv)
					label.Contains("buf") && bufs ||
					label.Contains("nor") && simple_comb ||
					label.Contains("xor") && simple_comb ||
					label.Contains("xnor") && simple_comb ||
					label.Contains("or") && simple_comb ||
					label.Contains("nand") && simple_comb ||
					label.Contains("and") && simple_comb ;

				if (!process_cell)
				{
					continue;
				}

				int in_counter = 0;

				var ports = EntityBox.GetCellPorts(cell, ents);
				foreach (var port in ports)
				{
					if (port.Label != "")
					{
						continue;
					}

					if (port.Type == EntityType.ViasInput)
					{
						port.Label = port_index_to_name(naming_rule, in_counter);
						in_counter++;
					}

					if (port.Type == EntityType.ViasOutput)
					{
						switch (naming_rule)
						{
							case 0:
								port.Label = "x";
								break;
							case 1:
								port.Label = "z";
								break;
							case 2:
								port.Label = "o";
								break;
						}
					}
				}
			}
		}

		private string port_index_to_name (int method, int index)
		{
			string res = "";

			switch (method)
			{
				case 0:
				case 1:
					res = ToAlpha(index + 1);
					break;
				case 2:
					res = "i" + (index + 1).ToString();
					break;
			}

			return res;
		}

		private static string ToAlpha(int i)
		{
			string result = "";
			do
			{
				result = (char)((i - 1) % 26 + 'a') + result;
				i = (i - 1) / 26;
			} while (i > 0);
			return result;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Process();
			Close();
		}
	}
}
