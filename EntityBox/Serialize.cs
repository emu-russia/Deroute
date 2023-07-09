// Serialization

using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace System.Windows.Forms
{
	public partial class EntityBox : Control
	{

		public void Serialize(string FileName)
		{
			XmlSerializer ser = new XmlSerializer(typeof(List<Entity>));

			DeleteGarbage();

			using (FileStream fs = new FileStream(FileName, FileMode.Create))
			{
				ser.Serialize(fs, root.Children);
			}
		}

		public void Unserialize(string FileName, bool Append)
		{
			XmlSerializer ser = new XmlSerializer(typeof(List<Entity>));

			using (FileStream fs = new FileStream(FileName, FileMode.Open))
			{
				if (Append == false)
					insertionNode.Children.Clear();

				List<Entity> newList = (List<Entity>)ser.Deserialize(fs);
				insertionNode.Children.AddRange(newList);

				FixupParentLinksAndSelectionRecursive(insertionNode);

				DeleteGarbage();
				SortEntities();
				Invalidate();

				if (OnEntityCountChanged != null)
					OnEntityCountChanged(this, EventArgs.Empty);

				UnserializeLastStamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
			}
		}

		private void FixupParentLinksAndSelectionRecursive (Entity parent)
		{
			parent.SetParentControl(this);

			foreach (var entity in parent.Children)
			{
				entity.parent = parent;
				entity.Selected = SelectEntitiesAfterAdd;
				entity.SetParentControl(this);

				FixupParentLinksAndSelectionRecursive(entity);
			}
		}

	}
}
