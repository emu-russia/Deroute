using Glfw;
using System.Runtime.InteropServices;
using Vulkan;
using static Vulkan.Vk;

// https://github.com/jpbruyere/vke.net/blob/master/vke/src/VkWindow.cs

namespace VulkanDemo
{
	public partial class Form1 : Form
	{
		protected Instance instance;

		public Form1()
		{
			InitializeComponent();
			VulkanInit();
		}

		private void VulkanInit()
		{
			List<string> instExts = new List<string>(Glfw3.GetRequiredInstanceExtensions());
			if (EnabledInstanceExtensions != null)
				instExts.AddRange(EnabledInstanceExtensions);

		}

		public static string[] SupportedExtensions(IntPtr layer)
		{
			CheckResult(vkEnumerateInstanceExtensionProperties(layer, out uint count, IntPtr.Zero));

			int sizeStruct = Marshal.SizeOf<VkExtensionProperties>();
			IntPtr ptrSupExts = Marshal.AllocHGlobal(sizeStruct * (int)count);
			CheckResult(vkEnumerateInstanceExtensionProperties(layer, out count, ptrSupExts));

			string[] result = new string[count];
			IntPtr tmp = ptrSupExts;
			for (int i = 0; i < count; i++)
			{
				result[i] = Marshal.PtrToStringAnsi(tmp);
				tmp += sizeStruct;
			}

			Marshal.FreeHGlobal(ptrSupExts);
			return result;
		}

		private void exitToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			Close();
		}
	}
}
