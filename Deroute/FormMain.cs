using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Runtime.InteropServices;
using System.Threading;
using NeuralNetwork;
using System.IO;
using System.IO.Compression;
using System.Xml.Serialization;

//
// Nothing to comment here. Everything is self-explanatory (GUI stubs)
//

namespace DerouteSharp
{
	public partial class FormMain : Form
	{
#if !__MonoCS__
		[DllImport("kernel32")]
		static extern bool AllocConsole();
#endif

		private string savedText;
		private TimeSpentStats timeStats = new TimeSpentStats();
		private Random rnd = new Random(DateTime.Now.Millisecond);
		private FormCells cells_editor = null;
		private List<CellSupport.Cell> cells_db = new List<CellSupport.Cell>();
		private DerouteSim sim;
		private FormWaves waves = null;

		public FormMain()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			entityBox1.AssociateSelectionPropertyGrid(propertyGrid2);

			entityBox1.Mode = EntityMode.Selection;

			SelectionButtonHighlight();

			entityBox1.OnScrollChanged += ScrollChanged;
			entityBox1.OnZoomChanged += ZoomChanged;
			entityBox1.OnEntityCountChanged += EntityCountChanged;
			entityBox1.OnEntityLabelEdit += EntityLabelChanged;
			entityBox1.OnFrameDone += entityBox1_OnFrameDone;
			entityBox1.OnDestinationNodeChanged += EntityBox1_OnDestinationNodeChanged;
			entityBox1.OnModuleChanged += EntityBox1_OnModuleChanged;
			entityBox1.OnSelectionBox += EntityBox1_OnSelectionBox;

			entityBox1.BeaconImage = Properties.Resources.beacon_entity;

			timeStats.normalFont = Font;
			timeStats.penaltyFont = new Font(Font, FontStyle.Bold);

			backgroundWorkerTimeSpent.RunWorkerAsync();

			savedText = Text;

			FormSettings.LoadSettings(entityBox1);

			PopulateTree();

#if DEBUG && (!__MonoCS__)
			AllocConsole ();
#endif

			entityBox1.Focus();
			
			sim = new DerouteSim(entityBox1);
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FormAbout aboutDialog = new FormAbout();
			aboutDialog.ShowDialog();
		}


		#region "Event Handlers"

		private void EntityBox1_OnDestinationNodeChanged(object sender, Entity entity, EventArgs e)
		{
			toolStripStatusLabel11.Text = entity.Type.ToString() + " " + entity.Label;
		}

		private void entityBox1_OnFrameDone(object sender, long ms_time, EventArgs e)
		{
			toolStripStatusLabel14.Text = ms_time.ToString() + " ms";
		}

		private void ScrollChanged(object sender, EventArgs e)
		{
			EntityBox entityBox = (EntityBox)sender;

			toolStripStatusLabel2.Text = entityBox.ScrollX.ToString() + "; " +
										 entityBox.ScrollY.ToString();
		}

		private void ZoomChanged(object sender, EventArgs e)
		{
			EntityBox entityBox = (EntityBox)sender;

			toolStripStatusLabel4.Text = entityBox.Zoom.ToString() + "%";
		}

		private void EntityCountChanged(object sender, EventArgs e)
		{
			EntityBox entityBox = (EntityBox)sender;

			toolStripStatusLabel6.Text = entityBox.GetViasCount().ToString();
			toolStripStatusLabel8.Text = entityBox.GetWireCount().ToString();
			toolStripStatusLabel10.Text = entityBox.GetCellCount().ToString();

			// Update beacon list

			if ( listView1.Items.Count != entityBox1.GetBeaconCount() )
			{
				RebuildBeaconList();
			}

			// Update module list

			RepopulateModulesList();

			// Update tree

			PopulateTree();
		}

		private void EntityLabelChanged(object sender, Entity entity, EventArgs e)
		{
			if (entity.Type == EntityType.Beacon)
			{
				RebuildBeaconList();
			}

			TreeNode node;

			if (SearchTreeNodeByEntity(entity, out node))
			{
				node.Text = entity.Type.ToString() + " " + entity.Label;
			}
		}

		private void EntityBox1_OnSelectionBox(object sender, PointF orig, PointF sizef, EventArgs e)
		{
			if (addCellFromSelectionToolStripMenuItem.Checked)
			{
				addCellFromSelectionToolStripMenuItem.Checked = false;

				if (entityBox1.Image == null)
				{
					MessageBox.Show("You must first load the original image (File -> Load Image)", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}

				if (cells_editor != null)
				{
					cells_editor.Focus();
				}
				else
				{
					cells_editor = new FormCells(entityBox1, cells_db);
					cells_editor.FormClosed += Cells_editor_FormClosed;
					cells_editor.Show();
				}

				Point point = entityBox1.LambdaToImage(orig.X, orig.Y);
				Point size_p = entityBox1.LambdaToImage(sizef.X, sizef.Y);
				Size size = new Size(size_p.X, size_p.Y);

				Bitmap sourceBitmap = new Bitmap(entityBox1.Image);
				Rectangle cloneRect = new Rectangle(point.X, point.Y, size.Width, size.Height);
				Bitmap cloneBitmap = sourceBitmap.Clone(cloneRect, sourceBitmap.PixelFormat);

				cells_editor.CreateCell(cloneBitmap, point, size);
			}
		}

		private void Cells_editor_FormClosed(object sender, FormClosedEventArgs e)
		{
			FormCells form = (FormCells)sender;
			if (form.Modifed)
			{
				cells_db = form.GetCollection();
			}
			cells_editor = null;
		}

		#endregion "Event Handlers"


		#region "Load / Save"

		private void loadImageToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DialogResult result = openFileDialog1.ShowDialog();

			if ( result == DialogResult.OK )
			{
				System.Drawing.Image image = System.Drawing.Image.FromFile(openFileDialog1.FileName);
				entityBox1.LoadImage(image);
			}
		}

		private void saveSceneAsImageToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DialogResult result = saveFileDialog1.ShowDialog();

			if (result == DialogResult.OK)
			{
				Cursor = Cursors.WaitCursor;

				entityBox1.SaveSceneAsImage(saveFileDialog1.FileName);

				Cursor = Cursors.Default;
			}
		}

		private void LoadEntitiesXml()
		{
			DialogResult result = openFileDialog2.ShowDialog();

			if (result == DialogResult.OK)
			{
				var filename = openFileDialog2.FileName;
				Text = savedText + " - " + filename;

				if (Path.GetExtension(filename).ToLower() == ".xmlz")
				{
					string temp_xml_dir = GetTemporaryDirectory();
					ZipFile.ExtractToDirectory(filename, temp_xml_dir);
					DirectoryInfo di = new DirectoryInfo(temp_xml_dir);
					bool first = true;
					foreach (FileInfo file in di.GetFiles())
					{
						if (first)
						{
							entityBox1.Unserialize(file.FullName, true);
							first = false;
						}
						file.Delete();
					}
					Directory.Delete(temp_xml_dir);
				}
				else
				{
					entityBox1.Unserialize(filename, true);
				}
			}
		}

		private void loadEntitiesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadEntitiesXml();
		}

		public string GetTemporaryDirectory()
		{
			string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempDirectory);
			return tempDirectory;
		}

		private void SaveEntitiesXml()
		{
			DialogResult result = saveFileDialog2.ShowDialog();

			if (result == DialogResult.OK)
			{
				var filename = saveFileDialog2.FileName;
				Text = savedText + " - " + filename;

				if (Path.GetExtension(filename).ToLower() == ".xmlz")
				{
					string temp_xml_dir = GetTemporaryDirectory();
					string temp_xml_filename = temp_xml_dir + "/" + Path.GetFileNameWithoutExtension(filename) + ".xml";
					entityBox1.Serialize(temp_xml_filename);
					if (File.Exists(filename))
					{
						File.Delete(filename);
					}
					ZipFile.CreateFromDirectory(temp_xml_dir, filename);
					File.Delete(temp_xml_filename);
					Directory.Delete(temp_xml_dir);
				}
				else
				{
					entityBox1.Serialize(filename);
				}
			}
		}

		private void saveEntitiesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveEntitiesXml();
		}

		private void saveSceneAsNetlistToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DialogResult result = saveFileDialog3.ShowDialog();

			if (result == DialogResult.OK)
			{
				string verilog_name = saveFileDialog3.FileName;
				string text = GetVerilog.EntitiesToVerilogSource(entityBox1, Path.GetFileNameWithoutExtension(verilog_name));
				File.WriteAllText(verilog_name, text, Encoding.ASCII);
				MessageBox.Show("Verilog successfully exported to file: " + verilog_name, "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		#endregion



		#region "Mode Selection"

		private void SelectionButtonHighlight()
		{
			toolStripDropDownButton1.BackColor = SystemColors.Control;
			toolStripDropDownButton2.BackColor = SystemColors.Control;
			toolStripDropDownButton3.BackColor = SystemColors.Control;
			toolStripButton5.BackColor = SystemColors.Control;
		}

		private void ViasButtonHighlight()
		{
			toolStripDropDownButton1.BackColor = SystemColors.ActiveCaption;
			toolStripDropDownButton2.BackColor = SystemColors.Control;
			toolStripDropDownButton3.BackColor = SystemColors.Control;
			toolStripButton5.BackColor = SystemColors.Control;
		}

		private void WiresButtonHighlight()
		{
			toolStripDropDownButton1.BackColor = SystemColors.Control;
			toolStripDropDownButton2.BackColor = SystemColors.ActiveCaption;
			toolStripDropDownButton3.BackColor = SystemColors.Control;
			toolStripButton5.BackColor = SystemColors.Control;
		}

		private void CellsButtonHighlight()
		{
			toolStripDropDownButton1.BackColor = SystemColors.Control;
			toolStripDropDownButton2.BackColor = SystemColors.Control;
			toolStripDropDownButton3.BackColor = SystemColors.ActiveCaption;
			toolStripButton5.BackColor = SystemColors.Control;
		}

		private void BeaconButtonHighlight()
		{
			toolStripDropDownButton1.BackColor = SystemColors.Control;
			toolStripDropDownButton2.BackColor = SystemColors.Control;
			toolStripDropDownButton3.BackColor = SystemColors.Control;
			toolStripButton5.BackColor = SystemColors.ActiveCaption;
		}

		private void wireInterconnectToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.WireInterconnect;
			WiresButtonHighlight();
		}

		private void wirePowerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.WirePower;
			WiresButtonHighlight();
		}

		private void wireGroundToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.WireGround;
			WiresButtonHighlight();
		}

		private void toolStripMenuItem1_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.ViasConnect;
			ViasButtonHighlight();
		}

		private void viasPowerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.ViasPower;
			ViasButtonHighlight();
		}

		private void viasGroundToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.ViasGround;
			ViasButtonHighlight();
		}

		private void viasInputToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.ViasInput;
			ViasButtonHighlight();
		}

		private void viasOutputToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.ViasOutput;
			ViasButtonHighlight();
		}

		private void viasInoutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.ViasInout;
			ViasButtonHighlight();
		}

		private void viasFloatingToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.ViasFloating;
			ViasButtonHighlight();
		}

		private void cellNotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellNot;
			CellsButtonHighlight();
		}

		private void cellBufferToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellBuffer;
			CellsButtonHighlight();
		}

		private void cellMuxToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellMux;
			CellsButtonHighlight();
		}

		private void cellLogicToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellLogic;
			CellsButtonHighlight();
		}

		private void cellAdderToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellAdder;
			CellsButtonHighlight();
		}

		private void cellBusSupportToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellBusSupp;
			CellsButtonHighlight();
		}

		private void cellFlipFlopToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellFlipFlop;
			CellsButtonHighlight();
		}

		private void cellLatchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellLatch;
			CellsButtonHighlight();
		}

		private void cellOtherToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.CellOther;
			CellsButtonHighlight();
		}

		private void unitRegisterFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.UnitRegfile;
			CellsButtonHighlight();
		}

		private void unitMemoryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.UnitMemory;
			CellsButtonHighlight();
		}

		private void unitCustomToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.UnitCustom;
			CellsButtonHighlight();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.ViasConnect;
			ViasButtonHighlight();
		}

		private void button3_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.WireInterconnect;
			WiresButtonHighlight();
		}

		private void toolStripButton5_Click(object sender, EventArgs e)
		{
			entityBox1.Mode = EntityMode.Beacon;
			BeaconButtonHighlight();
		}

		#endregion



		private void Form1_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F1 || e.KeyCode == Keys.Escape)
			{
				entityBox1.Mode = EntityMode.Selection;
				SelectionButtonHighlight();
			}
			else if (e.KeyCode == Keys.F2 )
			{
				entityBox1.Mode = EntityMode.ViasConnect;
				ViasButtonHighlight();
			}
			else if (e.KeyCode == Keys.F3)
			{
				entityBox1.Mode = EntityMode.WireInterconnect;
				WiresButtonHighlight();
			}
			else if (e.KeyCode == Keys.F10)
			{
				entityBox1.TraversalSelection(1);
			}
			else if (e.KeyCode == Keys.F11)
			{
				entityBox1.TraversalSelection(2);
			}
			else if (e.KeyCode == Keys.F12)
			{
				entityBox1.TraversalSelection(3);
			}
			else if (e.KeyCode == Keys.T && e.Control)
			{
				LambdaScale form = new LambdaScale();
				form.FormClosing += form_FormClosing;
				form.ShowDialog();
			}
			else if (e.KeyCode == Keys.A && e.Control)
			{
				entityBox1.SelectAll();
			}
			else if (e.KeyCode == Keys.S && e.Control)
			{
				SaveEntitiesXml();
			}
			else if (e.KeyCode == Keys.R && e.Control)
			{
				CellSupport.RotateCell(entityBox1);
			}
			else if (e.KeyCode == Keys.F && e.Control)
			{
				CellSupport.FlipCell(entityBox1);
			}
			else if (e.KeyCode == Keys.F5)
			{
				SimRunStop();
			}
			else if (e.KeyCode == Keys.F7)
			{
				SimStep();
			}
			else if (e.KeyCode == Keys.W && e.Control)
			{
				SimOpenWaves();
			}
		}

		private void toolStripButton4_Click(object sender, EventArgs e)
		{
			entityBox1.DrawWireBetweenSelectedViases();
		}

		private void listView1_DoubleClick(object sender, EventArgs e)
		{
			ListView listView = (ListView)sender;

			if (listView.SelectedItems.Count > 0)
			{
				ListViewItem selected = listView.SelectedItems[0];
				Entity beacon = (Entity)selected.Tag;
				entityBox1.ScrollToBeacon(beacon);

				//
				// Switch to selection mode
				//

				entityBox1.Mode = EntityMode.Selection;
				SelectionButtonHighlight();
			}
		}

		private void RebuildBeaconList ()
		{
			listView1.Items.Clear();
			List<Entity> beacons = entityBox1.GetBeacons();

			int id = 0;

			foreach (Entity beacon in beacons)
			{
				ListViewItem item = new ListViewItem(id.ToString());
				item.Tag = beacon;
				item.SubItems.Add(beacon.Label);
				listView1.Items.Add(item);

				id++;
			}
		}

		private void traverseTIER1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.TraversalSelection(1);
		}

		private void traverseTIER2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.TraversalSelection(2);
		}

		private void traverseTIER3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.TraversalSelection(3);
		}

		private void traverseTIER5ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.TraversalSelection(5);
		}

		private void keyBindingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			KeyBind keyBindDialog = new KeyBind();
			keyBindDialog.Show();
		}

		private void toolStripButton9_Click(object sender, EventArgs e)
		{
			entityBox1.WireExtendHead();
		}

		private void toolStripButton10_Click(object sender, EventArgs e)
		{
			entityBox1.WireExtendTail();
		}

		private void lambdaTransformToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LambdaScale form = new LambdaScale();
			form.FormClosing += form_FormClosing;
			form.ShowDialog();
		}

		private void form_FormClosing(object sender, FormClosingEventArgs e)
		{
			LambdaScale form = (LambdaScale)sender;
			if (form.ScaleValue != float.NaN)
				entityBox1.LambdaScale(form.ScaleValue);
		}

		private void toolStripButton12_Click(object sender, EventArgs e)
		{
			entityBox1.WireShortenHead();
		}

		private void toolStripButton11_Click(object sender, EventArgs e)
		{
			entityBox1.WireShortenTail();
		}

		private void selectAllViasesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.SelectAll(EntitySelection.Vias);
		}

		private void selectAllWiresToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.SelectAll(EntitySelection.Wire);
		}

		private void selectAllCellsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.SelectAll(EntitySelection.Cell);
		}

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.SelectAll();
		}

		private void toolStripButton13_Click(object sender, EventArgs e)
		{
			entityBox1.DrawRegionBetweenSelectedViases();
		}

		private void unloadImageToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.UnloadImage();
		}

		private void copyCtrlCToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Copy();
		}

		private void pasteCtrlVToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.Paste();
		}

		/// <summary>
		/// Worker wich updates time spent info
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void backgroundWorkerTimeSpent_DoWork(object sender, DoWorkEventArgs e)
		{
			while (true)
			{
				Thread.Sleep(1000);

				var secondsFromLastActivity = (DateTime.Now - timeStats.lastActivityTime).TotalSeconds;

				if (secondsFromLastActivity < timeStats.activityPenalty)
				{
					timeStats.seconds++;

					TimeSpan span = TimeSpan.FromSeconds(timeStats.seconds);
					string timeSpentStr = string.Format("{0:D2}:{1:D2}:{2:D2}",
						span.Hours,
						span.Minutes,
						span.Seconds);

					toolStripStatusLabelTimeSpent.Font = timeStats.normalFont;
					toolStripStatusLabelTimeSpent.Text = timeSpentStr;
				}
				else
				{
					toolStripStatusLabelTimeSpent.Font = timeStats.penaltyFont;
					toolStripStatusLabelTimeSpent.Text = timeStats.penaltyText;
				}
			}
		}

		/// <summary>
		/// Update last activity time
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void entityBox1_MouseClick(object sender, MouseEventArgs e)
		{
			timeStats.lastActivityTime = DateTime.Now;
		}

		private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FormSettings settings = new FormSettings(entityBox1);

			settings.FormClosed += Settings_FormClosed;
			settings.ShowDialog();
		}

		private void Settings_FormClosed(object sender, FormClosedEventArgs e)
		{
			FormSettings settings = (FormSettings)sender;

			if (settings.DialogResult == DialogResult.OK)
			{
				entityBox1.Invalidate();
			}
		}


		#region "Hierarchy"

		TreeNode prevNode = null;

		private void PopulateTree()
		{
			// During initial population all the nodes are collapsed.
			// The strategy for repopulating the tree is as follows: if there are entities in the tree, then save all the nodes in the Pair<Entity,bool Expanded> list.
			// When populating, check Tag, which stores reference to Entity and old Expanded value: if it was true - make node expanded.
			// This will allow to leave hierarchy without convulsions during tree repopulation.

			bool firstTime = myTreeView1.Nodes.Count == 0;
			List<Tuple<Entity, bool>> expandedList = firstTime ? new List<Tuple<Entity, bool>>() : GetTreeAsList(true);

			myTreeView1.Nodes.Clear();

			myTreeView1.BeginUpdate();

			TreeNode rootNode = new TreeNode("Root");

			rootNode.Tag = entityBox1.root;
			rootNode.Checked = true;

			myTreeView1.Nodes.Add(rootNode);

			foreach(var entity in entityBox1.root.Children)
			{
				PopulateTreeRecursive(entity, rootNode, expandedList);
			}

			myTreeView1.EndUpdate();
			rootNode.Expand();
		}

		private void PopulateTreeRecursive (Entity parent, TreeNode nodeParent, List<Tuple<Entity, bool>> expandedList)
		{
			TreeNode node = new TreeNode(parent.Type.ToString() + " " + parent.Label);

			node.Tag = parent;
			node.Checked = parent.Visible;

			nodeParent.Nodes.Add(node);

			foreach (var entity in parent.Children)
			{
				PopulateTreeRecursive(entity, node, expandedList);
			}

			// The tuple list contains Entity, which was previously associated with a TreeNode, and the TreeNode itself has been expanded.

			bool expanded = expandedList.Any(e => e.Item1 == parent && e.Item2);

			if (expanded)
			{
				node.Expand();
			}
		}

		private List<Tuple<Entity, bool>> GetTreeAsList(bool onlyExpanded)
		{
			List<Tuple<Entity, bool>> list = new List<Tuple<Entity, bool>>();

			foreach (TreeNode item in myTreeView1.Nodes)
			{
				if (item.Tag is Entity)
				{
					if (onlyExpanded)
					{
						bool expanded = item.IsExpanded;
						if (item.Nodes.Count == 0)
							expanded = false;
						if (expanded)
							list.Add(new Tuple<Entity, bool>(item.Tag as Entity, item.IsExpanded));
					}
					else
						list.Add(new Tuple<Entity, bool>(item.Tag as Entity, item.IsExpanded));
				}
				WalkTreeRecursive(list, item, onlyExpanded);
			}

			return list;
		}

		private void WalkTreeRecursive(List<Tuple<Entity, bool>> list, TreeNode parent, bool onlyExpanded)
		{
			foreach (TreeNode item in parent.Nodes)
			{
				if (item.Tag is Entity)
				{
					if (onlyExpanded)
					{
						bool expanded = item.IsExpanded;
						if (item.Nodes.Count == 0)
							expanded = false;
						if (expanded)
							list.Add(new Tuple<Entity, bool>(item.Tag as Entity, item.IsExpanded));
					}
					else
						list.Add(new Tuple<Entity, bool>(item.Tag as Entity, item.IsExpanded));
				}
				WalkTreeRecursive(list, item, onlyExpanded);
			}
		}

		private void myTreeView1_AfterSelect(object sender, TreeViewEventArgs e)
		{
			TreeView tree = (TreeView)sender;

			TreeNode node = tree.SelectedNode;

			if (prevNode != null)
			{
				prevNode.BackColor = tree.BackColor;
			}

			node.BackColor = Color.Gold;
			prevNode = node;

			if (node.Tag is Entity)
			{
				Entity entity = node.Tag as Entity;

				entityBox1.RemoveSelection();

				// Change the focus only if the type is not Layer

				if (entity != entityBox1.root && entity.Type != EntityType.Layer)
				{
					entityBox1.SelectEntity(entity);
					entityBox1.EnsureVisible(entity);
				}

				entityBox1.SetDestinationNode(entity);
				entityBox1.Invalidate();
				propertyGrid2.SelectedObject = entity;
			}

			// Dont loose focus after browing by keyboard arrows

			tree.Focus();
		}

		private void myTreeView1_AfterCheck(object sender, TreeViewEventArgs e)
		{
			TreeView tree = (TreeView)sender;

			foreach (TreeNode node in tree.Nodes)
			{
				SetNodeVisibilityRecursive(node);
			}

			entityBox1.Invalidate();
		}

		private void SetNodeVisibilityRecursive(TreeNode node)
		{
			if (node.Tag is Entity)
			{
				Entity entity = node.Tag as Entity;

				entity.Visible = node.Checked;
			}

			foreach (TreeNode child in node.Nodes)
			{
				SetNodeVisibilityRecursive(child);
			}
		}

		private bool SearchTreeNodeByEntity (Entity entity, out TreeNode nodeOut)
		{
			nodeOut = null;

			foreach (TreeNode node in myTreeView1.Nodes)
			{
				bool res = SearchTreeNodeByEntityRecursive(entity, node, out nodeOut);
				if (res)
					return res;
			}

			return false;
		}

		private bool SearchTreeNodeByEntityRecursive (Entity entity, TreeNode parentNode, out TreeNode nodeOut)
		{
			nodeOut = null;

			if (parentNode.Tag == entity)
			{
				nodeOut = parentNode;
				return true;
			}

			foreach(TreeNode node in parentNode.Nodes)
			{
				bool res = SearchTreeNodeByEntityRecursive(entity, node, out nodeOut);
				if (res)
					return res;
			}

			return false;
		}

		private void myTreeView1_ItemDrag(object sender, ItemDragEventArgs e)
		{
			DoDragDrop(e.Item, DragDropEffects.Move);
		}

		private void myTreeView1_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = DragDropEffects.Move;
		}

		private void myTreeView1_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
			{
				TreeNode node = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");

				TreeView treeView = (TreeView)sender;

				Point pt = treeView.PointToClient(new Point(e.X, e.Y));
				TreeNode destinationNode = treeView.GetNodeAt(pt);

				if (destinationNode.Tag is Entity && node.Tag is Entity)
				{
					Entity dest = (Entity)destinationNode.Tag;
					Entity source = (Entity)node.Tag;

					// It is forbidden to move the root

					if (source.parent == null)
					{
						return;
					}

					if (source != dest)
					{
						source.parent.Children.Remove(source);
						source.parent = dest;
						dest.Children.Add(source);

						PopulateTree();
						entityBox1.Invalidate();
					}
				}
			}
		}

		private void myTreeView1_KeyDown(object sender, KeyEventArgs e)
		{
			TreeNode sourceNode = myTreeView1.SelectedNode;
			TreeNode neighbourNode = null;

			if (e.Control)
			{
				switch (e.KeyCode)
				{
					case Keys.Up:
						neighbourNode = sourceNode.PrevNode;
						break;

					case Keys.Down:
						neighbourNode = sourceNode.NextNode;
						break;
				}
			}
			else
			{
				switch (e.KeyCode)
				{
					case Keys.Delete:
						Entity entity = sourceNode.Tag as Entity;
						if (entity.Type != EntityType.Root)
						{
							entityBox1.RemoveEntity(entity);
							entityBox1.Invalidate();
						}
						break;
				}
			}

			if (neighbourNode != null)
			{
				if (neighbourNode.Tag is Entity)
				{
					SwapNodes(sourceNode, neighbourNode);

					myTreeView1.SelectedNode = neighbourNode;
					myTreeView1.Focus();

					entityBox1.Invalidate();
				}
			}
		}

		private void SwapNodes(TreeNode a, TreeNode b)
		{
			string temp = a.Text;
			a.Text = b.Text;
			b.Text = temp;

			Entity aEnt = a.Tag as Entity;
			Entity bEnt = b.Tag as Entity;

			object tempTag = a.Tag;
			a.Tag = b.Tag;
			b.Tag = tempTag;

			Entity parent = aEnt.parent;

			int aCtrlIdx = parent.Children.IndexOf(aEnt);
			int bCtrlIdx = parent.Children.IndexOf(bEnt);

			Entity tempEntity = parent.Children[aCtrlIdx];
			parent.Children[aCtrlIdx] = parent.Children[bCtrlIdx];
			parent.Children[bCtrlIdx] = tempEntity;
		}

		#endregion


		#region "Tools"

		private void toolStripButton2_Click(object sender, EventArgs e)
		{
			entityBox1.MergeSelectedWires(false);
		}

		private void toolStripButton3_Click(object sender, EventArgs e)
		{
			entityBox1.MergeSelectedWires(true);
		}

		private void deleteAllEntitiesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.DeleteAllEntites();
		}

		private void routeSingleWireToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Entity vias1 = null;
			Entity vias2 = null;
			List<Entity> shapes = new List<Entity>();

			//
			// Get selected vias
			//

			List<Entity> selected = entityBox1.GetSelected();

			foreach (Entity entity in selected)
			{
				if (entity.IsVias())
				{
					if (vias1 == null)
					{
						vias1 = entity;
						continue;
					}

					if (vias2 == null)
					{
						vias2 = entity;
						continue;
					}

					if (vias1 != null && vias2 != null)
					{
						break;
					}
				}
			}

			//
			// Check 
			//

			if (vias1 == null || vias2 == null)
			{
				MessageBox.Show("Two selected vias required", "Error",
					MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}

			//
			// Get shapes
			//

			foreach (Entity entity in entityBox1.GetEntities())
			{
				if (entity.IsCell() || entity.IsRegion())
				{
					shapes.Add(entity);
				}
			}

			//
			// Add wire corners as artifical cells
			//

			foreach (Entity entity in entityBox1.GetEntities())
			{
				if (entity.IsWire())
				{
					Entity artifical1 = new Entity();

					artifical1.Type = EntityType.CellOther;
					artifical1.LambdaX = entity.LambdaX;
					artifical1.LambdaY = entity.LambdaY;
					artifical1.LambdaWidth = 1;
					artifical1.LambdaHeight = 1;

					Entity artifical2 = new Entity();

					artifical2.Type = EntityType.CellOther;
					artifical2.LambdaX = entity.LambdaEndX;
					artifical2.LambdaY = entity.LambdaEndY;
					artifical2.LambdaWidth = 1;
					artifical2.LambdaHeight = 1;

					shapes.Add(artifical1);
					shapes.Add(artifical2);
				}
			}

			Cursor = Cursors.WaitCursor;

			List<Entity> wires = entityBox1.Route(vias1, vias2, shapes, true);

			Cursor = Cursors.Default;

			vias1.Selected = false;
			vias2.Selected = false;
		}

		private void removeSmallWiresToolStripMenuItem_Click(object sender, EventArgs e)
		{
			int wireCount = entityBox1.GetWireCount();

			if ( wireCount == 0)
			{
				MessageBox.Show("No wires!");
				return;
			}

			FormEnterValue enterValue = new FormEnterValue("Remove wires smaller than (lambda):");

			enterValue.FormClosed += EnterRemoveSize_FormClosed;
			enterValue.ShowDialog();
		}

		private void EnterRemoveSize_FormClosed(object sender, FormClosedEventArgs e)
		{
			FormEnterValue enterValue = (FormEnterValue)sender;

			if (enterValue.DialogResult == DialogResult.OK)
			{
				float smallerThanSize = (float)enterValue.Value;

				entityBox1.RemoveSmallWires(smallerThanSize);
			}
		}

		private void removeNotOrthogonalWiresToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.RemoveNonOrthogonalWires();
		}

		private void entityLocatorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FormEntityLocator form = new FormEntityLocator(entityBox1);
			form.Show();
		}

		/// <summary>
		/// Places vias on the ends of the highlighted wires.
		/// </summary>
		private void addViasAtTheWireEndsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var wires = entityBox1.GetSelectedWires();
			foreach (var wire in wires)
			{
				var start = entityBox1.LambdaToScreen(wire.LambdaX, wire.LambdaY);
				var end = entityBox1.LambdaToScreen(wire.LambdaEndX, wire.LambdaEndY);

				entityBox1.AddVias(EntityType.ViasConnect, start.X, start.Y, Color.Black);
				entityBox1.AddVias(EntityType.ViasConnect, end.X, end.Y, Color.Black);
			}
			entityBox1.Invalidate();
		}

		private void bulkRenameToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FormBulkRename form = new FormBulkRename(entityBox1);
			form.ShowDialog();
		}

		#endregion


		#region "Machine Learning"

		private NeuralNetwork.EntityNetwork nn = null;
		private Bitmap ML_sourceBitmap;
		private Point windowsPos;

		private void createMLModelToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FormCreateMLModel form = new FormCreateMLModel(false, null);
			form.FormClosed += Form_FormCreateMLClosed;
			form.ShowDialog();
		}

		private void Form_FormCreateMLClosed(object sender, FormClosedEventArgs e)
		{
			FormCreateMLModel form = (FormCreateMLModel)sender;

			if (form.nn == null)
				return;

			if (form.nn._state.features.Count == 0)
			{
				MessageBox.Show("The model is missing features, you need to add at least one for the correct operation of the model.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}

			nn = form.nn;
			toolStripStatusLabel17.Text = "Not saved!";
		}

		private void loadMLModelToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DialogResult res = openFileDialog2.ShowDialog();

			if (res == DialogResult.OK)
			{
				string filename = openFileDialog2.FileName;

				XmlSerializer ser = new XmlSerializer(typeof(EntityNetwork.State));

				using (FileStream fs = new FileStream(filename, FileMode.Open))
				{
					nn = new EntityNetwork((EntityNetwork.State)ser.Deserialize(fs));
					toolStripStatusLabel17.Text = Path.GetFileName(filename);
				}
			}
		}

		private void saveMLModelToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (nn == null)
			{
				MessageBox.Show("Load or create neural model first", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			DialogResult res = saveFileDialog2.ShowDialog();

			if (res == DialogResult.OK)
			{
				string filename = saveFileDialog2.FileName;

				XmlSerializer ser = new XmlSerializer(typeof(EntityNetwork.State));

				using (FileStream fs = new FileStream(filename, FileMode.Create))
				{
					ser.Serialize(fs, nn._state);
					toolStripStatusLabel17.Text = Path.GetFileName(filename);
				}
			}
		}

		private void trainModelToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (nn == null)
			{
				MessageBox.Show("Load a trained neural model", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}

			if (entityBox1.Image == null)
			{
				MessageBox.Show("Load the original image into Image Layer0.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}

			FormTrainMLModel form = new FormTrainMLModel(nn, entityBox1.Image);
			form.ShowDialog();
		}

		private void runModelToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (nn == null)
			{
				MessageBox.Show("Load a trained neural model", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}

			if (entityBox1.Image == null)
			{
				MessageBox.Show("Load the original image into Image Layer0.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}

			ML_sourceBitmap = (Bitmap)entityBox1.Image;
			windowsPos = new Point(0, 0);

			backgroundWorkerML.RunWorkerAsync();

			FormRunMLModel form = new FormRunMLModel();
			form.FormClosed += Form_FormClosed;
			form.Show();
		}

		private void Form_FormClosed(object sender, FormClosedEventArgs e)
		{
			backgroundWorkerML.CancelAsync();

			PopulateTree();
			entityBox1.Invalidate();
		}

		private void toolStripStatusLabel16_Click(object sender, EventArgs e)
		{
			if (nn != null)
			{
				FormCreateMLModel form = new FormCreateMLModel(true, nn);
				form.ShowDialog();
			}
		}

		/// <summary>
		/// The main worker, which extracts the features from the original image, tries to find out from the neural network what this feature is,
		/// and if the neural network recognizes it, it takes the entities from the feature and adds it to the `insertionNode`.
		/// Entities from the fixture are placed in the center of the window under test.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void backgroundWorkerML_DoWork(object sender, DoWorkEventArgs e)
		{
			while (!backgroundWorkerML.CancellationPending)
			{
				// Pick a random sub-image (window)

				Rectangle rect = new Rectangle();

				rect.Width = nn.GetWindowSize();
				rect.Height = nn.GetWindowSize();

				bool zigzag = true;

				if (zigzag)
				{
					rect.X = windowsPos.X;
					rect.Y = windowsPos.Y;

					windowsPos.X += 1;
					if (windowsPos.X >= (ML_sourceBitmap.Width - rect.Width))
					{
						windowsPos.X = 0;
						windowsPos.Y += 1;

						if (windowsPos.Y >= (ML_sourceBitmap.Height - rect.Height))
						{
							Console.WriteLine("Zigzag scan complete");
							return;
						}
					}
				}
				else
				{
					rect.X = rnd.Next(0, ML_sourceBitmap.Width - rect.Width - 1);
					rect.Y = rnd.Next(0, ML_sourceBitmap.Height - rect.Height - 1);
				}

				Bitmap subImage = ML_sourceBitmap.Clone(rect, ML_sourceBitmap.PixelFormat);

				// Ask the neural network what it is

				int id = nn.Guess(subImage, false);

				EntityNetwork.Feature feature = nn.GetFeature(id);

				if (feature != null)
				{
					// If the neural network has detected the feature, get a list of the feature entities and center them in the sub-image window.

					if (feature.entities != null)
					{
						XmlSerializer ser = new XmlSerializer(typeof(List<Entity>));

						using (StringReader textReader = new StringReader(feature.entities))
						{
							PointF center = entityBox1.ImageToLambda(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
							List<Entity> entities = (List<Entity>)ser.Deserialize(textReader);
							EntityAligner.CenterFeatureEntities(center, entities);
							entityBox1.root.Children.AddRange(entities);

							Console.WriteLine("Found " + feature.name);

							//entityBox1.Invalidate();
						}
					}
				}
			}

		}





		#endregion "Machine Learning"

		private void addLayerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			entityBox1.AddLayer();
		}


		#region "Cells"

		private void loadLibraryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (openFileDialog2.ShowDialog() == DialogResult.OK)
			{
				string filename = openFileDialog2.FileName;
				cells_db = CellSupport.DeserializeFromFile(filename);
			}
		}

		private void saveLibraryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (saveFileDialog2.ShowDialog() == DialogResult.OK)
			{
				string filename = saveFileDialog2.FileName;
				CellSupport.SerializeToFile(cells_db, filename);
			}
		}

		private void manageCellsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (cells_editor != null)
			{
				cells_editor.Focus();
			}
			else
			{
				cells_editor = new FormCells(entityBox1, cells_db);
				cells_editor.FormClosed += Cells_editor_FormClosed;
				cells_editor.Show();
			}
		}

		private void addCellFromSelectionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (addCellFromSelectionToolStripMenuItem.Checked)
			{
				addCellFromSelectionToolStripMenuItem.Checked = false;
			}
			else
			{
				addCellFromSelectionToolStripMenuItem.Checked = true;
			}
		}

		private void rotateCellToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CellSupport.RotateCell(entityBox1);
		}

		private void flipCellToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CellSupport.FlipCell(entityBox1);
		}

		#endregion "Cells"

		#region "Simulation"

		private void SimStep()
		{
			Console.WriteLine("step");
			sim.Step();
			var dump = sim.GetDump();

			ValueChangeData[] vcd = new ValueChangeData[dump.Count];

			int i = 0;
			foreach (var entity in dump.Keys)
			{
				vcd[i] = new ValueChangeData();
				vcd[i].name = entity.Label;
				vcd[i].values = dump[entity].ToArray();
				i++;
			}

			if (waves != null)
			{
				waves.Update(vcd, 0);
			}
		}

		private void SimRunStop()
		{
			Console.WriteLine("Run/Stop sim");
		}

		private void SimOpenWaves()
		{
			if (waves == null)
			{
				waves = new FormWaves();
				waves.FormClosed += Waves_FormClosed;
				waves.Show();
			}
			else
			{
				waves.Focus();
			}
		}

		private void Waves_FormClosed(object sender, FormClosedEventArgs e)
		{
			waves = null;
		}

		private void stepToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SimStep();
		}

		private void runToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SimRunStop();
		}

		private void wavesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SimOpenWaves();
		}

		private void resetToolStripMenuItem_Click(object sender, EventArgs e)
		{
			sim.Reset();
		}

		#endregion "Simulation"


	}       // Form1


	internal class TimeSpentStats
	{
		public Font normalFont;
		public Font penaltyFont;

		public string penaltyText = "Go work lazy bitch!";
		public int activityPenalty = 10;
		public int seconds = 0;

		/// <summary>
		/// Activiy updates when user: clicks mouse
		/// </summary>

		public DateTime lastActivityTime = DateTime.Now;
	}


}
