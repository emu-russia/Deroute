using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Drawing.Design;
using System.Globalization;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Forms.Design;

public class EntityTypeListEditor : UITypeEditor
{
	public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
	{
		return UITypeEditorEditStyle.Modal;
	}

	public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
	{
		if (provider == null || context == null) return value;

		IWindowsFormsEditorService editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
		if (editorService == null) return value;

		List<EntityType> currentList = value as List<EntityType>;
		if (currentList == null) currentList = new List<EntityType>();

		var form = new Form
		{
			Text = "Traverse Black List",
			StartPosition = FormStartPosition.CenterParent,
			MinimizeBox = false,
			MaximizeBox = false,
			FormBorderStyle = FormBorderStyle.FixedDialog,
			ShowInTaskbar = false,
			Width = 360,
			Height = 480,
		};

		var listView = new ListView
		{
			Dock = DockStyle.Fill,
			View = View.List,
			FullRowSelect = true,
			CheckBoxes = false,
		};

		foreach (EntityType et in currentList)
		{
			listView.Items.Add(et.ToString());
		}

		var panel = new Panel
		{
			Dock = DockStyle.Fill,
		};
		panel.Controls.Add(listView);

		var buttonPanel = new FlowLayoutPanel
		{
			Dock = DockStyle.Bottom,
			Height = 45,
			FlowDirection = FlowDirection.RightToLeft,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			Padding = new Padding(10, 5, 10, 10),
			BackColor = System.Drawing.Color.White,
		};

		var btnAdd = new Button { Text = "Add", Width = 60, Height = 25, DialogResult = DialogResult.None };
		var btnRemove = new Button { Text = "Remove", Width = 60, Height = 25, DialogResult = DialogResult.None };
		var btnOk = new Button { Text = "OK", Width = 60, Height = 25 };
		var btnCancel = new Button { Text = "Cancel", Width = 60, Height = 25 };

		buttonPanel.Controls.Add(btnRemove);
		buttonPanel.Controls.Add(btnAdd);
		buttonPanel.Controls.Add(btnCancel);
		buttonPanel.Controls.Add(btnOk);

		form.Controls.Add(panel);
		form.Controls.Add(buttonPanel);
		form.AcceptButton = btnOk;
		form.CancelButton = btnCancel;

		btnAdd.Click += (s, e) =>
		{
			var dlg = new Form
			{
				Text = "Select Entity Type",
				StartPosition = FormStartPosition.CenterParent,
				MinimizeBox = false,
				MaximizeBox = false,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				ShowInTaskbar = false,
				Width = 250,
				Height = 370,
			};

			var listPanel = new Panel
			{
				Dock = DockStyle.Fill,
				BorderStyle = BorderStyle.FixedSingle,
			};

			var lb = new ListBox
			{
				Dock = DockStyle.Fill,
				BorderStyle = BorderStyle.None,
			};

			foreach (EntityType et in Enum.GetValues(typeof(EntityType)))
			{
				if (et == EntityType.Root) continue;
				lb.Items.Add(et);
			}

			listPanel.Controls.Add(lb);

			var okBtn = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom, Height = 30, Width = 75, Margin = new Padding(10, 5, 10, 10) };
			dlg.Controls.Add(listPanel);
			dlg.Controls.Add(okBtn);

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				if (lb.SelectedItem != null)
				{
					listView.Items.Add(lb.SelectedItem.ToString());
				}
			}
			dlg.Dispose();
		};

		btnRemove.Click += (s, e) =>
		{
			if (listView.SelectedIndices.Count > 0)
			{
				listView.SelectedItems[0].Remove();
			}
		};

		btnOk.Click += (s, e) => { form.DialogResult = DialogResult.OK; form.Close(); };
		btnCancel.Click += (s, e) => { form.DialogResult = DialogResult.Cancel; form.Close(); };

		if (editorService.ShowDialog(form) == DialogResult.OK)
		{
			var result = new List<EntityType>();
			foreach (ListViewItem item in listView.Items)
			{
				if (Enum.TryParse(item.Text, out EntityType et))
				{
					result.Add(et);
				}
			}
			return result;
		}

		return value;
	}
}

public class EntityTypeListConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
	{
		return (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);
	}

	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
	{
		return (destinationType == typeof(InstanceDescriptor)) || base.CanConvertTo(context, destinationType);
	}

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		string str = value as string;
		if (value == null) return base.ConvertFrom(context, culture, value);
		str = str.Trim();
		if (str.Length == 0) return null;
		if (culture == null) culture = CultureInfo.CurrentCulture;
		char ch = culture.TextInfo.ListSeparator[0];
		string[] strArray = str.Split(new char[] { ch });
		var result = new List<EntityType>();
		TypeConverter converter = TypeDescriptor.GetConverter(typeof(EntityType));
		foreach (string item in strArray)
		{
			try
			{
				result.Add((EntityType)converter.ConvertFromString(context, culture, item.Trim()));
			}
			catch
			{
			}
		}
		return result;
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if (destinationType == null) throw new ArgumentNullException("destinationType");

		if (value is List<EntityType>)
		{
			if (destinationType == typeof(string))
			{
				var list = (List<EntityType>)value;
				if (culture == null) culture = CultureInfo.CurrentCulture;
				string separator = culture.TextInfo.ListSeparator + " ";
				TypeConverter converter = TypeDescriptor.GetConverter(typeof(EntityType));
				string[] strArray = new string[list.Count];
				for (int i = 0; i < list.Count; i++)
				{
					strArray[i] = converter.ConvertToString(context, culture, list[i]);
				}
				return string.Join(separator, strArray);
			}
			if (destinationType == typeof(InstanceDescriptor))
			{
				var list = (List<EntityType>)value;
				ConstructorInfo constructor = typeof(List<EntityType>).GetConstructor(new Type[] { typeof(IEnumerable) });
				if (constructor != null) return new InstanceDescriptor(constructor, new object[] { list });
			}
		}

		return base.ConvertTo(context, culture, value, destinationType);
	}

	public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
	{
		if (propertyValues == null) throw new ArgumentNullException("propertyValues");
		return new List<EntityType>();
	}

	public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
	{
		return true;
	}

	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
	{
		return TypeDescriptor.GetProperties(typeof(List<EntityType>), attributes).Sort(new string[] { "Count", "Item" });
	}

	public override bool GetPropertiesSupported(ITypeDescriptorContext context)
	{
		return true;
	}
}
