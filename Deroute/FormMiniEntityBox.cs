using System.Windows.Forms;

namespace DerouteSharp
{
	public partial class FormMiniEntityBox : Form
	{
		public Entity root;
		public object custom;

		public FormMiniEntityBox(object custom, Entity root)
		{
			InitializeComponent();
			entityBox1.root.Children = root.Children;
			this.root = entityBox1.root;
			this.custom = custom;
		}

		private void FormMiniEntityBox_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F1)
			{
				entityBox1.Mode = EntityMode.Selection;
			}
			else if (e.KeyCode == Keys.F2)
			{
				entityBox1.Mode = EntityMode.ViasConnect;
			}
			else if (e.KeyCode == Keys.F3)
			{
				entityBox1.Mode = EntityMode.WireInterconnect;
			}
			else if (e.KeyCode == Keys.A && e.Control)
			{
				entityBox1.SelectAll();
			}
		}
	}
}
