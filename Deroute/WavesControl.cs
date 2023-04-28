using System.ComponentModel;
using System.Drawing;
using System.Collections.Generic;

namespace System.Windows.Forms
{
	public partial class WavesControl : Control
	{
		private BufferedGraphics gfx;
		private BufferedGraphicsContext context;

		private ValueChangeData[] data = Array.Empty<ValueChangeData>();
		private float minVal = 0.0f;
		private float maxVal = 1.0f;

		private Pen grid_pen;
		private Brush label_brush;
		private Pen signal_pen;
		private Pen highz_pen;
		private Pen x_pen;
		private Pen selection_pen;
		private Pen dotted_pen;

		private bool gdi_init = false;

		private PointF p1 = new PointF(0, 0);
		private PointF p2 = new PointF(0, 0);
		private PointF tp = new PointF(0, 0);

		private float xofs = 0.0f;  // Offset of the beginning of signals relative to the size of the longest text label
		private float yofs = 0.0f;      // Offset to output a legend with a cycle counter
		private bool ofs_done = false;
		private long max_samples = 0;        // The maximum number of values. For signals with less - the beginning is scored with the value Z
		private long clock_bias = 0;        // Offset on the phase scale

		public WavesControl()
		{
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		}

		/// <summary>
		/// Outputs a waves dump. Automatically performs a control update.
		/// </summary>
		public void PlotWaves(ValueChangeData[] samples, long bias)
		{
			if (data.Length != samples.Length)
			{
				data = new ValueChangeData[samples.Length];
			}
			Array.Copy(samples, data, samples.Length);

			clock_bias = bias;

			minVal = 0.0f;
			maxVal = 1.0f;
			MinMaxPadding(5.0f);

			// Determine the maximum number of values among all signal histories

			max_samples = 0;

			for (int i=0; i<data.Length; i++)
			{
				if (data[i].values.Length > max_samples)
				{
					max_samples = data[i].values.Length;
				}
			}

			if (!gdi_init)
			{
				grid_pen = new Pen(Color.FromArgb(GridOpacity, GridColor));
				signal_pen = new Pen(new SolidBrush(SignalColor));
				highz_pen = new Pen(new SolidBrush(HighZColor));
				x_pen = new Pen(new SolidBrush(UndefinedColor));
				label_brush = new SolidBrush(LabelsColor);
				selection_pen = new Pen(SelectionColor, 2);
				dotted_pen = new Pen(Color.FromArgb(DottedOpacity, DottedColor));
				dotted_pen.DashPattern = new float[] { 1, 4 };
				gdi_init = true;
			}

			ofs_done = false;

			Invalidate();
		}

		/// <summary>
		/// Make a slight padding of the signal (prc) at the top and bottom
		/// </summary>
		private void MinMaxPadding(float paddingPrc)
		{
			float gap = (paddingPrc * Math.Abs(maxVal - minVal)) / 100;
			maxVal += gap;
			minVal -= gap;
		}

		private void ReallocateGraphics()
		{
			context = BufferedGraphicsManager.Current;
			context.MaximumBuffer = new Size(Width + 1, Height + 1);

			gfx = context.Allocate(CreateGraphics(),
				 new Rectangle(0, 0, Width, Height));
		}

		private void DrawGrid (Graphics gr)
		{
			for (int i=0; i<data.Length; i++)
			{
				float h = (Height - yofs) / data.Length;
				float y = i * h + yofs;
				gr.DrawLine(grid_pen, 0, y, Width, y);
			}
			gr.DrawLine(grid_pen, xofs, 0, xofs, Height);
		}

		private void DrawSignal (Graphics gr, int n)
		{
			PointF prev = TransformCoord(-1, LogicValue.Z, n);

			// Make the padded Z value of signals that have been enabled by Scope not from the beginning of the simulation

			long padding_size = max_samples - data[n].values.Length;
			if (padding_size != 0)
			{
				for (long i = 0; i < padding_size; i++)
				{
					PointF pos = TransformCoord(i, LogicValue.Z, n);
					gr.DrawLine(LogicValueToPen(LogicValue.Z), prev.X, prev.Y, prev.X, pos.Y);
					gr.DrawLine(LogicValueToPen(LogicValue.Z), prev.X, pos.Y, pos.X, pos.Y);
					prev = pos;
				}
			}

			for (long i = 0; i < data[n].values.Length; i++)
			{
				PointF pos = TransformCoord(padding_size + i, data[n].values[i], n);
				gr.DrawLine(LogicValueToPen(data[n].values[i]), prev.X, prev.Y, prev.X, pos.Y);
				gr.DrawLine(LogicValueToPen(data[n].values[i]), prev.X, pos.Y, pos.X, pos.Y);
				prev = pos;
			}
		}

		private Pen LogicValueToPen (LogicValue val)
		{
			switch (val)
			{
				case LogicValue.Zero: return signal_pen;
				case LogicValue.One: return signal_pen;
				case LogicValue.Z: return highz_pen;
				default: return x_pen;
			}
		}

		private float LogicValueToFloat (LogicValue val)
		{
			switch (val)
			{
				case LogicValue.Zero: return 0.0f;
				case LogicValue.One: return 1.0f;
				default: return 0.5f;
			}
		}

		/// <summary>
		/// Transform a single sample from a VCD into a figure on the screen.
		/// </summary>
		/// <param name="i">The sequence number of the sample from the history (starting from 0) (aka ValueChangeData::values[n])</param>
		/// <param name="val">Logical value</param>
		/// <param name="n">Signal sequence number (starting from zero) (aka data[n])</param>
		/// <returns>Right edge screen coordinates</returns>
		private PointF TransformCoord (long i, LogicValue val, int n)
		{
			long padding_size = max_samples - data[n].values.Length;        // Align to the right signals that were not scoped from the beginning of the simulation
			float w = Width - xofs;                                     // The width of the area where the waves are displayed
			float h = (Height - yofs) / data.Length;                    // Height of area occupied by one signal
			float ofs = h * n;                                              // Vertical offset of the display area of the specified signal n
			float x = (float)((i + 1) * w) / (data[n].values.Length + padding_size);
			float delta = maxVal - minVal;
			if (maxVal == minVal)
				delta = 0.0001f;
			float y = ofs + h - ((LogicValueToFloat(val) - minVal) * h) / delta;
			tp.X = x + xofs;
			tp.Y = y + yofs;
			return tp;
		}

		/// <summary>
		/// Inverse transformation of the screen coordinate (X) to the sequence number of the sample.
		/// </summary>
		/// <param name="x">Screen coordinate X</param>
		/// <returns>The sequence number of the sample (all signals), starting from 0</returns>
		private long InvTransform (int x)
		{
			return Math.Max(Math.Min((long)(((x - xofs) * max_samples) / (Width - xofs)), max_samples-1), 0);
		}

		private void DrawLabels (Graphics gr)
		{
			// Cycles legend
			// float is used so that the cycle number is properly aligned to the middle of the phases.
			// The number of phases is determined by the dotted_every_nth property (usually 2)

			float start_cycle = (float)clock_bias / (float)(dotted_enabled ? dotted_every_nth : 1.0f);
			float end_cycle = (float)(clock_bias + max_samples) / (float)(dotted_enabled ? dotted_every_nth : 1.0f);
			float total_cycles = end_cycle - start_cycle;

			for (float i = 0; i< total_cycles; i += 1.0f)
			{
				float w = (Width - xofs) / total_cycles;
				float x = xofs + i * w;

				string cycle_text = ((long)(i + start_cycle)).ToString();
				SizeF textSize = gr.MeasureString(cycle_text, Font);
				PointF pt = new PointF(
					w / 2 - (int)(textSize.Width / 2) + x,
					yofs / 2 - (int)(textSize.Height / 2));
				gr.DrawString(cycle_text, Font, label_brush, pt);
			}

			// Signal names

			for (int i = 0; i < data.Length; i++)
			{
				float h = (Height - yofs) / data.Length;
				float y = yofs + i * h;

				SizeF textSize = gr.MeasureString(data[i].name, Font);
				PointF pt = new PointF(
					xofs / 2 - (int)(textSize.Width / 2),
					h / 2 - (int)(textSize.Height / 2) + y);
				gr.DrawString(data[i].name, Font, label_brush, pt);
			}
		}

		private void DrawPlot (Graphics gr)
		{
			gr.Clear(FillColor);

			if (Width == 0 || Height == 0 || !gdi_init)
				return;

			// Determine the width of the table with text labels and an indent at the top for the legend with timing

			if (!ofs_done)
			{
				xofs = 0;
				for (int i = 0; i < data.Length; i++)
				{
					int labelWidth = (int)gr.MeasureString(data[i].name + "XX", this.Font).Width;
					if (labelWidth > xofs)
					{
						xofs = labelWidth;
					}
				}

				yofs = (int)gr.MeasureString("XX", this.Font).Height;
				ofs_done = true;
			}

			DrawDotted(gr);
			DrawLabels(gr);
			for (int n = 0; n < data.Length; n++)
			{
				DrawSignal(gr, n);
			}
			DrawGrid(gr);
			DrawSelection(gr);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (gfx == null)
			{
				ReallocateGraphics();
			}

			if (gfx != null)
			{
				DrawPlot(gfx.Graphics);
				gfx.Render(e.Graphics);
			}
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			if (gfx != null)
			{
				gfx.Dispose();
				ReallocateGraphics();
			}

			Invalidate();
			base.OnSizeChanged(e);
		}


#region "Selection/Snatch support"

		private bool selection_enabled = false;
		private int SelectStartMouseX;
		private int SelectStartMouseY;
		private int LastMouseX;
		private int LastMouseY;
		private bool selection_box_active = false;
		private int min_selection_square = 64;
		private bool selection_in_progress = false;
		private Point last_mouse_pos = new Point();

		public void EnableSelection (bool enable)
		{
			selection_enabled = enable;
			ClearSelection();
		}

		public void ClearSelection()
		{
			selection_box_active = false;
			selection_in_progress = false;
			Invalidate();
		}

		public bool IsSelectedSomething()
		{
			if (Width == 0 || Height == 0)
				return false;

			return selection_enabled && selection_box_active;
		}

		public ValueChangeData[] SnatchSelection (out long bias)
		{
			bias = clock_bias;

			if (!IsSelectedSomething())
				return Array.Empty<ValueChangeData>();

			List<ValueChangeData> res = new List<ValueChangeData>();
			int dist = Math.Abs(SelectStartMouseX - LastMouseX);
			int start_x = Math.Min(SelectStartMouseX, LastMouseX);
			long sp = InvTransform(start_x + 1);
			long ep = InvTransform(start_x + dist - 1) + 1;
			long sc = ep - sp;
			bias += sp;

			for (int i=0; i<data.Length; i++)
			{
				ValueChangeData vcd = new ValueChangeData();
				vcd.name = data[i].name;
				vcd.values = new LogicValue[sc];
				int j = 0;
				for (long n=sp; n<ep; n++)
				{
					vcd.values[j] = data[i].values[n];
					j++;
				}
				res.Add(vcd);
			}

			return res.ToArray();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && selection_enabled && !selection_in_progress && data.Length != 0)
			{
				Point pos = GetMousePos(e);
				LastMouseX = SelectStartMouseX = pos.X;
				LastMouseY = SelectStartMouseY = pos.Y;
				selection_box_active = true;
				selection_in_progress = true;
			}

			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (selection_enabled && selection_in_progress)
			{
				int square = Math.Abs(SelectStartMouseX - LastMouseX) * Math.Abs(SelectStartMouseY - LastMouseY);

				selection_box_active = (square > min_selection_square);
				selection_in_progress = false;

				Invalidate();
			}

			base.OnMouseUp(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (selection_enabled && selection_in_progress)
			{
				Point pos = GetMousePos(e);
				LastMouseX = Math.Max(Math.Min(pos.X, Width-1), 0);
				LastMouseY = Math.Max(Math.Min(pos.Y, Height-1), 0);
				Invalidate();
			}

			base.OnMouseMove(e);
		}

		/// <summary>
		/// Snap to every nth grid if dotted mapping is enabled.
		/// </summary>
		private Point GetMousePos (MouseEventArgs e)
		{
			if (dotted_enabled)
			{
				var sample_num = InvTransform(e.X);
				sample_num = (long)Math.Round((double)sample_num / dotted_every_nth) * dotted_every_nth;	// Snap to grid
				var mp = TransformCoord(sample_num - 1, 0, 0);
				last_mouse_pos.X = (int)mp.X;
				last_mouse_pos.Y = e.Y;
			}
			else
			{
				last_mouse_pos.X = e.X;
				last_mouse_pos.Y = e.Y;
			}

			return last_mouse_pos;
		}

		private void DrawSelection(Graphics gr)
		{
			if (selection_enabled && selection_box_active && selection_pen != null)
			{
				Point[] points = new Point[5];

				points[0] = new Point(SelectStartMouseX, SelectStartMouseY);
				points[1] = new Point(SelectStartMouseX, LastMouseY);
				points[2] = new Point(LastMouseX, LastMouseY);
				points[3] = new Point(LastMouseX, SelectStartMouseY);
				points[4] = new Point(SelectStartMouseX, SelectStartMouseY);

				gr.DrawLines(selection_pen, points);
			}
		}

#endregion "Selection/Snatch support"


#region "Dotted Every Nth support"

		private int dotted_every_nth = 0;
		private bool dotted_enabled = false;

		public void EnabledDottedEveryNth (int every_nth_sample, bool enable)
		{
			dotted_every_nth = every_nth_sample;
			dotted_enabled = enable;
			Invalidate();
		}

		private void DrawDotted(Graphics gr)
		{
			if (dotted_enabled)
			{
				for (long i = -1; i < max_samples; i+=dotted_every_nth)
				{
					PointF pos = TransformCoord(i, LogicValue.One, 0);
					gr.DrawLine(dotted_pen, pos.X, yofs, pos.X, Height);
				}
			}
		}

#endregion "Dotted Every Nth support"


		[Category("Waves Appearance")]
		public Color FillColor { get; set; }

		[Category("Waves Appearance")]
		public Color SignalColor { get; set; }

		[Category("Waves Appearance")]
		public Color HighZColor { get; set; }
		
		[Category("Waves Appearance")]
		public Color UndefinedColor { get; set; }

		[Category("Waves Appearance")]
		public Color GridColor { get; set; }

		[Category("Waves Appearance")]
		public Color LabelsColor { get; set; }

		[Category("Waves Appearance")]
		public Color SelectionColor { get; set; }

		[Category("Waves Appearance")]
		public Color DottedColor { get; set; }

		[Category("Waves Appearance")]
		public int GridOpacity { get; set; }

		[Category("Waves Appearance")]
		public int DottedOpacity { get; set; }
	}

	public class ValueChangeData
	{
		public string name;
		public LogicValue[] values;
	}
}
