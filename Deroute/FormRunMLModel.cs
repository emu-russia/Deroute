using System.Windows.Forms;

namespace DerouteSharp
{
	public partial class FormRunMLModel : Form
	{
		public FormRunMLModel()
		{
			InitializeComponent();

			progressBar1.Style = ProgressBarStyle.Marquee;
			progressBar1.MarqueeAnimationSpeed = 30;
		}
	}
}
