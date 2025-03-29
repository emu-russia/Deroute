// Key input handling

using System.Drawing;
using System.Collections.Generic;

namespace System.Windows.Forms
{
	public partial class EntityBox : Control
	{

		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
				DeleteSelected();

			else if (e.KeyCode == Keys.Escape)
				RemoveSelection();

			else if (e.KeyCode == Keys.Home)
			{
				_ScrollX = 0;
				_ScrollY = 0;
				Invalidate();

				if (OnScrollChanged != null)
					OnScrollChanged(this, EventArgs.Empty);
			}

			else if ((e.KeyCode == Keys.Right ||
						e.KeyCode == Keys.Left ||
						e.KeyCode == Keys.Up ||
						e.KeyCode == Keys.Down) && Mode == EntityMode.Selection && !e.Control)
			{
				bool NeedUpdate = false;
				float deltaX = 0;
				float deltaY = 0;

				switch (e.KeyCode)
				{
					case Keys.Right:
						deltaX = +0.1F;
						deltaY = 0;
						break;
					case Keys.Left:
						deltaX = -0.1F;
						deltaY = 0;
						break;
					case Keys.Up:
						deltaX = 0;
						deltaY = -0.1F;
						break;
					case Keys.Down:
						deltaX = 0;
						deltaY = +0.1F;
						break;
				}

				foreach (Entity entity in GetEntities())
				{
					if (entity.Selected)
					{
						if (entity.PathPoints != null && entity.PathPoints.Count != 0)
						{
							entity.LambdaX += deltaX;
							entity.LambdaY += deltaY;

							List<PointF> points = new List<PointF>();

							foreach (PointF point in entity.PathPoints)
							{
								PointF newPoint = new PointF();

								newPoint.X = point.X + deltaX;
								newPoint.Y = point.Y + deltaY;

								points.Add(newPoint);
							}

							entity.PathPoints = points;
						}

						entity.LambdaX += deltaX;
						entity.LambdaY += deltaY;
						entity.LambdaEndX += deltaX;
						entity.LambdaEndY += deltaY;
						NeedUpdate = true;
					}
				}

				if (NeedUpdate)
					Invalidate();
			}

			base.OnKeyUp(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.C && e.Control)
			{
				Copy();
			}
			else if (e.KeyCode == Keys.V && e.Control)
			{
				Paste();
			}

			else if ((e.KeyCode == Keys.Right ||
						e.KeyCode == Keys.Left ||
						e.KeyCode == Keys.Up ||
						e.KeyCode == Keys.Down) && e.Control)
			{
				float panDelta = 5.0f;

				// With an extra Shift press to make a quadratic acceleration

				if (e.Shift)
					panDelta *= panDelta;

				float deltaX = 0;
				float deltaY = 0;

				switch (e.KeyCode)
				{
					case Keys.Right:
						deltaX = -panDelta;
						deltaY = 0;
						break;
					case Keys.Left:
						deltaX = +panDelta;
						deltaY = 0;
						break;
					case Keys.Up:
						deltaX = 0;
						deltaY = +panDelta;
						break;
					case Keys.Down:
						deltaX = 0;
						deltaY = -panDelta;
						break;
				}

				PointF lambdaCoord = new PointF(ScrollX - deltaX, ScrollY - deltaY);
				UpdateScrollDirectionAngle(lambdaCoord);

				ScrollX += deltaX;
				ScrollY += deltaY;
			}

			base.OnKeyDown(e);
		}

	}
}
