using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;
using System.Xml.Serialization;
using System.Drawing;

/*
Features of saving entities as Netlist:
- All vias are nodes.
- Connections of wires between vias are edges. A multi-segment wire becomes a single wire.
- Cells and units - group
- Cell vias are grouped in the last step
*/

namespace DerouteSharp
{
	/// <summary>
	/// This class exports meaningful entities as a graph (in GraphML format).
	/// Subsequently, the graph can be opened in utilities like yEd and can be arranged as required.
	/// </summary>
	public class GraphMLExport
	{
		private const float traverseLambdaDelta = 0.7F;   // lambdas

		/// <summary>
		/// The raw node of a graph, which is obtained from the vias of the original set of entities.
		/// </summary>
		internal class FutureNode
		{
			public string name = "";
			public Entity baseVias;
			public int id = 0;
		}

		/// <summary>
		/// Raw edge of the graph, which is obtained from the initial data of entities. The edges with open beginnings or ends do not end up in the final collection of the graph.
		/// </summary>
		internal class FutureEdge
		{
			public string name = "";
			public List<Entity> segments = new List<Entity>();
			public FutureNode from;     // can be null (open-begin)
			public FutureNode to;   // can be null (open-end)
			public int id = 0;
		}

		public static string ExportEntitiesNetlist (List<Entity> entities, float ViasSize)
		{
			Console.WriteLine("\nExportEntitiesNetlist");

			// FutureNodes + FutureEdges => GraphModel.

			List<FutureNode> nodes = new List<FutureNode>();
			List<FutureEdge> edges = new List<FutureEdge>();

			// STEP 1: Get nodes

			// Get a list of all vias that will become nodes of the graph.

			int node_counter = 0;

			foreach (Entity ent in entities)
			{
				if (ent.IsVias())
				{
					FutureNode node = new FutureNode();
					node.baseVias = ent;
					node.name = ent.Label;
					node.id = node_counter;
					nodes.Add(node);
					node_counter++;
				}
			}

			// DEBUG: Output a list of future nodes of the graph

			foreach (FutureNode node in nodes)
			{
				Console.WriteLine("node " + node.id.ToString() + ": `" + node.name + "`, pos: " + node.baseVias.LambdaX + ", " + node.baseVias.LambdaY);
			}

			// STEP 2: Get edges

			// Get a set of graph edges from wires (no connections yet)

			int edge_counter = 0;

			foreach (Entity ent in entities)
			{
				if (ent.IsWire() && !WireExistsInFutureEdges(edges, ent))
				{
					FutureEdge edge = new FutureEdge();
					TraverseWire (edges, nodes, edge, ent, entities);
					edge.name = edge.name.Trim();
					edge.id = edge_counter;
					edges.Add(edge);
					edge_counter++;
				}
			}

			// DEBUG: Output all edges before connections.

			Console.WriteLine("BEFORE CONNECT:");

			foreach (FutureEdge edge in edges)
			{
				Console.WriteLine("edge " + edge.id.ToString() + ": `" + edge.name + "`, segments: " + edge.segments.Count.ToString());
			}

			// Connect the ends of the edges with the corresponding vias, which will become the nodes of the graph (establish connections)

			foreach (FutureEdge edge in edges)
			{
				// There could be two options here:
				// - If an edge consists of one segment, it is trivial to find two nodes corresponding to the beginning and the end
				// - If the edge has several segments - find the start node corresponding to the beginning or the end of the first segment and find the end node corresponding to the beginning or the end of the last segment.

				if (edge.segments.Count == 0)
				{
					throw new Exception("There is not an edge without segments");
				}

				if (edge.segments.Count == 1)
				{
					PointF from = new PointF(edge.segments.First().LambdaX, edge.segments.First().LambdaY);
					PointF to = new PointF(edge.segments.First().LambdaEndX, edge.segments.First().LambdaEndY);

					edge.from = FindNode(nodes, from);
					edge.to = FindNode(nodes, to);
				}
				else
				{
					PointF beginFrom = new PointF(edge.segments.First().LambdaX, edge.segments.First().LambdaY);
					PointF beginTo = new PointF(edge.segments.First().LambdaEndX, edge.segments.First().LambdaEndY);
					PointF endFrom = new PointF(edge.segments.Last().LambdaX, edge.segments.Last().LambdaY);
					PointF endTo = new PointF(edge.segments.Last().LambdaEndX, edge.segments.Last().LambdaEndY);

					edge.from = FindNode(nodes, beginFrom);
					if (edge.from == null)
					{
						edge.from = FindNode(nodes, beginTo);
					}

					edge.to = FindNode(nodes, endFrom);
					if (edge.to == null)
					{
						edge.to = FindNode(nodes, endTo);
					}
				}
			}

			// DEBUG: Output all edges for debugging.

			Console.WriteLine("AFTER CONNECT:");

			foreach (FutureEdge edge in edges)
			{
				if (edge.from == null || edge.to == null)
				{
					if (edge.from == null && edge.to != null)
					{
						Console.WriteLine("Open-Begin edge " + edge.id.ToString() + ": `" + edge.name + "`, from: NONE, to: " + edge.to.name);
					}
					else if (edge.from != null && edge.to == null)
					{
						Console.WriteLine("Open-End edge " + edge.id.ToString() + ": `" + edge.name + "`, from: " + edge.from.name + ", to: NONE");
					}
					else
					{
						Console.WriteLine("Open edge " + edge.id.ToString() + ": `" + edge.name + "`");
					}
				}
				else
				{
					Console.WriteLine("edge " + edge.id.ToString() + ": `" + edge.name + "`, from: " + edge.from.name + ", to: " + edge.to.name);
				}
			}

			// STEP 3: Group cells

			// TBD.

			// STEP 4: Output

			GraphModel netlist = new GraphModel();

			// Prepare the final model: get node and edges of the final graph from Future collections.
			// Ignore open edges (without assigned start/end).

			float geom_scale_factor = 3.0f;

			foreach (FutureNode node in nodes)
			{
				GraphNode n = new GraphNode();
				n.id = "n" + node.id.ToString();

				// yEd specific

				YedData d6 = new YedData();

				d6.key = "d6";
				d6.node = new YedShapeNode();
				d6.node.label = new YedNodeLabel();
				d6.node.label.text = node.name;
				d6.node.geometry = new YedNodeGeometry();
				d6.node.geometry.x = node.baseVias.LambdaX * geom_scale_factor;
				d6.node.geometry.y = node.baseVias.LambdaY * geom_scale_factor;
				d6.node.geometry.width = ViasSize * 2;
				d6.node.geometry.height = ViasSize * 2;

				n.data.Add(d6);

				netlist.graph.nodes.Add(n);
			}

			foreach (FutureEdge edge in edges)
			{
				if ( edge.from == null || edge.to == null)
					continue;

				GraphEdge e = new GraphEdge();
				e.id = "e" + edge.id.ToString();
				e.source = "n" + edge.from.id.ToString();
				e.target = "n" + edge.to.id.ToString();

				// yEd specific

				YedData d10 = new YedData();
				d10.key = "d10";

				d10.edge = new YedPolyLineEdge();
				d10.edge.label = new YedEdgeLabel(edge.name);

				e.data.Add(d10);

				netlist.graph.edges.Add(e);
			}

			// Serialize list of graph nodes and edges in GraphML format

			XmlDocument doc = SerializeGraphModel(netlist);

			return doc.OuterXml;
		}

		private static bool WireExistsInFutureEdges(List<FutureEdge> edges, Entity wire)
		{
			foreach (FutureEdge edge in edges)
			{
				if (edge.segments.Contains(wire))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Perform the traverse starting with the specified wire. The traverse runs only through the wires.
		/// </summary>
		/// <param name="edgesAll">A collection of all future edges to check the presence of the wire as a segment in them.</param>
		/// <param name="nodesAll">A collection of all nodes that have been collected beforehand.</param>
		/// <param name="edge">The future edge of the graph, where all the segments (wires that have been traversed) are stored.</param>
		/// <param name="wire">Source wire</param>
		/// <param name="entities">Collection of source entities (all entities)</param>
		/// <param name="tier">Recursion depth</param>
		/// <param name="vias_intersections">Vias сrossing сounter</param>
		private static void TraverseWire (List<FutureEdge> edgesAll, List<FutureNode> nodesAll, FutureEdge edge, Entity wire, List<Entity> entities, int tier = 1, int vias_intersections = 0)
		{
			float maxDist = traverseLambdaDelta;
			float dist;

			if (edge.segments.Contains(wire))
				return;

			// Add the wire itself as segment.

			edge.name += wire.Label + " ";
			edge.segments.Add(wire);

			// Iterate all entities and perform a traverse.

			foreach (Entity entity in entities)
			{
				if (wire.TraverseBlackList != null && entity.TraverseBlackList != null)
				{
					if (wire.TraverseBlackList.Contains(entity.Type) || entity.TraverseBlackList.Contains(wire.Type))
						continue;
				}

				// If the number of intersections of segments with vias >= 2 - interrupt the process (terminate the segment).

				if (entity.IsVias() && IsViasInWire(entity, wire, maxDist))
				{
					vias_intersections++;
					if (vias_intersections >= 2)
					{
						break;
					}
				}

				// Continue the segment only if there is no vias(aka node) at the intersection

				else if (entity.IsWire() && !WireExistsInFutureEdges(edgesAll, entity))
				{
					PointF pointStart = new PointF(entity.LambdaX, entity.LambdaY);
					PointF pointEnd = new PointF(entity.LambdaEndX, entity.LambdaEndY);

					dist = (float)Math.Sqrt(Math.Pow(entity.LambdaX - wire.LambdaX, 2) +
											 Math.Pow(entity.LambdaY - wire.LambdaY, 2));

					if (dist < maxDist && !edge.segments.Contains(entity) && FindNode(nodesAll, pointStart) == null)
					{
						TraverseWire(edgesAll, nodesAll, edge, entity, entities, tier+1, vias_intersections);
						continue;
					}

					dist = (float)Math.Sqrt(Math.Pow(entity.LambdaX - wire.LambdaEndX, 2) +
											 Math.Pow(entity.LambdaY - wire.LambdaEndY, 2));

					if (dist < maxDist && !edge.segments.Contains(entity) && FindNode(nodesAll, pointStart) == null)
					{
						TraverseWire(edgesAll, nodesAll, edge, entity, entities, tier+1, vias_intersections);
						continue;
					}

					dist = (float)Math.Sqrt(Math.Pow(entity.LambdaEndX - wire.LambdaEndX, 2) +
											 Math.Pow(entity.LambdaEndY - wire.LambdaEndY, 2));

					if (dist < maxDist && !edge.segments.Contains(entity) && FindNode(nodesAll, pointEnd) == null)
					{
						TraverseWire(edgesAll, nodesAll, edge, entity, entities, tier+1, vias_intersections);
						continue;
					}

					dist = (float)Math.Sqrt(Math.Pow(entity.LambdaEndX - wire.LambdaX, 2) +
											 Math.Pow(entity.LambdaEndY - wire.LambdaY, 2));

					if (dist < maxDist && !edge.segments.Contains(entity) && FindNode(nodesAll, pointEnd) == null)
					{
						TraverseWire(edgesAll, nodesAll, edge, entity, entities, tier+1, vias_intersections);
						continue;
					}
				}

			}

		}

		private static bool IsViasInWire(Entity vias, Entity wire, float delta)
		{
			PointF start = new PointF(wire.LambdaX, wire.LambdaY);
			PointF end = new PointF(wire.LambdaEndX, wire.LambdaEndY);

			RectangleF rect = new RectangleF(
				vias.LambdaX - delta, vias.LambdaY - delta,
				2 * delta, 2 * delta);

			return LineIntersectsRect(start, end, rect);
		}

		private static bool LineIntersectsRect(PointF p1, PointF p2, RectangleF r)
		{
			return LineIntersectsLine(p1, p2, new PointF(r.X, r.Y), new PointF(r.X + r.Width, r.Y)) ||
				   LineIntersectsLine(p1, p2, new PointF(r.X + r.Width, r.Y), new PointF(r.X + r.Width, r.Y + r.Height)) ||
				   LineIntersectsLine(p1, p2, new PointF(r.X + r.Width, r.Y + r.Height), new PointF(r.X, r.Y + r.Height)) ||
				   LineIntersectsLine(p1, p2, new PointF(r.X, r.Y + r.Height), new PointF(r.X, r.Y)) ||
				   (r.Contains(p1) && r.Contains(p2));
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

		private static FutureNode FindNode (List<FutureNode> nodes, PointF point)
		{
			float maxDist = traverseLambdaDelta;
			float dist;

			foreach (FutureNode node in nodes)
			{
				dist = (float)Math.Sqrt(Math.Pow(node.baseVias.LambdaX - point.X, 2) +
										 Math.Pow(node.baseVias.LambdaY - point.Y, 2));

				if (dist < maxDist)
				{
					return node;
				}
			}

			return null;
		}

		private static XmlDocument SerializeGraphModel(GraphModel model)
		{
			XmlDocument doc = new XmlDocument();

			// Create an XML declaration
			XmlDeclaration xmldecl;
			xmldecl = doc.CreateXmlDeclaration("1.0", "UTF-8", "no");

			// Add the new node to the document
			XmlElement root = doc.DocumentElement;
			doc.InsertBefore(xmldecl, root);

			AddYedKeys(model);

			var nav = doc.CreateNavigator();
			var ns = new XmlSerializerNamespaces();
			ns.Add("java", "http://www.yworks.com/xml/yfiles-common/1.0/java");
			ns.Add("sys", "http://www.yworks.com/xml/yfiles-common/markup/primitives/2.0");
			ns.Add("x", "http://www.yworks.com/xml/yfiles-common/markup/2.0");
			ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
			ns.Add("y", "http://www.yworks.com/xml/graphml");
			ns.Add("yed", "http://www.yworks.com/xml/yed/3");

			using (var writer = nav.AppendChild())
			{
				var ser = new XmlSerializer(typeof(GraphModel));
				ser.Serialize(writer, model, ns);
			}

			return doc;
		}

		private static void AddYedKeys(GraphModel yed)
		{
			yed.keys.Add(new YedKey("d0", "Description", "string", "graph", null));
			yed.keys.Add(new YedKey("d1", null, null, "port", "portgraphics"));
			yed.keys.Add(new YedKey("d2", null, null, "port", "portgeometry"));
			yed.keys.Add(new YedKey("d3", null, null, "port", "portuserdata"));
			yed.keys.Add(new YedKey("d4", "url", "string", "node", null));
			yed.keys.Add(new YedKey("d5", "description", "string", "node", null));
			yed.keys.Add(new YedKey("d6", null, null, "node", "nodegraphics"));
			yed.keys.Add(new YedKey("d7", null, null, "graphml", "resources"));
			yed.keys.Add(new YedKey("d8", "url", "string", "edge", null));
			yed.keys.Add(new YedKey("d9", "description", "string", "edge", null));
			yed.keys.Add(new YedKey("d10", null, null, "edge", "edgegraphics"));
		}

	}

	#region "Minimal object model of GraphML format"

	[XmlRoot("graphml")]
	public class GraphModel
	{
		[XmlElement(ElementName = "key")]
		public List<YedKey> keys = new List<YedKey>();
		[XmlElement]
		public Graph graph = new Graph();
	}

	public class Graph
	{
		[XmlAttribute]
		public string edgedefault = "directed";
		[XmlAttribute]
		public string id = "G";
		[XmlElement(ElementName = "node")]
		public List<GraphNode> nodes = new List<GraphNode>();
		[XmlElement(ElementName = "edge")]
		public List<GraphEdge> edges = new List<GraphEdge>();
	}

	public class GraphNode
	{
		[XmlAttribute]
		public string id;
		[XmlElement(ElementName = "data")]
		public List<YedData> data = new List<YedData>();
	}

	public class GraphEdge
	{
		[XmlAttribute] public string id;
		[XmlAttribute] public string source;
		[XmlAttribute] public string target;
		[XmlElement(ElementName = "data")]
		public List<YedData> data = new List<YedData>();
	}

	#endregion "Minimal object model of GraphML format"


	#region "GraphML format extension to work with yWorks utilities"

	public class YedKey
	{
		[XmlAttribute]
		public string id;
		[XmlAttribute(AttributeName = "attr.name")]
		public string attrName;
		[XmlAttribute(AttributeName = "attr.type")]
		public string attrType;
		[XmlAttribute(AttributeName = "for")]
		public string For;
		[XmlAttribute(AttributeName = "yfiles.type")]
		public string yfilesType;

		public YedKey() { }

		public YedKey(string _id, string _attrName, string _attrType, string _for, string _yfilesType)
		{
			id = _id;
			attrName = _attrName;
			attrType = _attrType;
			For = _for;
			yfilesType = _yfilesType;
		}
	}

	public class YedData
	{
		[XmlAttribute]
		public string key;
		[XmlAttribute(AttributeName = "xml:space")]
		public string xmlSpace;
		[XmlElement(ElementName = "ShapeNode", Namespace = "http://www.yworks.com/xml/graphml")]
		public YedShapeNode node;
		[XmlElement(ElementName = "PolyLineEdge", Namespace = "http://www.yworks.com/xml/graphml")]
		public YedPolyLineEdge edge;
	}

	public class YedShapeNode
	{
		[XmlElement(ElementName = "NodeLabel", Namespace = "http://www.yworks.com/xml/graphml")]
		public YedNodeLabel label;
		[XmlElement(ElementName = "Geometry", Namespace = "http://www.yworks.com/xml/graphml")]
		public YedNodeGeometry geometry;
	}

	public class YedNodeLabel
	{
		[XmlAttribute] public string alignment = "center";
		[XmlAttribute] public int fontSize = 5;
		[XmlAttribute] public string fontStyle = "plain";
		[XmlAttribute] public string horizontalTextPosition = "center";
		[XmlAttribute] public string textColor = "#000000";
		[XmlAttribute] public string verticalTextPosition = "bottom";
		[XmlAttribute] public bool visible = true;
		[XmlAttribute] public float x = 3;
		[XmlAttribute] public float y = 0;
		[XmlAttribute] public float width = 13;
		[XmlAttribute] public float height = 18;
		[XmlText]
		public string text;
	}

	public class YedNodeGeometry
	{
		[XmlAttribute] public float x = 0;
		[XmlAttribute] public float y = 0;
		[XmlAttribute] public float width = 15.0f;
		[XmlAttribute] public float height = 15.0f;
	}

	public class YedPolyLineEdge
	{
		[XmlElement(ElementName = "EdgeLabel", Namespace = "http://www.yworks.com/xml/graphml")]
		public YedEdgeLabel label;
	}

	public class YedEdgeLabel
	{
		// Not sure wtf is this, just set to some nice values

		[XmlAttribute] public string alignment = "center";
		[XmlAttribute] public string configuration = "AutoFlippingLabel";
		[XmlAttribute] public float distance = 2;
		[XmlAttribute] public string fontFamily = "Dialog";
		[XmlAttribute] public int fontSize = 5;
		[XmlAttribute] public string fontStyle = "plain";
		[XmlAttribute] public bool hasBackgroundColor = false;
		[XmlAttribute] public bool hasLineColor = false;
		[XmlAttribute] public string horizontalTextPosition = "center";
		[XmlAttribute] public string modelName = "centered";
		[XmlAttribute] public string modelPosition = "center";
		[XmlAttribute] public string preferredPlacement = "anywhere";
		[XmlAttribute] public string textColor = "#000000";
		[XmlAttribute] public string verticalTextPosition = "bottom";
		[XmlAttribute] public bool visible = true;

		[XmlText]
		public string text;
		public YedEdgeLabel() { }

		public YedEdgeLabel(string _text)
		{
			text = _text;
		}
	}

	#endregion "GraphML format extension to work with yWorks utilities"

}
