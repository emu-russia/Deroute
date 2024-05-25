using System;
using System.Windows.Forms;
using System.Collections.Generic;

namespace DerouteSharp
{
	public partial class FormGetVerilogSettings : Form
	{
		private VerilogExportSettings export_settings = new VerilogExportSettings();

		public FormGetVerilogSettings(EntityBox ebox)
		{
			InitializeComponent();

			radioButton1.Checked = true;
			comboBox1.Enabled = false;

			// Get all Top-level layers

			List<Entity> top_layers = new List<Entity>();

			foreach (var entity in ebox.root.Children)
			{
				if (entity.Type == EntityType.Layer && entity.Label.Trim() != "")
				{
					top_layers.Add(entity);
				}
			}

			// Populate combobox

			comboBox1.Items.Clear();

			foreach (var layer in top_layers)
			{
				comboBox1.Items.Add(layer.Label);
			}

			if (comboBox1.Items.Count == 0)
			{
				radioButton2.Enabled = false;
				radioButton3.Enabled = false;
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			export_settings.mode = VerilogExportMode.Unknown;

			if (radioButton1.Checked)
			{
				export_settings.mode = VerilogExportMode.Everyhing;
			}
			else if (radioButton2.Checked)
			{
				export_settings.mode = VerilogExportMode.LayerByLayer;
			}
			else if (radioButton3.Checked)
			{
				export_settings.mode = VerilogExportMode.SpecifiedLayerOnly;
				export_settings.layer_name = comboBox1.Text;
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			Close();
		}

		public VerilogExportSettings GetVerilogExportSettings()
		{
			return export_settings;
		}

		public enum VerilogExportMode
		{
			Unknown,
			Everyhing,              // Export everything without taking hierarchy into account
			LayerByLayer,               // All layers from Root are counted as independent modules and exported individually in sequence
			SpecifiedLayerOnly,     // Only the specified Layer from Root is exported (similar to Everything, but the root is the specified Top layer)
		}

		public class VerilogExportSettings
		{
			public VerilogExportMode mode = VerilogExportMode.Unknown;
			public string layer_name = null;
		}

		private void radioButton3_CheckedChanged(object sender, EventArgs e)
		{
			comboBox1.Enabled = radioButton3.Checked;
		}
	}
}
