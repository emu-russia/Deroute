using System.Drawing;
using System.ComponentModel;

public static class FontXmlConverter
{
	public static string ConvertToString(Font font)
	{
		try
		{
			if (font != null)
			{
				TypeConverter converter = TypeDescriptor.GetConverter(typeof(Font));
				return converter.ConvertToString(font);
			}
			else 
				return null;
		}
		catch { System.Diagnostics.Debug.WriteLine("Unable to convert"); }
		return null;
	}
	public static Font ConvertToFont(string fontString)
	{
		try
		{
			TypeConverter converter = TypeDescriptor.GetConverter(typeof(Font));
			return (Font)converter.ConvertFromString(fontString);
		}
		catch { System.Diagnostics.Debug.WriteLine("Unable to convert"); }
		return null;
	}
}
