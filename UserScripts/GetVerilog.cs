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

			string text = GetVerilogText(top, instances, wires, true);
			File.WriteAllText(verilog_name, text, Encoding.UTF8);
		}

		/// <summary>
		/// Wires are obtained by combining segments by traverse.
		/// </summary>
		static List<FutureWire> GetWires (List<Entity> ents)
		{
			List<FutureWire> wires = new List<FutureWire>();

			// The wire name is taken by concatenating all segment names with a space.
			// If the result is an empty string, then the wire name is generated as `w1`, `w2` and so on.

			return wires;
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

		class MyRect
		{
			public float x;
			public float y;
			public float w;
			public float h;

			public MyRect (float x, float y, float w, float h)
			{
				this.x = x;
				this.y = y;
				this.w = w;
				this.h = h;
			}

			public bool Contains (float px, float py)
			{
				return (px > x && px < (x + w) && py > y && py < (y + h));
			}
		}


		/// <summary>
		/// The script does not check connectivity and does not make any special checks at all.
		/// All errors can be checked later when using the generated HDL in your favorite CAD.
		/// </summary>
		static string GetVerilogText (FutureInstance top, List<FutureInstance> instances, List<FutureWire> wires, bool compact)
		{
			string text = "";

			// Top

			text += "module " + top.module_name + " ( ";
			foreach (var p in top.ports)
			{
				text += " " + p.Label + ",";
			}
			text = text.Remove(text.Length - 1);
			text += ");\r\n";
			text += "\r\n";

			foreach (var p in top.ports)
			{
				text += "\t" + p.Type.ToString().Replace("Vias", "").ToLower() + " wire " + p.Label + ";\r\n";
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
					var wire = GetConnection(p, wires);

					if (!compact)
						text += "\r\n\t\t";

					if (wire != null)
					{
						text += "." + p.Label + "(" + wire.name + ")";
					}
					else
					{
						text += "." + p.Label + "()";
					}

					text += ",";
				}

				text = text.Remove(text.Length - 1);

				text += " );\r\n";

				if (!compact)
					text += "\r\n";
			}

			// End

			text += "endmodule // " + top.module_name;

			return text;
		}

		static FutureWire GetConnection (Entity port, List<FutureWire> wires)
		{
			FutureWire wire = null;
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
