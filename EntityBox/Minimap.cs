using System.Drawing;
using System.Drawing.Drawing2D;

namespace System.Windows.Forms
{
	public class Minimap
	{
		private Bitmap _cachedBitmap;
		private bool _hasImage;

		public bool Enabled { get; set; }
		public float SizePercent { get; set; }
		public MinimapPosition Position { get; set; }
		public Color ViewportColor { get; set; }
		public int ViewportOpacity { get; set; }
		public int MinSize { get; set; }
		public int Margin { get; set; }

		public Minimap()
		{
			Enabled = false;
			SizePercent = 0.15f;
			Position = MinimapPosition.TopRight;
			ViewportColor = Color.Red;
			ViewportOpacity = 128;
			MinSize = 50;
			Margin = 8;
		}

		public void InvalidateCache()
		{
			if (_cachedBitmap != null)
			{
				_cachedBitmap.Dispose();
				_cachedBitmap = null;
			}
			_hasImage = false;
		}

		public bool HasImage { get => _hasImage; set => _hasImage = value; }

		public void Draw(Graphics targetGraphics, EntityBox entityBox)
		{
			if (!Enabled || !HasImage || entityBox.Image == null)
				return;

			int size = CalculateSize(entityBox);
			Rectangle targetRect = CalculatePosition(entityBox, size);

			DrawMinimap(targetGraphics, entityBox, targetRect, size);
		}

		private int CalculateSize(EntityBox entityBox)
		{
			int calculatedSize = (int)(entityBox.Width * SizePercent);
			return Math.Max(calculatedSize, MinSize);
		}

		private Rectangle CalculatePosition(EntityBox entityBox, int size)
		{
			int x, y;

			switch (Position)
			{
				case MinimapPosition.TopLeft:
					x = Margin;
					y = Margin;
					break;
				case MinimapPosition.TopRight:
					x = entityBox.Width - size - Margin;
					y = Margin;
					break;
				case MinimapPosition.BottomLeft:
					x = Margin;
					y = entityBox.Height - size - Margin;
					break;
				case MinimapPosition.BottomRight:
					x = entityBox.Width - size - Margin;
					y = entityBox.Height - size - Margin;
					break;
				default:
					x = entityBox.Width - size - Margin;
					y = Margin;
					break;
			}

			x = Math.Max(Margin, x);
			y = Math.Max(Margin, y);

			return new Rectangle(x, y, size, size);
		}

		private void DrawMinimap(Graphics targetGraphics, EntityBox entityBox, Rectangle targetRect, int size)
		{
			Bitmap sourceBitmap = GetMinimapBitmap(entityBox);

			if (sourceBitmap == null)
				return;

			Rectangle sourceRect = new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height);

			targetGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			targetGraphics.SmoothingMode = SmoothingMode.None;

			targetGraphics.FillRectangle(Brushes.Black, targetRect);

			targetGraphics.DrawImage(sourceBitmap, targetRect, sourceRect, GraphicsUnit.Pixel);

			DrawViewport(targetGraphics, entityBox, targetRect);

			Pen borderPen = new Pen(Color.FromArgb(180, Color.White), 1f);
			targetGraphics.DrawRectangle(borderPen, targetRect);
			borderPen.Dispose();
		}

		private Bitmap GetMinimapBitmap(EntityBox entityBox)
		{
			if (!_hasImage || _cachedBitmap == null)
			{
				_cachedBitmap = CreateMinimapBitmap(entityBox);
				_hasImage = true;
			}

			return _cachedBitmap;
		}

		private Bitmap CreateMinimapBitmap(EntityBox entityBox)
		{
			Image origImage = entityBox.Image;
			if (origImage == null)
				return null;

			int targetSize = (int)(entityBox.Width * SizePercent);
			targetSize = Math.Max(targetSize, MinSize);

			Bitmap bitmap = new Bitmap(targetSize, targetSize);

			using (Graphics g = Graphics.FromImage(bitmap))
			{
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.SmoothingMode = SmoothingMode.None;

				float imageWidth = origImage.Width;
				float imageHeight = origImage.Height;
				float maxDim = Math.Max(imageWidth, imageHeight);

				float scale = targetSize / maxDim;

				int drawWidth = (int)(imageWidth * scale);
				int drawHeight = (int)(imageHeight * scale);

				int x = (targetSize - drawWidth) / 2;
				int y = (targetSize - drawHeight) / 2;

				g.FillRectangle(Brushes.Black, 0, 0, targetSize, targetSize);
				g.DrawImage(origImage, x, y, drawWidth, drawHeight);
			}

			return bitmap;
		}

		private void DrawViewport(Graphics targetGraphics, EntityBox entityBox, Rectangle targetRect)
		{
			float imageWidth = entityBox.Image.Width;
			float imageHeight = entityBox.Image.Height;

			if (imageWidth <= 0 || imageHeight <= 0)
				return;

			PointF topLeftLambda = entityBox.ScreenToLambda(0, 0);
			PointF bottomRightLambda = entityBox.ScreenToLambda(entityBox.Width, entityBox.Height);

			PointF topLeftImage = entityBox.LambdaToImage(topLeftLambda.X, topLeftLambda.Y);
			PointF bottomRightImage = entityBox.LambdaToImage(bottomRightLambda.X, bottomRightLambda.Y);

			float mapScale = targetRect.Width / Math.Max(imageWidth, imageHeight);

			int x = (int)(topLeftImage.X * mapScale) + targetRect.X;
			int y = (int)(topLeftImage.Y * mapScale) + targetRect.Y;
			int w = (int)((bottomRightImage.X - topLeftImage.X) * mapScale);
			int h = (int)((bottomRightImage.Y - topLeftImage.Y) * mapScale);

			if (w <= 0) w = 1;
			if (h <= 0) h = 1;

			int maxX = targetRect.Width - (x - targetRect.X);
			int maxY = targetRect.Height - (y - targetRect.Y);

			if (w > maxX && maxX > 0)
			{
				w = maxX;
			}

			if (h > maxY && maxY > 0)
			{
				h = maxY;
			}

			x = Math.Max(targetRect.X, x);
			y = Math.Max(targetRect.Y, y);

			x = Math.Min(x, targetRect.Right - w);
			y = Math.Min(y, targetRect.Bottom - h);

			int alpha = Math.Max(0, Math.Min(255, ViewportOpacity));
			Color vpColor = ViewportColor;

			Brush fillBrush = new SolidBrush(Color.FromArgb(alpha / 2, vpColor));
			Pen fillPen = new Pen(Color.FromArgb(alpha, vpColor), 1f);

			targetGraphics.FillRectangle(fillBrush, x, y, w, h);
			targetGraphics.DrawRectangle(fillPen, x, y, w, h);

			fillBrush.Dispose();
			fillPen.Dispose();
		}

		public Rectangle GetTargetRect(EntityBox entityBox)
		{
			int size = CalculateSize(entityBox);
			return CalculatePosition(entityBox, size);
		}
	}
}
