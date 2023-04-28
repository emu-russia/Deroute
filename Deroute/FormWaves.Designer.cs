namespace DerouteSharp
{
	partial class FormWaves
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormWaves));
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.wavesControl1 = new System.Windows.Forms.WavesControl();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStrip1
			// 
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(909, 25);
			this.toolStrip1.TabIndex = 2;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// toolStripButton1
			// 
			this.toolStripButton1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
			this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton1.Name = "toolStripButton1";
			this.toolStripButton1.Size = new System.Drawing.Size(47, 22);
			this.toolStripButton1.Text = "Snatch";
			this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
			// 
			// wavesControl1
			// 
			this.wavesControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.wavesControl1.DottedColor = System.Drawing.Color.Silver;
			this.wavesControl1.DottedOpacity = 65;
			this.wavesControl1.FillColor = System.Drawing.Color.Black;
			this.wavesControl1.GridColor = System.Drawing.Color.Green;
			this.wavesControl1.GridOpacity = 95;
			this.wavesControl1.HighZColor = System.Drawing.Color.Gold;
			this.wavesControl1.LabelsColor = System.Drawing.Color.White;
			this.wavesControl1.Location = new System.Drawing.Point(0, 25);
			this.wavesControl1.Name = "wavesControl1";
			this.wavesControl1.SelectionColor = System.Drawing.Color.GhostWhite;
			this.wavesControl1.SignalColor = System.Drawing.Color.SpringGreen;
			this.wavesControl1.Size = new System.Drawing.Size(909, 361);
			this.wavesControl1.TabIndex = 3;
			this.wavesControl1.Text = "wavesControl1";
			this.wavesControl1.UndefinedColor = System.Drawing.Color.Red;
			// 
			// FormWaves
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(909, 386);
			this.Controls.Add(this.wavesControl1);
			this.Controls.Add(this.toolStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MinimumSize = new System.Drawing.Size(300, 200);
			this.Name = "FormWaves";
			this.Text = "Waves";
			this.Load += new System.EventHandler(this.FormWaves_Load);
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FormWaves_KeyUp);
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton toolStripButton1;
		private System.Windows.Forms.WavesControl wavesControl1;
	}
}