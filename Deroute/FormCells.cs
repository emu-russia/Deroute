using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DerouteSharp
{
	public partial class FormCells : Form
	{
		private bool Saved = true;

		public FormCells()
		{
			InitializeComponent();
		}

		private void FormCells_Load(object sender, EventArgs e)
		{
			button1.Enabled = !Saved;
			entityBox1.OnEntityAdd += EntityBox1_OnEntityAdd;
			entityBox1.OnEntityRemove += EntityBox1_OnEntityRemove;
		}

		private void EntityBox1_OnEntityRemove(object sender, Entity entity, EventArgs e)
		{
			Saved = false;
			button1.Enabled = !Saved;
		}

		private void EntityBox1_OnEntityAdd(object sender, Entity entity, EventArgs e)
		{
			Saved = false;
			button1.Enabled = !Saved;
		}

		public void CreateCell (Image source_image, Point point, Size size)
		{

		}

		#region "Mode Selection"

		private void SelectionButtonHighlight()
		{
			toolStripDropDownButton1.BackColor = SystemColors.Control;
			toolStripDropDownButton2.BackColor = SystemColors.Control;
			toolStripDropDownButton3.BackColor = SystemColors.Control;
		}

		private void ViasButtonHighlight()
		{
			toolStripDropDownButton1.BackColor = SystemColors.ActiveCaption;
			toolStripDropDownButton2.BackColor = SystemColors.Control;
			toolStripDropDownButton3.BackColor = SystemColors.Control;
		}

		private void WiresButtonHighlight()
		{
			toolStripDropDownButton1.BackColor = SystemColors.Control;
			toolStripDropDownButton2.BackColor = SystemColors.ActiveCaption;
			toolStripDropDownButton3.BackColor = SystemColors.Control;
		}

		private void CellsButtonHighlight()
		{
			toolStripDropDownButton1.BackColor = SystemColors.Control;
			toolStripDropDownButton2.BackColor = SystemColors.Control;
			toolStripDropDownButton3.BackColor = SystemColors.ActiveCaption;
		}

		private void toolStripMenuItem1_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.ViasConnect;
			ViasButtonHighlight();
		}

		private void viasPowerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.ViasPower;
			ViasButtonHighlight();
		}

		private void viasGroundToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.ViasGround;
			ViasButtonHighlight();
		}

		private void viasInputToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.ViasInput;
			ViasButtonHighlight();
		}

		private void viasOutputToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.ViasOutput;
			ViasButtonHighlight();
		}

		private void viasInoutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.ViasInout;
			ViasButtonHighlight();
		}

		private void viasFloatingToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.ViasFloating;
			ViasButtonHighlight();
		}

		private void wireInterconnectToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.WireInterconnect;
			WiresButtonHighlight();
		}

		private void wirePowerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.WirePower;
			WiresButtonHighlight();
		}

		private void wireGroundToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.WireGround;
			WiresButtonHighlight();
		}

		private void cellNotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellNot;
			CellsButtonHighlight();
		}

		private void cellBufferToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellBuffer;
			CellsButtonHighlight();
		}

		private void cellMuxToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellMux;
			CellsButtonHighlight();
		}

		private void cellLogicToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellLogic;
			CellsButtonHighlight();
		}

		private void cellAdderToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellAdder;
			CellsButtonHighlight();
		}

		private void cellBusSupportToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellBusSupp;
			CellsButtonHighlight();
		}

		private void cellFlipFlopToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellFlipFlop;
			CellsButtonHighlight();
		}

		private void cellLatchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellLatch;
			CellsButtonHighlight();
		}

		private void cellOtherToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellOther;
			CellsButtonHighlight();
		}

		private void unitRegisterFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.UnitRegfile;
			CellsButtonHighlight();
		}

		private void unitMemoryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.UnitMemory;
			CellsButtonHighlight();
		}

		private void unitCustomToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.UnitCustom;
			CellsButtonHighlight();
		}

		#endregion "Mode Selection"

		private void FormCells_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F1 || e.KeyCode == Keys.Escape)
			{
				entityBox1.Mode = EntityMode.Selection;
				SelectionButtonHighlight();
			}
			else if (e.KeyCode == Keys.F2)
			{
				entityBox1.Mode = EntityMode.ViasConnect;
				ViasButtonHighlight();
			}
			else if (e.KeyCode == Keys.F3)
			{
				entityBox1.Mode = EntityMode.WireInterconnect;
				WiresButtonHighlight();
			}
			else if (e.KeyCode == Keys.Escape)
			{
				Close();
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Saved = true;
			button1.Enabled = !Saved;
		}

		private void FormCells_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (!Saved)
			{
				var res = MessageBox.Show(this, "You sure you want to get out without saving?", "Exit",
				MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
				if (res != DialogResult.Yes)
				{
					e.Cancel = true;
				}
			}
		}
	}
}
