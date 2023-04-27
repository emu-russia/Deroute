using System.Windows.Forms;

namespace DerouteSharp
{
	public partial class KeyBind : Form
	{
		public KeyBind()
		{
			InitializeComponent();
		}

		private void KeyBind_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
				Close();
		}
	}
}
