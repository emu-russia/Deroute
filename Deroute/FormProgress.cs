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
	public partial class FormProgress : Form
	{
		public bool Aborted = false;

		public FormProgress(string caption, string text)
		{
			InitializeComponent();

			this.Text = caption;
			label1.Text = text;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Aborted = true;
			Close();
		}

		private void FormProgress_FormClosed(object sender, FormClosedEventArgs e)
		{
			Aborted = true;
		}
	}
}
