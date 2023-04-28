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
		public FormWaves()
		{
			InitializeComponent();
		}

		private void FormWaves_Load(object sender, EventArgs e)
		{
			wavesControl1.EnabledDottedEveryNth(2, true);
			wavesControl1.EnableSelection(true);
		}

		public void Update(ValueChangeData[] samples, long bias)
		{
			wavesControl1.PlotWaves(samples, bias);
		}

		private void FormWaves_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				Close();
			}
		}

		private void toolStripButton1_Click(object sender, EventArgs e)
		{
			if (wavesControl1.IsSelectedSomething())
			{
				long bias;
				ValueChangeData[] vcd = wavesControl1.SnatchSelection (out bias);
				wavesControl1.ClearSelection();

				FormWaves waves = new FormWaves();
				waves.Show();
				waves.Update(vcd, bias);
			}
		}
	}
}
