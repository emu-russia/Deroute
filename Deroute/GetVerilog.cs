// Deroute XML -> Verilog converter.

using System;
using System.Collections.Generic;
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

		public static string EntitiesToVerilogSource(EntityBox ebox, string top_module_name, ref bool abort)
		{
			FutureInstance top = new FutureInstance();
			List<FutureInstance> instances;
			List<FutureWire> wires;

			// Load the original XML

			List<Entity> ents = ebox.GetEntities();

			top.module_name = top_module_name;
			top.ports = GetTopPorts(ents, ref abort);
			if (abort)
				return null;

			// Get wire list

			wires = GetWires(ebox, ents, ref abort);
			if (abort)
				return null;

			// Get a list of module instances

			string mod_prefix = top.module_name.ToLower() + "_";
			instances = GetInstances(ents, mod_prefix, ref abort);
			if (abort)
				return null;

			var sanity_text = SanityCheck(top, instances, wires);

			// Output the verilog

			string text = GetVerilogText(top, instances, wires, true, ref sanity_text, ref abort) + GetModulesVerilog(instances);
			if (abort)
				return null;
			if (sanity_text != "")
			{
				text += "\n\n" + sanity_text;
			}
			return text;
		}

		/// <summary>
		/// Wires are obtained by combining segments by traverse.
		/// </summary>
		static List<FutureWire> GetWires (EntityBox ebox, List<Entity> ents, ref bool abort)
		{
			List<FutureWire> wires = new List<FutureWire>();

			int cnt = 1;

			foreach (var wire in ents)
			{
				if (abort)
				{
					Console.WriteLine("GetWires Aborted");
					return null;
				}

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

					fw.name = fw.name.Trim();

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
		static List<FutureInstance> GetInstances (List<Entity> ents, string common_prefix, ref bool abort)
		{
			List<FutureInstance> instances = new List<FutureInstance>();

			int cnt = 1;

			foreach (var ent in ents)
			{
				if (abort)
				{
					Console.WriteLine("GetInstances Aborted");
					return null;
				}

				if (ent.IsCell())
				{
					FutureInstance inst = new FutureInstance();

					// The instance name is taken from the `Label` property of the cell/block.
					// The first word is the module name, the second word (if any) is the instance name.
					// If there is no name, then a name of the form `g1`, `g2` and so on is generated.

					string label = ent.Label.Trim();

					if (label == "")
					{
						label = "Unknown";		// Instead of error
					}

					var pair = label.Split(' ');

					inst.cell = ent;
					inst.module_name = common_prefix + pair[0].Trim();
					inst.inst_name = pair.Length == 1 ? "g" + cnt.ToString() : pair[1].Trim();
					inst.ports = EntityBox.GetCellPorts(ent, ents);

					instances.Add(inst);

					cnt++;
				}
			}

			return instances;
		}

		/// <summary>
		/// The ports for the top module are all input/output/inout vias NOT of cells
		/// </summary>
		static List<Entity> GetTopPorts (List<Entity> ents, ref bool abort)
		{
			List<Entity> ports = new List<Entity>();

			foreach (var p in ents)
			{
				if (abort)
				{
					Console.WriteLine("GetTopPorts Aborted");
					return null;
				}

				if (p.IsPort())
				{
					bool foundWithin = false;

					foreach (var c in ents)
					{
						if (abort)
						{
							Console.WriteLine("GetTopPorts Aborted");
							return null;
						}

						if (c.IsCell())
						{
							var cell_ports = EntityBox.GetCellPorts(c, ents);
							foreach (var port in cell_ports)
							{
								if (port.LambdaX == p.LambdaX && port.LambdaY == p.LambdaY)
								{
									foundWithin = true;
									break;
								}
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
		/// The script does not check connectivity and does not make any special checks at all.
		/// All errors can be checked later when using the generated HDL in your favorite CAD.
		/// </summary>
		static string GetVerilogText (FutureInstance top, List<FutureInstance> instances, List<FutureWire> wires, bool compact, ref string sanity_text, ref bool abort)
		{
			string text = "";

			// Top

			text += GetModuleHeaderText(top);

			// Wires

			text += "\t// Wires\r\n";
			text += "\r\n";

			foreach (var wire in wires)
			{
				text += "\twire " + wire.name + ";\r\n";
			}
			text += "\r\n";

			// Top -> Wires

			foreach (var p in top.ports)
			{
				if (abort)
				{
					Console.WriteLine("GetVerilogText assign Aborted");
					return null;
				}

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

			// Instancies

			text += "\t// Instances\r\n";
			text += "\r\n";

			foreach (var inst in instances)
			{
				if (abort)
				{
					Console.WriteLine("GetVerilogText Instances Aborted");
					return null;
				}

				if (inst.ports.Count == 0)
					continue;

				text += "\t" + inst.module_name + " " + inst.inst_name + " (";

				foreach (var p in inst.ports)
				{
					if (p.Label == "")
					{
						Console.WriteLine("ERROR: Cell {0}:{1} has unnamed port!", inst.module_name, inst.inst_name);
						sanity_text += "// ERROR: Cell " + inst.module_name + ":" + inst.inst_name + " has unnamed port!\n";
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
						Console.WriteLine("WARNING: Cell {0}:{1} port {2} not connected.", inst.module_name, inst.inst_name, p.Label);
						sanity_text += "// WARNING: Cell " + inst.module_name + ":" + inst.inst_name + " port " + p.Label + " not connected.\n";
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
				var inst = kv.Value;
				if (inst.ports.Count == 0)
					continue;
				text += GetModuleHeaderText(inst);
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

		static string SanityCheck (FutureInstance top, List<FutureInstance> instances, List<FutureWire> wires)
		{
			string text = "";
			foreach (var wire in wires)
			{
				int input_ports = 0;
				int output_ports = 0;
				foreach (var e in wire.parts)
				{
					if (top.ports.Contains(e))
					{
						if (e.Type == EntityType.ViasOutput)
							input_ports++;
						if (e.Type == EntityType.ViasInput)
							output_ports++;
					}
					else
					{
						if (e.Type == EntityType.ViasOutput)
							output_ports++;
						if (e.Type == EntityType.ViasInput)
							input_ports++;
					}
					// ViasInout ??
				}
				if (output_ports > 1)
				{
					Console.WriteLine("ERROR: conflicting wire {0}!!!", wire.name);
					text += "// ERROR: conflicting wire " + wire.name + "\n";
				}
				if (output_ports == 0 && input_ports > 0)
				{
					Console.WriteLine("ERROR: floating wire {0}!!!", wire.name);
					text += "// ERROR: floating wire " + wire.name + "\n";
				}
				if (output_ports == 1 && input_ports == 0)
				{
					Console.WriteLine("WARNING: wire not driving anything {0}!!!", wire.name);
					text += "// WARNING: wire not driving anything " + wire.name + "\n";
				}
			}
			return text;
		}
	}
}
