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

}
