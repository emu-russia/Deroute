using System.Collections.Generic;
using System.Windows.Forms;

namespace DerouteSharp
{
	public partial class FormCellPorts : Form
	{
		public FormCellPorts(List<Entity> ports)
		{
			InitializeComponent();
			textBox1.Text = PortsToText(ports);
		}

		private string PortsToText (List<Entity> ports)
		{
			string text = "";

			return text;
		}
	}
}
