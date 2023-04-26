// Auxiliary utilities for cell management

using DerouteSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
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

		List<Entity> entities = box.GetEntities();

		foreach (var entity in entities)
		{
			if ((entity.IsCell() || entity.IsUnit()) && entity.Selected)
			{
				var ports = GetPorts(entity, entities);

				switch (entity.LabelAlignment)
				{
					case TextAlignment.TopLeft:
					case TextAlignment.GlobalSettings:
						entity.LabelAlignment = TextAlignment.TopRight;
						break;

					case TextAlignment.TopRight:
						entity.LabelAlignment = TextAlignment.TopLeft;
						break;

					case TextAlignment.BottomRight:
						entity.LabelAlignment = TextAlignment.BottomLeft;
						break;

					case TextAlignment.BottomLeft:
						entity.LabelAlignment = TextAlignment.BottomRight;
						break;
				}

				foreach (var port in ports)
				{
					var midp = entity.LambdaX + entity.LambdaWidth / 2;
					var ofs_x = midp - port.LambdaX;
					port.LambdaX = midp + ofs_x;
				}
			}
		}

		box.Invalidate();
	}

	private static string GetTemporaryDirectory()
	{
		string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		Directory.CreateDirectory(tempDirectory);
		return tempDirectory;
	}

	public static List<Cell> DeserializeFromFile (string FileName)
	{
		XmlSerializer ser = new XmlSerializer(typeof(List<Cell>));
		List<Cell> newList = new List<Cell>();

		if (Path.GetExtension(FileName).ToLower() == ".xmlz")
		{
			string temp_xml_dir = GetTemporaryDirectory();
			ZipFile.ExtractToDirectory(FileName, temp_xml_dir);
			DirectoryInfo di = new DirectoryInfo(temp_xml_dir);
			bool first = true;
			foreach (FileInfo file in di.GetFiles())
			{
				if (first)
				{
					using (FileStream fs = new FileStream(file.FullName, FileMode.Open))
					{
						newList = (List<Cell>)ser.Deserialize(fs);
					}
					first = false;
				}
				file.Delete();
			}
			Directory.Delete(temp_xml_dir);
		}
		else
		{
			using (FileStream fs = new FileStream(FileName, FileMode.Open))
			{
				newList = (List<Cell>)ser.Deserialize(fs);
			}
		}

		return newList;
	}

	public static void SerializeToFile(List<Cell> cells, string FileName)
	{
		XmlSerializer ser = new XmlSerializer(typeof(List<Cell>));

		if (Path.GetExtension(FileName).ToLower() == ".xmlz")
		{
			string temp_xml_dir = GetTemporaryDirectory();
			string temp_xml_filename = temp_xml_dir + "/" + Path.GetFileNameWithoutExtension(FileName) + ".xml";
			using (FileStream fs = new FileStream(temp_xml_filename, FileMode.Create))
			{
				ser.Serialize(fs, cells);
			}
			if (File.Exists(FileName))
			{
				File.Delete(FileName);
			}
			ZipFile.CreateFromDirectory(temp_xml_dir, FileName);
			File.Delete(temp_xml_filename);
			Directory.Delete(temp_xml_dir);
		}
		else
		{
			using (FileStream fs = new FileStream(FileName, FileMode.Create))
			{
				ser.Serialize(fs, cells);
			}
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
						cell_image = new Bitmap(Image.FromStream(ms));
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
