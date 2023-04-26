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
	public partial class FormWaves : Form
	{
		private DerouteSim sim;

		public FormWaves(DerouteSim parent_sim)
		{
			InitializeComponent();
			sim = parent_sim;
		}

		private void FormWaves_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				Close();
			}
		}
	}
}
