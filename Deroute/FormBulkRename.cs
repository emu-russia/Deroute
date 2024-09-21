using System;
using System.Windows.Forms;

namespace DerouteSharp
{
	public partial class FormBulkRename : Form
	{
		EntityBox parentBox;
		int counter = 1;

		enum ComboItem
		{
			Any = 0,
			AnyVias,
			AnyWire,
			AnyCell,
			AnyUnit,
			ViasInput,
			ViasOutput,
			ViasInout,
			ViasConnect,
			ViasFloating,
			ViasPower,
			ViasGround,
			WireInterconnect,
			WirePower,
			WireGround,
			CellNot,
			CellBuffer,
			CellMux,
			CellLogic,
			CellAdder,
			CellBusSupp,
			CellFlipFlop,
			CellLatch,
			CellOther,
			UnitRegfile,
			UnitMemory,
			UnitCustom,
			Region,
		}

		public FormBulkRename(EntityBox ebox)
		{
			parentBox = ebox;

			InitializeComponent();

			comboBox1.SelectedIndex = (int)ComboItem.AnyCell;
		}

		private void RenameItemsRecursive(Entity parent)
		{
			if (IsEntityMeetRequirements(parent))
			{
				bool append = checkBox1.Checked;

				if (append)
				{
					parent.Label += " " + textBox1.Text + counter.ToString();
				}
				else
				{
					parent.Label = textBox1.Text + counter.ToString();
				}

				counter++;
			}

			foreach (var child in parent.Children)
			{
				RenameItemsRecursive(child);
			}
		}

		/// <summary>
		/// Check if the specified entity satisfies the criteria (filter).
		/// </summary>
		/// <param name="entity"></param>
		/// <returns></returns>
		private bool IsEntityMeetRequirements(Entity entity)
		{
			bool typeApplied = false;

			if (checkBox2.Enabled && !entity.Visible)
			{
				return false;
			}

			if (checkBox2.Enabled && entity.Visible)
			{
				// Check that all parents are visible

				Entity parent = entity.parent;
				bool parentsVisible = true;

				while (parent != null)
				{
					if (!parent.Visible)
					{
						parentsVisible = false;
						break;
					}

					parent = parent.parent;
				}
				if (!parentsVisible)
				{
					return false;
				}
			}

			switch ((ComboItem)comboBox1.SelectedIndex)
			{
				case ComboItem.Any:
					typeApplied = entity.IsVias() || entity.IsWire() || entity.IsCell() || entity.IsUnit();
					break;
				case ComboItem.AnyVias:
					typeApplied = entity.IsVias();
					break;
				case ComboItem.AnyWire:
					typeApplied = entity.IsWire();
					break;
				case ComboItem.AnyCell:
					typeApplied = entity.IsCell();
					break;
				case ComboItem.AnyUnit:
					typeApplied = entity.IsUnit();
					break;

				case ComboItem.ViasInput:
					typeApplied = entity.Type == EntityType.ViasInput;
					break;
				case ComboItem.ViasOutput:
					typeApplied = entity.Type == EntityType.ViasOutput;
					break;
				case ComboItem.ViasInout:
					typeApplied = entity.Type == EntityType.ViasInout;
					break;
				case ComboItem.ViasConnect:
					typeApplied = entity.Type == EntityType.ViasConnect;
					break;
				case ComboItem.ViasFloating:
					typeApplied = entity.Type == EntityType.ViasFloating;
					break;
				case ComboItem.ViasPower:
					typeApplied = entity.Type == EntityType.ViasPower;
					break;
				case ComboItem.ViasGround:
					typeApplied = entity.Type == EntityType.ViasGround;
					break;

				case ComboItem.WireInterconnect:
					typeApplied = entity.Type == EntityType.WireInterconnect;
					break;
				case ComboItem.WirePower:
					typeApplied = entity.Type == EntityType.WirePower;
					break;
				case ComboItem.WireGround:
					typeApplied = entity.Type == EntityType.WireGround;
					break;

				case ComboItem.CellNot:
					typeApplied = entity.Type == EntityType.CellNot;
					break;
				case ComboItem.CellBuffer:
					typeApplied = entity.Type == EntityType.CellBuffer;
					break;
				case ComboItem.CellMux:
					typeApplied = entity.Type == EntityType.CellMux;
					break;
				case ComboItem.CellLogic:
					typeApplied = entity.Type == EntityType.CellLogic;
					break;
				case ComboItem.CellAdder:
					typeApplied = entity.Type == EntityType.CellAdder;
					break;
				case ComboItem.CellBusSupp:
					typeApplied = entity.Type == EntityType.CellBusSupp;
					break;
				case ComboItem.CellFlipFlop:
					typeApplied = entity.Type == EntityType.CellFlipFlop;
					break;
				case ComboItem.CellLatch:
					typeApplied = entity.Type == EntityType.CellLatch;
					break;
				case ComboItem.CellOther:
					typeApplied = entity.Type == EntityType.CellOther;
					break;

				case ComboItem.UnitRegfile:
					typeApplied = entity.Type == EntityType.UnitRegfile;
					break;
				case ComboItem.UnitMemory:
					typeApplied = entity.Type == EntityType.UnitMemory;
					break;
				case ComboItem.UnitCustom:
					typeApplied = entity.Type == EntityType.UnitCustom;
					break;

				case ComboItem.Region:
					typeApplied = entity.Type == EntityType.Region;
					break;
			}

			return typeApplied;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			RenameItemsRecursive(parentBox.root);
			Close();
		}
	}
}
