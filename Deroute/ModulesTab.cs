// Responsible for displaying and processing module list events.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DerouteSharp
{
	public partial class Form1 : Form
	{
		private void RepopulateModulesList()
		{
			if (!listViewModules.Visible)
				return;

			List<Entity> cells = entityBox1.GetCells();

			List<string> modules = new List<string>();
			foreach (Entity ent in cells)
			{
				if (ent.Module == null || ent.Module == "")
				{
					continue;
				}

				if (!modules.Contains(ent.Module))
				{
					modules.Add(ent.Module);
				}
			}

			listViewModules.Items.Clear();
			listViewModules.BeginUpdate();

			foreach (var module in modules)
			{
				listViewModules.Items.Add(module);
			}

			listViewModules.EndUpdate();
		}

		private void EntityBox1_OnModuleChanged(object sender, Entity entity, EventArgs e)
		{
			RepopulateModulesList();
		}

		private void listViewModules_DoubleClick(object sender, EventArgs e)
		{
			ListView listView = (ListView)sender;

			if (listView.SelectedItems.Count > 0)
			{
				ListViewItem selected = listView.SelectedItems[0];

				entityBox1.RemoveSelection();

				var cells = entityBox1.GetCells();
				foreach (var cell in cells)
				{
					if (cell.Module != null || cell.Module != "")
					{
						if (cell.Module == selected.Text)
						{
							cell.Selected = true;
						}
					}
				}

				entityBox1.Invalidate();

				// Switch to selection mode

				entityBox1.Mode = EntityMode.Selection;
				SelectionButtonHighlight();
			}
		}

		private void listViewModules_VisibleChanged(object sender, EventArgs e)
		{
			RepopulateModulesList();
		}
	}
}
