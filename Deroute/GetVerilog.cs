// Deroute XML -> Verilog converter.

using System;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Drawing;
using System.Windows.Forms;

namespace DerouteSharp
{
	class GetVerilog
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

		public static string EntitiesToVerilogSource(EntityBox ebox, string top_module_name)
		{
			FutureInstance top = new FutureInstance();
			List<FutureInstance> instances;
			List<FutureWire> wires;

			// Load the original XML

			List<Entity> ents = ebox.GetEntities();

			top.module_name = top_module_name;
			top.ports = GetTopPorts(ents);

			// Get wire list

			wires = GetWires(ebox, ents);

			// Get a list of module instances

			string mod_prefix = top.module_name.ToLower() + "_";
			instances = GetInstances(ents, mod_prefix);

			// Output the verilog

			string text = GetVerilogText(top, instances, wires, true) + GetModulesVerilog(instances);
			return text;
		}

		/// <summary>
		/// Wires are obtained by combining segments by traverse.
		/// </summary>
		static List<FutureWire> GetWires (EntityBox ebox, List<Entity> ents)
		{
			List<FutureWire> wires = new List<FutureWire>();

			int cnt = 1;

			foreach (var wire in ents)
			{
				if (wire.IsWire() && !IsInCollectionAlready(wire, wires))
				{
					wire.Selected = true;
					ebox.TraversalSelection(1);

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
							RectangleF rect = new RectangleF (c.LambdaX, c.LambdaY, c.LambdaWidth, c.LambdaHeight);
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
					RectangleF rect = new RectangleF(cell.LambdaX, cell.LambdaY, cell.LambdaWidth, cell.LambdaHeight);
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
						var const_wire = IsConstantScalar(wire);

						text += "." + p.Label + "(" + (const_wire != null ? const_wire : wire.name) + "), ";
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

		static string IsConstantScalar (FutureWire wire)
		{
			foreach (var ent in wire.parts)
			{
				if (ent.Type == EntityType.ViasPower)
				{
					return "1'b1";
				}

				if (ent.Type == EntityType.ViasGround)
				{
					return "1'b0";
				}
			}
			return null;
		}
	}
}
