using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

namespace System.Windows.Forms
{
	public class Minimap
	{
		private Bitmap _cachedBitmap;
		private bool _hasImage;
		private bool _isTilemap;

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
			_isTilemap = false;
		}

		public bool HasImage { get => _hasImage; set => _hasImage = value; }

		public void SetTilemapMode(bool isTilemap)
		{
			_isTilemap = isTilemap;
			InvalidateCache();
		}

		public void Draw(Graphics targetGraphics, EntityBox entityBox)
		{
			if (!Enabled || !HasImage || entityBox.Image == null)
				return;

			Rectangle targetRect = CalculatePosition(entityBox);

			if (_isTilemap)
			{
				DrawMinimapTilemap(targetGraphics, entityBox, targetRect);
			}
			else
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
		}

		private Rectangle CalculatePosition(EntityBox entityBox)
		{
			int width = CalculateWidth(entityBox);
			int height = CalculateHeight(entityBox);

			int x, y;

			switch (Position)
			{
				case MinimapPosition.TopLeft:
					x = Margin;
					y = Margin;
					break;
				case MinimapPosition.TopRight:
					x = entityBox.Width - width - Margin;
					y = Margin;
					break;
				case MinimapPosition.BottomLeft:
					x = Margin;
					y = entityBox.Height - height - Margin;
					break;
				case MinimapPosition.BottomRight:
					x = entityBox.Width - width - Margin;
					y = entityBox.Height - height - Margin;
					break;
				default:
					x = entityBox.Width - width - Margin;
					y = Margin;
					break;
			}

			x = Math.Max(Margin, x);
			y = Math.Max(Margin, y);

			return new Rectangle(x, y, width, height);
		}

		private int CalculateWidth(EntityBox entityBox)
		{
			int calculatedSize = (int)(entityBox.Width * SizePercent);
			return Math.Max(calculatedSize, MinSize);
		}

		private int CalculateHeight(EntityBox entityBox)
		{
			if (!_hasImage || entityBox.Image == null)
			{
				return CalculateWidth(entityBox);
			}

			int width = CalculateWidth(entityBox);

			float imageWidth = entityBox.Image.Width;
			float imageHeight = entityBox.Image.Height;

			if (imageWidth <= 0 || imageHeight <= 0)
				return width;

			float aspectRatio = imageHeight / imageWidth;
			int height = (int)(width * aspectRatio);

			return Math.Max(height, MinSize);
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

			int width = CalculateWidth(entityBox);
			int height = CalculateHeight(entityBox);

			float imageWidth = origImage.Width;
			float imageHeight = origImage.Height;
			float maxDim = Math.Max(imageWidth, imageHeight);

			float scale = width / maxDim;

			int drawWidth = (int)(imageWidth * scale);
			int drawHeight = (int)(imageHeight * scale);

			Bitmap bitmap = new Bitmap(width, height);

			using (Graphics g = Graphics.FromImage(bitmap))
			{
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.SmoothingMode = SmoothingMode.None;

				int x = (width - drawWidth) / 2;
				int y = (height - drawHeight) / 2;

				g.FillRectangle(Brushes.Black, 0, 0, width, height);
				g.DrawImage(origImage, x, y, drawWidth, drawHeight);
			}

			return bitmap;
		}

		private void DrawMinimapTilemap(Graphics targetGraphics, EntityBox entityBox, Rectangle targetRect)
		{
			if (entityBox.OptimizeTilemap == false)
				return;

			var tilemap = entityBox.Tilemap;
			if (tilemap == null || tilemap.Count == 0)
				return;

			int width = targetRect.Width;
			int height = targetRect.Height;

			Bitmap bitmap = new Bitmap(width, height);

			using (Graphics g = Graphics.FromImage(bitmap))
			{
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.SmoothingMode = SmoothingMode.None;
				g.FillRectangle(Brushes.Black, 0, 0, width, height);

				float imageWidth = entityBox.Image.Width;
				float imageHeight = entityBox.Image.Height;
				float maxDim = Math.Max(imageWidth, imageHeight);
				float scale = width / maxDim;

				foreach (Tile tile in tilemap)
				{
					if (tile.img == null)
						continue;

					int drawX = (int)(tile.ofsx * scale);
					int drawY = (int)(tile.ofsy * scale);
					int drawW = (int)(tile.width * scale);
					int drawH = (int)(tile.height * scale);

					g.DrawImage(tile.img, drawX, drawY, drawW, drawH);
				}
			}

			targetGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			targetGraphics.SmoothingMode = SmoothingMode.None;

			targetGraphics.DrawImage(bitmap, targetRect);
			bitmap.Dispose();

			DrawViewport(targetGraphics, entityBox, targetRect);

			Pen borderPen = new Pen(Color.FromArgb(180, Color.White), 1f);
			targetGraphics.DrawRectangle(borderPen, targetRect);
			borderPen.Dispose();
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

			int x = (int)(topLeftImage.X * mapScale);
			int y = (int)(topLeftImage.Y * mapScale);
			int w = (int)((bottomRightImage.X - topLeftImage.X) * mapScale);
			int h = (int)((bottomRightImage.Y - topLeftImage.Y) * mapScale);

			if (w <= 0) w = 1;
			if (h <= 0) h = 1;

			int rx = Math.Max(0, x);
			int ry = Math.Max(0, y);
			int rw = Math.Max(0, Math.Min(x + w, targetRect.Width) - rx);
			int rh = Math.Max(0, Math.Min(y + h, targetRect.Height) - ry);

			if (rw <= 0 || rh <= 0)
				return;

			int alpha = Math.Max(0, Math.Min(255, ViewportOpacity));
			Color vpColor = ViewportColor;

			Brush fillBrush = new SolidBrush(Color.FromArgb(alpha / 2, vpColor));
			Pen fillPen = new Pen(Color.FromArgb(alpha, vpColor), 1f);

			targetGraphics.FillRectangle(fillBrush, rx + targetRect.X, ry + targetRect.Y, rw, rh);
			targetGraphics.DrawRectangle(fillPen, rx + targetRect.X, ry + targetRect.Y, rw, rh);

			fillBrush.Dispose();
			fillPen.Dispose();
		}

		public Rectangle GetTargetRect(EntityBox entityBox)
		{
			return CalculatePosition(entityBox);
		}
	}
}
