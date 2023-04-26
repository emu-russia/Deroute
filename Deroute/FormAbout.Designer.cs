namespace DerouteSharp
{
	partial class FormAbout
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormAbout));
			this.button1 = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.linkLabel1 = new System.Windows.Forms.LinkLabel();
			this.entityBox1 = new System.Windows.Forms.EntityBox();
			this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
			this.SuspendLayout();
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.Location = new System.Drawing.Point(638, 396);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(109, 41);
			this.button1.TabIndex = 0;
			this.button1.Text = "Close";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label1.Location = new System.Drawing.Point(584, 258);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(172, 48);
			this.label1.TabIndex = 1;
			this.label1.Text = "Deroute Tool 2.5\r\n© 2023, emu-russia";
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(24, 266);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(125, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = ".NET Framework Version";
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(155, 266);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(42, 13);
			this.label3.TabIndex = 3;
			this.label3.Text = "Version";
			// 
			// label4
			// 
			this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(24, 293);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(75, 13);
			this.label4.TabIndex = 4;
			this.label4.Text = "Memory in use";
			// 
			// label5
			// 
			this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(155, 293);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(33, 13);
			this.label5.TabIndex = 5;
			this.label5.Text = "Bytes";
			// 
			// label6
			// 
			this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(24, 322);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(452, 26);
			this.label6.TabIndex = 6;
			this.label6.Text = "Everything (code, images etc) is public domain (Creative Commons Zero). Use at yo" +
    "ur own risk)\r\nNo credits required.\r\n";
			// 
			// linkLabel1
			// 
			this.linkLabel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.linkLabel1.AutoSize = true;
			this.linkLabel1.Location = new System.Drawing.Point(24, 366);
			this.linkLabel1.Name = "linkLabel1";
			this.linkLabel1.Size = new System.Drawing.Size(163, 13);
			this.linkLabel1.TabIndex = 7;
			this.linkLabel1.TabStop = true;
			this.linkLabel1.Text = "https://discord.gg/WJcvqyCHkh";
			this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
			// 
			// entityBox1
			// 
			this.entityBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.entityBox1.AutoPriority = true;
			this.entityBox1.BackColor = System.Drawing.Color.SlateGray;
			this.entityBox1.BeaconPriority = 4;
			this.entityBox1.CellAdderColor = System.Drawing.Color.Red;
			this.entityBox1.CellBufferColor = System.Drawing.Color.Navy;
			this.entityBox1.CellBusSuppColor = System.Drawing.Color.DarkViolet;
			this.entityBox1.CellFlipFlopColor = System.Drawing.Color.Lime;
			this.entityBox1.CellLatchColor = System.Drawing.Color.Cyan;
			this.entityBox1.CellLogicColor = System.Drawing.Color.Yellow;
			this.entityBox1.CellMuxColor = System.Drawing.Color.DarkOrange;
			this.entityBox1.CellNotColor = System.Drawing.Color.Navy;
			this.entityBox1.CellOpacity = 128;
			this.entityBox1.CellOtherColor = System.Drawing.Color.Snow;
			this.entityBox1.CellOverrideColor = System.Drawing.Color.Black;
			this.entityBox1.CellPriority = 1;
			this.entityBox1.CellTextAlignment = TextAlignment.TopLeft;
			this.entityBox1.ForeColor = System.Drawing.Color.Snow;
			this.entityBox1.Grayscale = false;
			this.entityBox1.HideCells = false;
			this.entityBox1.HideGrid = true;
			this.entityBox1.HideImage = false;
			this.entityBox1.HideLambdaMetrics = true;
			this.entityBox1.HideRegions = false;
			this.entityBox1.HideVias = false;
			this.entityBox1.HideWires = false;
			this.entityBox1.HighZColor = System.Drawing.Color.Gold;
			this.entityBox1.ImageOpacity = 100;
			this.entityBox1.Lambda = 5F;
			this.entityBox1.Location = new System.Drawing.Point(12, 12);
			this.entityBox1.Mode = System.Windows.Forms.EntityMode.Selection;
			this.entityBox1.Name = "entityBox1";
			this.entityBox1.OneColor = System.Drawing.Color.LawnGreen;
			this.entityBox1.RegionOpacity = 128;
			this.entityBox1.RegionOverrideColor = System.Drawing.Color.Black;
			this.entityBox1.RegionPriority = 0;
			this.entityBox1.ScrollX = 0F;
			this.entityBox1.ScrollY = 0F;
			this.entityBox1.SelectEntitiesAfterAdd = true;
			this.entityBox1.SelectionBoxColor = System.Drawing.Color.Red;
			this.entityBox1.SelectionColor = System.Drawing.Color.LimeGreen;
			this.entityBox1.Size = new System.Drawing.Size(744, 243);
			this.entityBox1.TabIndex = 8;
			this.entityBox1.Text = "entityBox1";
			this.entityBox1.UnitCustomColor = System.Drawing.Color.Snow;
			this.entityBox1.UnitMemoryColor = System.Drawing.Color.Snow;
			this.entityBox1.UnitRegfileColor = System.Drawing.Color.Snow;
			this.entityBox1.ViasBaseSize = 2;
			this.entityBox1.ViasConnectColor = System.Drawing.Color.Chartreuse;
			this.entityBox1.ViasFloatingColor = System.Drawing.Color.Gray;
			this.entityBox1.ViasGroundColor = System.Drawing.Color.Lime;
			this.entityBox1.ViasGroundText = "1\'b0";
			this.entityBox1.ViasInoutColor = System.Drawing.Color.Gold;
			this.entityBox1.ViasInputColor = System.Drawing.Color.Green;
			this.entityBox1.ViasOpacity = 255;
			this.entityBox1.ViasOutputColor = System.Drawing.Color.Red;
			this.entityBox1.ViasOverrideColor = System.Drawing.Color.Black;
			this.entityBox1.ViasPowerColor = System.Drawing.Color.Tomato;
			this.entityBox1.ViasPowerText = "1\'b1";
			this.entityBox1.ViasPriority = 1;
			this.entityBox1.ViasShape = ViasShape.Square;
			this.entityBox1.ViasTextAlignment = TextAlignment.Top;
			this.entityBox1.WireBaseSize = 0;
			this.entityBox1.WireGroundColor = System.Drawing.Color.Green;
			this.entityBox1.WireInterconnectColor = System.Drawing.Color.DeepSkyBlue;
			this.entityBox1.WireOpacity = 128;
			this.entityBox1.WireOverrideColor = System.Drawing.Color.Black;
			this.entityBox1.WirePowerColor = System.Drawing.Color.Red;
			this.entityBox1.WirePriority = 3;
			this.entityBox1.WireSelectionAutoTraverse = false;
			this.entityBox1.WireTextAlignment = TextAlignment.TopLeft;
			this.entityBox1.ZeroColor = System.Drawing.Color.Green;
			this.entityBox1.Zoom = 100;
			this.entityBox1.OnEntityAdd += new System.Windows.Forms.EntityBoxEntityEventHandler(this.entityBox1_OnEntityAdd);
			// 
			// backgroundWorker1
			// 
			this.backgroundWorker1.WorkerReportsProgress = true;
			this.backgroundWorker1.WorkerSupportsCancellation = true;
			this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
			this.backgroundWorker1.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker1_ProgressChanged);
			// 
			// FormAbout
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(769, 454);
			this.Controls.Add(this.entityBox1);
			this.Controls.Add(this.linkLabel1);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.button1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MinimumSize = new System.Drawing.Size(520, 327);
			this.Name = "FormAbout";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "About";
			this.SizeChanged += new System.EventHandler(this.FormAbout_SizeChanged);
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.About_KeyUp);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.EntityBox entityBox1;
		private System.ComponentModel.BackgroundWorker backgroundWorker1;
	}
}