// Sometimes double-clicking on TreeView checkboxes trigger additional events

using System;
using System.Windows.Forms;

namespace DerouteSharp
{
	class MyTreeView : TreeView
	{
		protected override void WndProc(ref Message m)
		{
			if (m.Msg == 0x0203)
			{
				m.Result = IntPtr.Zero;
			}
			else
			{
				base.WndProc(ref m);
			}
		}
	}
}
