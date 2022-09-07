// Deroute XML -> Verilog converter.

using System;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GetVerilog
{
	class Program
	{
		class FutureWire
		{
			public List<Entity> parts;
			public string name;
		}

		class FutureInstance
		{
			public Entity cell;
			public List<Entity> ports;
			public string module_name;
			public string inst_name;
		}

		static void Main(string[] args)
		{
			FutureInstance top = new FutureInstance();
			List<FutureInstance> instances;
			List<FutureWire> wires;

			// Parse arguments

			if (args.Length < 2)
			{
				Console.WriteLine ("Use: GetVerilog.exe <source.xml(z)> <dest.v>");
				return;
			}

			string xml_name = args[0];
			string verilog_name = args[1];

			Console.WriteLine ("{0} -> {1}", xml_name, verilog_name);

			// Load the original XML

			List<Entity> ents = LoadEntitiesXml(xml_name);

			top.module_name = Path.GetFileNameWithoutExtension(xml_name);
			top.ports = GetTopPorts(ents);

			// Get wire list

			wires = GetWires(ents);

			// Get a list of module instances

			instances = GetInstances(ents, top.module_name.ToLower() + "_");

			// Output the verilog

			string text = GetVerilogText(top, instances, wires, true) + GetModulesVerilog(instances);
			File.WriteAllText(verilog_name, text, Encoding.ASCII);
		}

		/// <summary>
		/// Wires are obtained by combining segments by traverse.
		/// </summary>
		static List<FutureWire> GetWires (List<Entity> ents)
		{
			List<FutureWire> wires = new List<FutureWire>();

			int cnt = 1;

			foreach (var wire in ents)
			{
				if (wire.IsWire() && !IsInCollectionAlready(wire, wires))
				{
					wire.Selected = true;
					TraversalSelection(ents, 1);

					FutureWire fw = new FutureWire();

					fw.parts = GetSelected(ents);
					ClearSelected(ents);
					fw.name = "";

					// The wire name is taken by concatenating all segment names with a underscore.

					foreach (var part in fw.parts)
					{
						if (part.IsWire() && part.Label != "")
						{
							fw.name += part.Label + "_";
						}
					}

					if (fw.name.Length != 0)
					{
						fw.name = fw.name.Remove(fw.name.Length - 1);
					}

					// If the result is an empty string, then the wire name is generated as `w1`, `w2` and so on.

					if (fw.name == "")
					{
						fw.name = "w" + cnt.ToString();
					}

					wires.Add(fw);

					cnt++;
				}
			}

			return wires;
		}

		static List<Entity> GetSelected(List<Entity> ents)
		{
			List<Entity> res = new List<Entity>();

			foreach (var ent in ents)
			{
				if (ent.Selected)
				{
					res.Add (ent);
				}
			}

			return res;
		}

		static void ClearSelected(List<Entity> ents)
		{
			foreach (var ent in ents)
			{
				ent.Selected = false;
			}
		}

		static bool IsInCollectionAlready(Entity wire, List<FutureWire> wires)
		{
			foreach (var w in wires)
			{
				if (w.parts.Contains(wire))
					return true;
			}
			return false;
		}

		/// <summary>
		/// All cells (entities of `Cell` type) and custom blocks (entities of `Unit` type) become module instances.
		/// </summary>
		static List<FutureInstance> GetInstances (List<Entity> ents, string common_prefix)
		{
			List<FutureInstance> instances = new List<FutureInstance>();

			int cnt = 1;

			foreach (var ent in ents)
			{
				if (ent.IsCell())
				{
					FutureInstance inst = new FutureInstance();

					// The instance name is taken from the `Label` property of the cell/block.
					// The first word is the module name, the second word (if any) is the instance name.
					// If there is no name, then a name of the form `g1`, `g2` and so on is generated.

					string label = ent.Label;

					if (label == "")
					{
						label = "Unknown";		// Instead of error
					}

					var pair = label.Split(' ');

					inst.cell = ent;
					inst.module_name = common_prefix + pair[0];
					inst.inst_name = pair.Length == 1 ? "g" + cnt.ToString() : pair[1];
					inst.ports = GetPorts(ent, ents);

					instances.Add(inst);

					cnt++;
				}
			}

			return instances;
		}

		/// <summary>
		/// The ports for the top module are all input/output/inout vias NOT of cells
		/// </summary>
		static List<Entity> GetTopPorts (List<Entity> ents)
		{
			List<Entity> ports = new List<Entity>();

			foreach (var p in ents)
			{
				if (IsPort(p))
				{
					bool foundWithin = false;

					foreach (var c in ents)
					{
						if (c.IsCell())
						{
							MyRect rect = new MyRect(c.LambdaX, c.LambdaY, c.LambdaWidth, c.LambdaHeight);
							if (rect.Contains(p.LambdaX, p.LambdaY))
							{
								foundWithin = true;
								break;
							}
						}
					}

					if (!foundWithin)
					{
						ports.Add(p);
					}
				}
			}

			return ports;
		}

		/// <summary>
		/// All input/output/input vias within a cell/block become ports
		/// </summary>
		static List<Entity> GetPorts (Entity cell, List<Entity> ents)
		{
			List<Entity> ports = new List<Entity>();

			foreach (var ent in ents)
			{
				if (IsPort(ent))
				{
					MyRect rect = new MyRect (cell.LambdaX, cell.LambdaY, cell.LambdaWidth, cell.LambdaHeight);
					if (rect.Contains(ent.LambdaX, ent.LambdaY))
					{
						ports.Add(ent);
					}
				}
			}

			return ports;
		}

		static bool IsPort(Entity ent)
		{
			return ent.Type == EntityType.ViasInput || ent.Type == EntityType.ViasOutput || ent.Type == EntityType.ViasInout;
		}

		/// <summary>
		/// The script does not check connectivity and does not make any special checks at all.
		/// All errors can be checked later when using the generated HDL in your favorite CAD.
		/// </summary>
		static string GetVerilogText (FutureInstance top, List<FutureInstance> instances, List<FutureWire> wires, bool compact)
		{
			string text = "";

			// Top

			text += GetModuleHeaderText(top);

			// Top -> Wires

			foreach (var p in top.ports)
			{
				var wire = GetConnection(p, wires);

				if (wire != null)
				{
					if (p.Type == EntityType.ViasInput)
					{
						text += "\tassign " + wire.name + " = " + p.Label + ";\r\n";
					}
					else
					{
						text += "\tassign " + p.Label + " = " + wire.name + ";\r\n";
					}
				}
			}
			text += "\r\n";

			// Wires

			text += "// Wires\r\n";
			text += "\r\n";

			foreach (var wire in wires)
			{
				text += "\twire " + wire.name + ";\r\n";
			}
			text += "\r\n";

			// Instancies

			text += "// Instances\r\n";
			text += "\r\n";

			foreach (var inst in instances)
			{
				text += "\t" + inst.module_name + " " + inst.inst_name + " (";

				foreach (var p in inst.ports)
				{
					if (p.Label == "")
					{
						Console.WriteLine("ERROR: Cell {0}:{1} has unnamed port!", inst.module_name, inst.inst_name);
						continue;
					}

					var wire = GetConnection(p, wires);

					if (!compact)
						text += "\r\n\t\t";

					if (wire != null)
					{
						text += "." + p.Label + "(" + wire.name + "), ";
					}
					else
					{
						//text += "." + p.Label + "()";
						Console.WriteLine("WARNING: Cell {0}:{1} port {2} not connected.", inst.module_name, inst.inst_name, p.Label);
					}
				}

				text = text.Remove(text.Length - 2);

				text += " );\r\n";

				if (!compact)
					text += "\r\n";
			}

			// End

			text += "endmodule // " + top.module_name;

			return text;
		}

		static string GetModuleHeaderText(FutureInstance inst)
		{
			string text = "";

			text += "module " + inst.module_name + " ( ";
			foreach (var p in inst.ports)
			{
				text += " " + p.Label + ",";
			}
			text = text.Remove(text.Length - 1);
			text += ");\r\n";
			text += "\r\n";

			foreach (var p in inst.ports)
			{
				text += "\t" + p.Type.ToString().Replace("Vias", "").ToLower() + " wire " + p.Label + ";\r\n";
			}
			text += "\r\n";

			return text;
		}

		static string GetModulesVerilog(List<FutureInstance> instances)
		{
			string text = "";

			Dictionary<string, FutureInstance> modules = new Dictionary<string, FutureInstance>();

			foreach (var inst in instances)
			{
				if (modules.ContainsKey(inst.module_name))
					continue;

				modules.Add(inst.module_name, inst);
			}

			text += "\r\n\r\n";
			text += "// Module Definitions [It is possible to wrap here on your primitives]\r\n";
			text += "\r\n";

			foreach (var kv in modules)
			{
				text += GetModuleHeaderText(kv.Value);
				text += "endmodule // " + kv.Key + "\r\n\r\n";
			}

			return text;
		}

		static FutureWire GetConnection (Entity port, List<FutureWire> wires)
		{
			FutureWire wire = null;

			foreach (FutureWire w in wires)
			{
				if (w.parts.Contains (port))
				{
					wire = w;
					break;
				}
			}

			return wire;
		}

		static List<Entity> LoadEntitiesXml(string filename)
		{
			List<Entity> res = null;
			if (Path.GetExtension(filename).ToLower() == ".xmlz")
			{
				string temp_xml_dir = GetTemporaryDirectory();
				ZipFile.ExtractToDirectory(filename, temp_xml_dir);
				DirectoryInfo di = new DirectoryInfo(temp_xml_dir);
				bool first = true;
				foreach (FileInfo file in di.GetFiles())
				{
					if (first)
					{
						res = Unserialize(file.FullName);
						first = false;
					}
					file.Delete();
				}
				Directory.Delete(temp_xml_dir);
			}
			else
			{
				res = Unserialize(filename);
			}
			return res;
		}

		static string GetTemporaryDirectory()
		{
			string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempDirectory);
			return tempDirectory;
		}

		static List<Entity> Unserialize(string filename)
		{
			XmlSerializer ser = new XmlSerializer(typeof(List<Entity>));
			Entity root = new Entity();

			using (FileStream fs = new FileStream(filename, FileMode.Open))
			{
				List<Entity> newList = (List<Entity>)ser.Deserialize(fs);
				root.Children.AddRange(newList);
			}

			return GetEntities (root);
		}

		/// <summary>
		/// Get a list of all entities. The hierarchy is destroyed (you get a regular linear list).
		/// </summary>
		/// <returns>A list of all entities.</returns>
		static List<Entity> GetEntities(Entity root)
		{
			List<Entity> list = new List<Entity>();

			foreach (var entity in root.Children)
			{
				GetEntitiesRecursive(entity, list);
			}

			return list;
		}

		static void GetEntitiesRecursive(Entity parent, List<Entity> list)
		{
			list.Add(parent);

			foreach (var entity in parent.Children)
			{
				GetEntitiesRecursive(entity, list);
			}
		}


		#region "Traverse"

		const float traverseLambdaDelta = 0.7F;      // Lambdas

		static bool IsViasInWire(Entity vias, Entity wire)
		{
			float delta = traverseLambdaDelta;

			MyPoint start = new MyPoint(wire.LambdaX, wire.LambdaY);
			MyPoint end = new MyPoint(wire.LambdaEndX, wire.LambdaEndY);

			MyRect rect = new MyRect(
				vias.LambdaX - delta, vias.LambdaY - delta,
				2 * delta, 2 * delta);

			return LineIntersectsRect(start, end, rect);
		}

		static void SelectTraverse(Entity source,
									  List<Entity> ents,
									  int Tier,
									  int TierMax,
									  int Depth)
		{
			MyPoint[] rect1 = new MyPoint[4];
			MyPoint[] rect2 = new MyPoint[4];
			MyPoint restrictedStart = new MyPoint(0, 0);
			MyPoint restrictedEnd = new MyPoint(0, 0);

			if (Tier >= TierMax)
				return;

			//
			// Wire joint Vias/Wire
			//

			if (source.IsWire())
			{
				float maxDist = traverseLambdaDelta;
				float dist;
				List<Entity> viases = new List<Entity>();

				//
				// Get not selected entities in delta range for Start point
				//
				// Get not selected entities in delta range for End point
				//

				float dx = Math.Abs(source.LambdaEndX - source.LambdaX);
				float dy = Math.Abs(source.LambdaEndY - source.LambdaY);
				bool Vert = dx < dy;

				foreach (Entity entity in ents)
				{
					if (source.TraverseBlackList != null && entity.TraverseBlackList != null)
					{
						if (source.TraverseBlackList.Contains(entity.Type) || entity.TraverseBlackList.Contains(source.Type))
							continue;
					}

					if (entity.Selected == false)
					{
						//
						// Wire -> Vias
						// 

						if (entity.IsVias())
						{
							if (IsViasInWire(entity, source))
								viases.Add(entity);
						}

						//
						// Wire -> Wire
						//

						else if (entity.IsWire())
						{
							MyPoint pointStart = new MyPoint(entity.LambdaX, entity.LambdaY);
							MyPoint pointEnd = new MyPoint(entity.LambdaEndX, entity.LambdaEndY);

							dist = (float)Math.Sqrt(Math.Pow(entity.LambdaX - source.LambdaX, 2) +
													 Math.Pow(entity.LambdaY - source.LambdaY, 2));

							if (dist < maxDist)
							{
								entity.Selected = true;
								SelectTraverse(entity, ents, Tier, TierMax, Depth + 1);
							}

							dist = (float)Math.Sqrt(Math.Pow(entity.LambdaX - source.LambdaEndX, 2) +
													 Math.Pow(entity.LambdaY - source.LambdaEndY, 2));

							if (dist < maxDist && entity.Selected == false)
							{
								entity.Selected = true;
								SelectTraverse(entity, ents, Tier, TierMax, Depth + 1);
							}

							dist = (float)Math.Sqrt(Math.Pow(entity.LambdaEndX - source.LambdaEndX, 2) +
													 Math.Pow(entity.LambdaEndY - source.LambdaEndY, 2));

							if (dist < maxDist && entity.Selected == false)
							{
								entity.Selected = true;
								SelectTraverse(entity, ents, Tier, TierMax, Depth + 1);
							}

							dist = (float)Math.Sqrt(Math.Pow(entity.LambdaEndX - source.LambdaX, 2) +
													 Math.Pow(entity.LambdaEndY - source.LambdaY, 2));

							if (dist < maxDist && entity.Selected == false)
							{
								entity.Selected = true;
								SelectTraverse(entity, ents, Tier, TierMax, Depth + 1);
							}

						}
					}
				}           // foreach


				//
				// Process viases
				//

				foreach (Entity entity in viases)
				{
					entity.Selected = true;
					SelectTraverse(entity, ents, Tier, TierMax, Depth + 1);
				}

			}

			//
			// Vias joint Cell/Wire
			//

			else if (source.IsVias())
			{
				MyPoint point = new MyPoint(source.LambdaX, source.LambdaY);
				MyPoint[] rect = new MyPoint[4];
				List<Entity> wires = new List<Entity>();
				List<Entity> cells = new List<Entity>();

				rect[0] = new MyPoint();
				rect[1] = new MyPoint();
				rect[2] = new MyPoint();
				rect[3] = new MyPoint();

				//
				// Collect all wires/cells by vias intersections
				//

				foreach (Entity entity in ents)
				{
					if (source.TraverseBlackList != null && entity.TraverseBlackList != null)
					{
						if (source.TraverseBlackList.Contains(entity.Type) || entity.TraverseBlackList.Contains(source.Type))
							continue;
					}

					if (entity.Selected == false)
					{
						//
						// Vias -> Wire
						//

						if (entity.IsWire())
						{
							if (IsViasInWire(source, entity))
								wires.Add(entity);
						}

						//
						// Vias -> Cell
						//

						else if (entity.IsCell())
						{
							rect[0].X = entity.LambdaX;
							rect[0].Y = entity.LambdaY;

							rect[1].X = entity.LambdaX;
							rect[1].Y = entity.LambdaY + entity.LambdaHeight;

							rect[2].X = entity.LambdaX + entity.LambdaWidth;
							rect[2].Y = entity.LambdaY + entity.LambdaHeight;

							rect[3].X = entity.LambdaX + entity.LambdaWidth;
							rect[3].Y = entity.LambdaY;

							if (PointInPoly(rect, point))
								cells.Add(entity);
						}
					}
				}

				//
				// Process
				//

				foreach (Entity entity in wires)
				{
					entity.Selected = true;
					SelectTraverse(entity, ents, Tier, TierMax, Depth + 1);

					//
					// Only single child
					//

					break;
				}

				foreach (Entity entity in cells)
				{
					entity.Selected = true;
					SelectTraverse(entity, ents, Tier + 1, TierMax, Depth + 1);

					//
					// Only single child
					//

					break;
				}

			}
		}

		static void TraversalSelection(List<Entity> ents, int TierMax)
		{
			List<Entity> selectedEnts = new List<Entity>();

			foreach (Entity entity in ents)
			{
				if ((entity.IsCell() || entity.IsVias() || entity.IsWire())
					 && entity.Selected)
					selectedEnts.Add(entity);
			}

			foreach (Entity entity in selectedEnts)
			{
				SelectTraverse(entity, ents, 0, TierMax, 0);
			}
		}

		#endregion "Traverse"

		
		#region "Geometry"

		class MyPoint
		{
			public float X;
			public float Y;

			public MyPoint()
			{
				X = 0;
				Y = 0;
			}

			public MyPoint (float x, float y)
			{
				X = x;
				Y = y;
			}
		}

		class MyRect
		{
			public float X;
			public float Y;
			public float Width;
			public float Height;

			public MyRect (float x, float y, float w, float h)
			{
				X = x;
				Y = y;
				Width = w;
				Height = h;
			}

			public bool Contains (float px, float py)
			{
				return (px > X && px < (X + Width) && py > Y && py < (Y + Height));
			}
		}

		static bool LineIntersectsRect(MyPoint p1, MyPoint p2, MyRect r)
		{
			return LineIntersectsLine(p1, p2, new MyPoint(r.X, r.Y), new MyPoint(r.X + r.Width, r.Y)) ||
				   LineIntersectsLine(p1, p2, new MyPoint(r.X + r.Width, r.Y), new MyPoint(r.X + r.Width, r.Y + r.Height)) ||
				   LineIntersectsLine(p1, p2, new MyPoint(r.X + r.Width, r.Y + r.Height), new MyPoint(r.X, r.Y + r.Height)) ||
				   LineIntersectsLine(p1, p2, new MyPoint(r.X, r.Y + r.Height), new MyPoint(r.X, r.Y)) ||
				   (r.Contains(p1.X, p1.Y) && r.Contains(p2.X, p2.Y));
		}

		static bool LineIntersectsLine(MyPoint l1p1, MyPoint l1p2, MyPoint l2p1, MyPoint l2p2)
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

		static bool PointInPoly(MyPoint[] poly, MyPoint point)
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

		static float GetAngle(float Ax, float Ay,
			float Bx, float By, float Cx, float Cy)
		{
			float dot_product = DotProduct(Ax, Ay, Bx, By, Cx, Cy);

			float cross_product = CrossProductLength(Ax, Ay, Bx, By, Cx, Cy);

			return (float)Math.Atan2(cross_product, dot_product);
		}

		static float DotProduct(float Ax, float Ay,
			float Bx, float By, float Cx, float Cy)
		{
			float BAx = Ax - Bx;
			float BAy = Ay - By;
			float BCx = Cx - Bx;
			float BCy = Cy - By;

			return (BAx * BCx + BAy * BCy);
		}

		static float CrossProductLength(float Ax, float Ay,
			float Bx, float By, float Cx, float Cy)
		{
			float BAx = Ax - Bx;
			float BAy = Ay - By;
			float BCx = Cx - Bx;
			float BCy = Cy - By;

			return (BAx * BCy - BAy * BCx);
		}

		#endregion "Geometry"

	}
}

/// <summary>
/// Make a class stub to use the parent EntityBox for entities.
/// </summary>
public class EntityBox
{
	public void Invalidate()
	{}

	public void SortEntities()
	{}

	public void LabelEdited(Entity entity)
	{}
}
