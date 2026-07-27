using System;
using System.Drawing;
using System.Collections.Generic;

namespace System.Windows.Forms
{
	public static class Overlap
	{
		public static void FindOverlappedEntities(EntityBox entityBox)
		{
			foreach (Entity entity in entityBox.GetEntities())
			{
				if (entity.Type != EntityType.Root && entity.Type != EntityType.Layer)
				{
					entity.Selected = false;
				}
			}

			List<Entity> entities = new List<Entity>();

			foreach (Entity entity in entityBox.GetEntities())
			{
				if (entity.Type != EntityType.Root && entity.Type != EntityType.Layer)
				{
					entities.Add(entity);
				}
			}

			for (int i = 0; i < entities.Count; i++)
			{
				for (int j = i + 1; j < entities.Count; j++)
				{
					Entity a = entities[i];
					Entity b = entities[j];

					if (EntitiesOverlap(a, b))
					{
						a.Selected = true;
						b.Selected = true;
					}
				}
			}

			entityBox.Invalidate();
		}

		private static bool EntitiesOverlap(Entity a, Entity b)
		{
			if (a.IsCell() && b.IsCell())
			{
				return CellsOverlap(a, b);
			}

			if (a.IsRegion() && b.IsRegion())
			{
				return RegionsOverlap(a, b);
			}

			if (a.IsCell() && b.IsRegion())
			{
				return CellRegionOverlap(a, b);
			}

			if (a.IsRegion() && b.IsCell())
			{
				return CellRegionOverlap(b, a);
			}

			if (a.IsWire() && b.IsWire())
			{
				return WiresOverlap(a, b);
			}

			if (a.IsVias() && b.IsVias())
			{
				return ViaseOverlap(a, b);
			}

			if ((a.IsCell() || a.IsRegion()) && b.IsVias())
			{
				return CellOrRegionContainsVias(a, b);
			}

			if (a.IsVias() && (b.IsCell() || b.IsRegion()))
			{
				return CellOrRegionContainsVias(b, a);
			}

			if ((a.IsCell() || a.IsRegion()) && b.IsWire())
			{
				return WireIntersectsCellOrRegion(b, a);
			}

			if (a.IsWire() && (b.IsCell() || b.IsRegion()))
			{
				return WireIntersectsCellOrRegion(a, b);
			}

			if (a.IsVias() && b.IsWire())
			{
				return ViaOnWire(a, b);
			}

			if (a.IsWire() && b.IsVias())
			{
				return ViaOnWire(b, a);
			}

			return false;
		}

		private static bool CellsOverlap(Entity a, Entity b)
		{
			float a_minx = a.LambdaX;
			float a_miny = a.LambdaY;
			float a_maxx = a.LambdaX + a.LambdaWidth;
			float a_maxy = a.LambdaY + a.LambdaHeight;

			float b_minx = b.LambdaX;
			float b_miny = b.LambdaY;
			float b_maxx = b.LambdaX + b.LambdaWidth;
			float b_maxy = b.LambdaY + b.LambdaHeight;

			if (a_maxx < b_minx || a_minx > b_maxx) return false;
			if (a_maxy < b_miny || a_miny > b_maxy) return false;

			if (a.PathPoints != null && a.PathPoints.Count > 0)
			{
				for (int i = 0; i < b.PathPoints.Count; i++)
				{
					if (PointInPoly(a.PathPoints.ToArray(), b.PathPoints[i]))
					{
						return true;
					}
				}
			}

			if (b.PathPoints != null && b.PathPoints.Count > 0)
			{
				for (int i = 0; i < a.PathPoints.Count; i++)
				{
					if (PointInPoly(b.PathPoints.ToArray(), a.PathPoints[i]))
					{
						return true;
					}
				}
			}

			return false;
		}

		private static bool RegionsOverlap(Entity a, Entity b)
		{
			if (a.PathPoints == null || a.PathPoints.Count == 0 || b.PathPoints == null || b.PathPoints.Count == 0)
			{
				return false;
			}

			float a_minx = float.MaxValue, a_miny = float.MaxValue, a_maxx = float.MinValue, a_maxy = float.MinValue;
			foreach (var p in a.PathPoints)
			{
				if (p.X < a_minx) a_minx = p.X;
				if (p.Y < a_miny) a_miny = p.Y;
				if (p.X > a_maxx) a_maxx = p.X;
				if (p.Y > a_maxy) a_maxy = p.Y;
			}

			float b_minx = float.MaxValue, b_miny = float.MaxValue, b_maxx = float.MinValue, b_maxy = float.MinValue;
			foreach (var p in b.PathPoints)
			{
				if (p.X < b_minx) b_minx = p.X;
				if (p.Y < b_miny) b_miny = p.Y;
				if (p.X > b_maxx) b_maxx = p.X;
				if (p.Y > b_maxy) b_maxy = p.Y;
			}

			if (a_maxx < b_minx || a_minx > b_maxx) return false;
			if (a_maxy < b_miny || a_miny > b_maxy) return false;

			for (int i = 0; i < a.PathPoints.Count; i++)
			{
				if (PointInPoly(b.PathPoints.ToArray(), a.PathPoints[i]))
				{
					return true;
				}
			}

			for (int i = 0; i < b.PathPoints.Count; i++)
			{
				if (PointInPoly(a.PathPoints.ToArray(), b.PathPoints[i]))
				{
					return true;
				}
			}

			return false;
		}

		private static bool CellRegionOverlap(Entity cell, Entity region)
		{
			if (region.PathPoints == null || region.PathPoints.Count == 0)
			{
				return false;
			}

			float cell_minx = cell.LambdaX;
			float cell_miny = cell.LambdaY;
			float cell_maxx = cell.LambdaX + cell.LambdaWidth;
			float cell_maxy = cell.LambdaY + cell.LambdaHeight;

			float region_minx = float.MaxValue, region_miny = float.MaxValue, region_maxx = float.MinValue, region_maxy = float.MinValue;
			foreach (var p in region.PathPoints)
			{
				if (p.X < region_minx) region_minx = p.X;
				if (p.Y < region_miny) region_miny = p.Y;
				if (p.X > region_maxx) region_maxx = p.X;
				if (p.Y > region_maxy) region_maxy = p.Y;
			}

			if (cell_maxx < region_minx || cell_minx > region_maxx) return false;
			if (cell_maxy < region_miny || cell_miny > region_maxy) return false;

			for (int i = 0; i < region.PathPoints.Count; i++)
			{
				if (PointInPoly(cell.PathPoints.ToArray(), region.PathPoints[i]))
				{
					return true;
				}
			}

			return false;
		}

		private static bool WiresOverlap(Entity a, Entity b)
		{
			if (!a.IsWire() || !b.IsWire()) return false;

			return LineIntersectsLine(
				new PointF(a.LambdaX, a.LambdaY),
				new PointF(a.LambdaEndX, a.LambdaEndY),
				new PointF(b.LambdaX, b.LambdaY),
				new PointF(b.LambdaEndX, b.LambdaEndY));
		}

		private static bool ViaseOverlap(Entity a, Entity b)
		{
			if (!a.IsVias() || !b.IsVias()) return false;

			float dx = a.LambdaX - b.LambdaX;
			float dy = a.LambdaY - b.LambdaY;
			float dist = (float)Math.Sqrt(dx * dx + dy * dy);

			return dist < 1.0f;
		}

		private static bool CellOrRegionContainsVias(Entity cellOrRegion, Entity vias)
		{
			if (!vias.IsVias()) return false;

			if (cellOrRegion.IsCell())
			{
				float minx = cellOrRegion.LambdaX;
				float miny = cellOrRegion.LambdaY;
				float maxx = cellOrRegion.LambdaX + cellOrRegion.LambdaWidth;
				float maxy = cellOrRegion.LambdaY + cellOrRegion.LambdaHeight;

				if (vias.LambdaX >= minx && vias.LambdaX <= maxx &&
					vias.LambdaY >= miny && vias.LambdaY <= maxy)
				{
					if (cellOrRegion.PathPoints != null && cellOrRegion.PathPoints.Count > 0)
					{
						return PointInPoly(cellOrRegion.PathPoints.ToArray(), new PointF(vias.LambdaX, vias.LambdaY));
					}
					return true;
				}
			}
			else if (cellOrRegion.IsRegion())
			{
				if (cellOrRegion.PathPoints != null && cellOrRegion.PathPoints.Count > 0)
				{
					return PointInPoly(cellOrRegion.PathPoints.ToArray(), new PointF(vias.LambdaX, vias.LambdaY));
				}
			}

			return false;
		}

		private static bool WireIntersectsCellOrRegion(Entity wire, Entity cellOrRegion)
		{
			if (!wire.IsWire()) return false;

			if (cellOrRegion.IsCell())
			{
				float minx = cellOrRegion.LambdaX;
				float miny = cellOrRegion.LambdaY;
				float maxx = cellOrRegion.LambdaX + cellOrRegion.LambdaWidth;
				float maxy = cellOrRegion.LambdaY + cellOrRegion.LambdaHeight;

				RectangleF rect = new RectangleF(minx, miny, maxx - minx, maxy - miny);

				if (LineIntersectsRect(
					new PointF(wire.LambdaX, wire.LambdaY),
					new PointF(wire.LambdaEndX, wire.LambdaEndY),
					rect))
				{
					if (cellOrRegion.PathPoints != null && cellOrRegion.PathPoints.Count > 0)
					{
						if (PointInPoly(cellOrRegion.PathPoints.ToArray(),
							new PointF(wire.LambdaX, wire.LambdaY)) ||
							PointInPoly(cellOrRegion.PathPoints.ToArray(),
							new PointF(wire.LambdaEndX, wire.LambdaEndY)))
						{
							return true;
						}
					}
					else
					{
						return true;
					}
				}
			}
			else if (cellOrRegion.IsRegion())
			{
				if (cellOrRegion.PathPoints != null && cellOrRegion.PathPoints.Count > 0)
				{
					for (int i = 0; i < cellOrRegion.PathPoints.Count; i++)
					{
						int next = (i + 1) % cellOrRegion.PathPoints.Count;
						if (LineIntersectsLine(
							new PointF(wire.LambdaX, wire.LambdaY),
							new PointF(wire.LambdaEndX, wire.LambdaEndY),
							cellOrRegion.PathPoints[i],
							cellOrRegion.PathPoints[next]))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		private static bool ViaOnWire(Entity vias, Entity wire)
		{
			if (!vias.IsVias() || !wire.IsWire()) return false;

			float dx = wire.LambdaEndX - wire.LambdaX;
			float dy = wire.LambdaEndY - wire.LambdaY;
			float lenSq = dx * dx + dy * dy;

			if (lenSq == 0) return false;

			float t = ((vias.LambdaX - wire.LambdaX) * dx + (vias.LambdaY - wire.LambdaY) * dy) / lenSq;

			float projX = wire.LambdaX + t * dx;
			float projY = wire.LambdaY + t * dy;

			float distX = vias.LambdaX - projX;
			float distY = vias.LambdaY - projY;
			float dist = (float)Math.Sqrt(distX * distX + distY * distY);

			return dist < 2.0f && t >= 0.0f && t <= 1.0f;
		}

		private static bool LineIntersectsLine(PointF l1p1, PointF l1p2, PointF l2p1, PointF l2p2)
		{
			float q = (l1p1.Y - l2p1.Y) * (l2p2.X - l2p1.X) - (l1p1.X - l2p1.X) * (l2p2.Y - l2p1.Y);
			float d = (l1p2.X - l1p1.X) * (l2p2.Y - l2p1.Y) - (l1p2.Y - l1p1.Y) * (l2p2.X - l2p1.X);

			if (d == 0)
			{
				return false;
			}

			float r = q / d;

			q = (l1p1.Y - l2p1.Y) * (l1p2.X - l1p1.X) - (l1p1.X - l2p1.X) * (l1p2.Y - l1p1.Y);
			float s = q / d;

			if (r < 0 || r > 1 || s < 0 || s > 1)
			{
				return false;
			}

			return true;
		}

		private static bool LineIntersectsRect(PointF p1, PointF p2, RectangleF r)
		{
			return LineIntersectsLine(p1, p2, new PointF(r.X, r.Y), new PointF(r.X + r.Width, r.Y)) ||
				   LineIntersectsLine(p1, p2, new PointF(r.X + r.Width, r.Y), new PointF(r.X + r.Width, r.Y + r.Height)) ||
				   LineIntersectsLine(p1, p2, new PointF(r.X + r.Width, r.Y + r.Height), new PointF(r.X, r.Y + r.Height)) ||
				   LineIntersectsLine(p1, p2, new PointF(r.X, r.Y + r.Height), new PointF(r.X, r.Y)) ||
				   (r.Contains(p1) && r.Contains(p2));
		}

		private static bool PointInPoly(PointF[] poly, PointF point)
		{
			int max_point = poly.Length - 1;
			float total_angle = GetAngle(
				poly[max_point].X, poly[max_point].Y,
				point.X, point.Y,
				poly[0].X, poly[0].Y);

			for (int i = 0; i < max_point; i++)
			{
				total_angle += GetAngle(
					poly[i].X, poly[i].Y,
					point.X, point.Y,
					poly[i + 1].X, poly[i + 1].Y);
			}

			return (Math.Abs(total_angle) > 0.000001);
		}

		private static float GetAngle(float Ax, float Ay,
			float Bx, float By, float Cx, float Cy)
		{
			float dot_product = DotProduct(Ax, Ay, Bx, By, Cx, Cy);

			float cross_product = CrossProductLength(Ax, Ay, Bx, By, Cx, Cy);

			return (float)Math.Atan2(cross_product, dot_product);
		}

		private static float DotProduct(float Ax, float Ay,
			float Bx, float By, float Cx, float Cy)
		{
			float BAx = Ax - Bx;
			float BAy = Ay - By;
			float BCx = Cx - Bx;
			float BCy = Cy - By;

			return (BAx * BCx + BAy * BCy);
		}

		private static float CrossProductLength(float Ax, float Ay,
			float Bx, float By, float Cx, float Cy)
		{
			float BAx = Ax - Bx;
			float BAy = Ay - By;
			float BCx = Cx - Bx;
			float BCy = Cy - By;

			return (BAx * BCy - BAy * BCx);
		}
	}
}
