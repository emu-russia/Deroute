// Auxiliary utilities for cell management

using DerouteSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

public class CellSupport
{

	public static void RotateCell (EntityBox box)
	{
		Console.WriteLine("rot");

		List<Entity> entities = box.GetEntities();

		foreach (var entity in entities)
		{
			if ((entity.IsCell() || entity.IsUnit()) && entity.Selected)
			{
				var ports = GetPorts(entity, entities);

				var temp = entity.LambdaWidth;
				entity.LambdaWidth = entity.LambdaHeight;
				entity.LambdaHeight = temp;

				switch (entity.LabelAlignment)
				{
					case TextAlignment.TopLeft:
					case TextAlignment.GlobalSettings:
						entity.LabelAlignment = TextAlignment.TopRight;
						break;

					case TextAlignment.TopRight:
						entity.LabelAlignment = TextAlignment.BottomRight;
						break;

					case TextAlignment.BottomRight:
						entity.LabelAlignment = TextAlignment.BottomLeft;
						break;

					case TextAlignment.BottomLeft:
						entity.LabelAlignment = TextAlignment.TopLeft;
						break;
				}

				foreach (var port in ports)
				{
					var ofs_x = port.LambdaX - entity.LambdaX;
					var ofs_y = port.LambdaY - entity.LambdaY;
					port.LambdaX = entity.LambdaX + (entity.LambdaWidth - ofs_y);
					port.LambdaY = entity.LambdaY + ofs_x;
				}
			}
		}

		box.Invalidate();
	}

	public static void FlipCell (EntityBox box)
	{
		Console.WriteLine("flip");
	}

	public static List<Cell> DeserializeFromFile (string FileName)
	{
		XmlSerializer ser = new XmlSerializer(typeof(List<Cell>));

		using (FileStream fs = new FileStream(FileName, FileMode.Open))
		{
			List<Cell> newList = (List<Cell>)ser.Deserialize(fs);
			return newList;
		}
	}

	public static void SerializeToFile(List<Cell> cells, string FileName)
	{
		XmlSerializer ser = new XmlSerializer(typeof(List<Cell>));

		//DeleteGarbage();

		using (FileStream fs = new FileStream(FileName, FileMode.Create))
		{
			ser.Serialize(fs, cells);
		}
	}


	public class Cell
	{
		/// <summary>
		/// Cell name
		/// </summary>
		[XmlElement("Name")]
		public string Name { get; set; } = "";

		/// <summary>
		/// For the simulator
		/// </summary>
		[XmlElement("ScriptSource")]
		public string ScriptSource { get; set; } = "";

		// https://stackoverflow.com/questions/1907077/serialize-a-bitmap-in-c-net-to-xml
		// `I would do something like:`

		[XmlIgnore]
		public Bitmap cell_image { get; set; } = null;

		/// <summary>
		/// Topology image (bitmap)
		/// </summary>
		[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
		[XmlElement("CellImage")]
		public byte[] CellImage
		{
			get
			{ // serialize
				if (cell_image == null) return null;
				using (MemoryStream ms = new MemoryStream())
				{
					cell_image.Save(ms, ImageFormat.Jpeg);
					return ms.ToArray();
				}
			}
			set
			{ // deserialize
				if (value == null)
				{
					cell_image = null;
				}
				else
				{
					using (MemoryStream ms = new MemoryStream(value))
					{
						cell_image = new Bitmap(ms);
					}
				}
			}
		}

		// me too :=P

		/// <summary>
		/// Entities from which the cell is made (CellXxx, Vias, etc.)
		/// </summary>
		public List<Entity> Entities { get; set; } = new List<Entity>();
	}


	/// <summary>
	/// All input/output/input vias within a cell/block become ports
	/// </summary>
	static List<Entity> GetPorts(Entity cell, List<Entity> ents)
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
}
