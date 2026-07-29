using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using DerouteSharp.Collab;

namespace DerouteSharp
{
	public partial class FormSettings : Form
	{
		private GlobalSettings globalSettings;
		private ColorSettings colorSettings;
		private PrioritySettings prioritySettings;
		private OpacitySettings opacitySettings;
		private SizeSettings sizeSettings;
		private ShapeSettings shapeSettings;
		private CollabMcpSettings collabMcpSettings;

		[Serializable()]
		public class SerializedCollabMcpSettings
		{
			public bool Enabled = false;
			public string ServerUrl = "http://localhost:5000";
			public string ApiKey = string.Empty;
			public string UserId = string.Empty;
			public string SessionId = string.Empty;
			public string Username = string.Empty;
			public int ReconnectDelayMs = 2000;
			public int MaxReconnectAttempts = 50;
		}

		[Serializable()]
		public class SerializedSettings
		{
			public GlobalSettings globalSettings = new GlobalSettings(null);
			public ColorSettings colorSettings = new ColorSettings(null);
			public PrioritySettings prioritySettings = new PrioritySettings(null);
			public OpacitySettings opacitySettings = new OpacitySettings(null);
			public SizeSettings sizeSettings = new SizeSettings(null);
			public ShapeSettings shapeSettings = new ShapeSettings(null);
			public SerializedCollabMcpSettings collabMcpSettings = new SerializedCollabMcpSettings();
		}

		private EntityBox savedEntityBox;

		public FormSettings(EntityBox entityBox, CollabSettings collabSettings = null)
		{
			InitializeComponent();

			savedEntityBox = entityBox;

			propertyGridEntityBox.SelectedObject = entityBox;

			globalSettings = new GlobalSettings(entityBox);
			propertyGridGlobal.SelectedObject = globalSettings;

			colorSettings = new ColorSettings(entityBox);
			propertyGridColors.SelectedObject = colorSettings;

			prioritySettings = new PrioritySettings(entityBox);
			propertyGridPriority.SelectedObject = prioritySettings;

			opacitySettings = new OpacitySettings(entityBox);
			propertyGridOpacity.SelectedObject = opacitySettings;

			sizeSettings = new SizeSettings(entityBox);
			propertyGridSize.SelectedObject = sizeSettings;

			shapeSettings = new ShapeSettings(entityBox);
			propertyGridShape.SelectedObject = shapeSettings;

			if (collabSettings != null)
			{
				collabMcpSettings = new CollabMcpSettings(collabSettings);
				propertyGridCollabMcp.SelectedObject = collabMcpSettings;
			}
		}

		private void FormSettings_KeyDown(object sender, KeyEventArgs e)
		{
			if ( e.KeyCode == Keys.Escape)
			{
				DialogResult = DialogResult.Cancel;
				Close();
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			globalSettings.Save();
			colorSettings.Save();
			prioritySettings.Save();
			opacitySettings.Save();
			sizeSettings.Save();
			shapeSettings.Save();

			if (collabMcpSettings != null)
			{
				collabMcpSettings.Save();
			}

			SaveSettings(savedEntityBox);

			DialogResult = DialogResult.OK;
			Close();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}


		[Serializable()]
		public class GlobalSettings
		{
			[Description("Select the new added entities (from the File menu) so that you can move them around, for example.")]
			public bool SelectEntitiesAfterAdd { get; set; }
			[Description("Make loaded images in grayscale, to save memory.")]
			public bool Grayscale { get; set; }
			[Description("Use accelerated rendering of images using partitioning into tiles. The source image is split into 16x16 tiles")]
			public bool OptimizeTilemap { get; set; }
			[Description("How many pixels will equal one lambda.")]
			public float Lambda { get; set; }
			public bool HideGrid { get; set; }
			[Description("The size of one grid cell in lambda.")]
			public float GridSize { get; set; }
			[Description("Drawing with sticking to the grid.")]
			public bool SnapToGrid { get; set; }
			[Description("Hide the ruler in the lambda at the bottom right. I don't remember it ever being useful.")]
			public bool HideLambdaMetrics { get; set; }
			[Description("The position of the text label for the cells/units.")]
			public TextAlignment CellTextAlignment { get; set; }
			[Description("The position of the text label for the viases.")]
			public TextAlignment ViasTextAlignment { get; set; }
			[Description("The position of the text label for the wires.")]
			public TextAlignment WireTextAlignment { get; set; }
			[Description("This setting allows for automatic traverse (Tier 1) when any wire is selected. This is similar to pressing F10 after selecting a wire.")]
			public bool WireSelectionAutoTraverse { get; set; }
			[Description("Text that is automatically inserted into the Label property of the ViasGround entity being added.")]
			public string ViasGroundText { get; set; }
			[Description("Text that is automatically inserted into the Label property of the ViasPower entity being added.")]
			public string ViasPowerText { get; set; }
			[Description("Select the cell along with the ports. Works only when selected by mouse click.")]
			public bool SelectCellWithPorts { get; set; }

			[Description("Enable the minimap showing the current viewport position on the loaded image.")]
			public bool MinimapEnabled { get; set; }
			[Description("Size of the minimap as a fraction of the EntityBox width (0.05 to 0.5).")]
			public float MinimapSizePercent { get; set; }
			[Description("Position of the minimap (0=TopLeft, 1=TopRight, 2=BottomLeft, 3=BottomRight).")]
			public int MinimapPosition { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color MinimapViewportColor { get; set; }
			[Description("Opacity of the viewport rectangle on the minimap (0-255).")]
			public int MinimapViewportOpacity { get; set; }
			[Description("Minimum size of the minimap in pixels.")]
			public int MinimapMinSize { get; set; }
			[Description("Radius in lambda for checking neighbor viases when adding new vias. Vias within this distance will not be created.")]
			public float ViasNeighborRadius { get; set; }

			private EntityBox savedEntityBox;
			public GlobalSettings() { }

			public GlobalSettings(EntityBox entityBox)
			{
				if (entityBox == null)
					return;

				savedEntityBox = entityBox;

				SelectEntitiesAfterAdd = entityBox.SelectEntitiesAfterAdd;
				Grayscale = entityBox.Grayscale;
				OptimizeTilemap = entityBox.OptimizeTilemap;
				Lambda = entityBox.Lambda;
				HideGrid = entityBox.HideGrid;
				GridSize = entityBox.GridSize;
				SnapToGrid = entityBox.SnapToGrid;
				HideLambdaMetrics = entityBox.HideLambdaMetrics;
				CellTextAlignment = entityBox.CellTextAlignment;
				ViasTextAlignment = entityBox.ViasTextAlignment;
				WireTextAlignment = entityBox.WireTextAlignment;
				WireSelectionAutoTraverse = entityBox.WireSelectionAutoTraverse;
				ViasGroundText = entityBox.ViasGroundText;
				ViasPowerText = entityBox.ViasPowerText;
				SelectCellWithPorts = entityBox.SelectCellWithPorts;

				MinimapEnabled = entityBox.MinimapEnabled;
				MinimapSizePercent = entityBox.MinimapSizePercent;
				MinimapPosition = (int)entityBox.MinimapPosition;
				MinimapViewportColor = entityBox.MinimapViewportColor;
				MinimapViewportOpacity = entityBox.MinimapViewportOpacity;
				MinimapMinSize = entityBox.MinimapMinSize;

				ViasNeighborRadius = entityBox.ViasNeighborRadius;
			}

			public void Save()
			{
				savedEntityBox.SelectEntitiesAfterAdd = SelectEntitiesAfterAdd;
				savedEntityBox.Grayscale = Grayscale;
				savedEntityBox.OptimizeTilemap = OptimizeTilemap;
				savedEntityBox.Lambda = Lambda;
				savedEntityBox.HideGrid = HideGrid;
				savedEntityBox.GridSize = GridSize;
				savedEntityBox.SnapToGrid = SnapToGrid;
				savedEntityBox.HideLambdaMetrics = HideLambdaMetrics;
				savedEntityBox.CellTextAlignment = CellTextAlignment;
				savedEntityBox.ViasTextAlignment = ViasTextAlignment;
				savedEntityBox.WireTextAlignment = WireTextAlignment;
				savedEntityBox.WireSelectionAutoTraverse = WireSelectionAutoTraverse;
				savedEntityBox.ViasGroundText = ViasGroundText;
				savedEntityBox.ViasPowerText = ViasPowerText;
				savedEntityBox.SelectCellWithPorts = SelectCellWithPorts;

				savedEntityBox.MinimapEnabled = MinimapEnabled;
				savedEntityBox.MinimapSizePercent = MinimapSizePercent;
				savedEntityBox.MinimapPosition = (MinimapPosition)MinimapPosition;
				savedEntityBox.MinimapViewportColor = MinimapViewportColor;
				savedEntityBox.MinimapViewportOpacity = MinimapViewportOpacity;
				savedEntityBox.MinimapMinSize = MinimapMinSize;

				savedEntityBox.ViasNeighborRadius = ViasNeighborRadius;
			}
		}



		[Serializable()]
		public class ColorSettings
		{
			[XmlElement(Type = typeof(XmlColor))]
			public Color SelectionBoxColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color ViasInputColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color ViasOutputColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color ViasInoutColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color ViasConnectColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color ViasFloatingColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color ViasPowerColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color ViasGroundColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color WireInterconnectColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color WirePowerColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color WireGroundColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color CellNotColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color CellBufferColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color CellMuxColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color CellLogicColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color CellAdderColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color CellBusSuppColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color CellFlipFlopColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color CellLatchColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color CellOtherColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color UnitRegfileColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color UnitMemoryColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color UnitCustomColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color SelectionColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color ViasOverrideColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color WireOverrideColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color CellOverrideColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color RegionOverrideColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color HighZColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color ZeroColor { get; set; }
			[XmlElement(Type = typeof(XmlColor))]
			public Color OneColor { get; set; }

			private EntityBox savedEntityBox;

			public ColorSettings () { }

			public ColorSettings(EntityBox entityBox)
			{
				if (entityBox == null)
					return;

				savedEntityBox = entityBox;

				SelectionBoxColor = entityBox.SelectionBoxColor;
				ViasInputColor = entityBox.ViasInputColor;
				ViasOutputColor = entityBox.ViasOutputColor;
				ViasInoutColor = entityBox.ViasInoutColor;
				ViasConnectColor = entityBox.ViasConnectColor;
				ViasFloatingColor = entityBox.ViasFloatingColor;
				ViasPowerColor = entityBox.ViasPowerColor;
				ViasGroundColor = entityBox.ViasGroundColor;
				WireInterconnectColor = entityBox.WireInterconnectColor;
				WirePowerColor = entityBox.WirePowerColor;
				WireGroundColor = entityBox.WireGroundColor;
				CellNotColor = entityBox.CellNotColor;
				CellBufferColor = entityBox.CellBufferColor;
				CellMuxColor = entityBox.CellMuxColor;
				CellLogicColor = entityBox.CellLogicColor;
				CellAdderColor = entityBox.CellAdderColor;
				CellBusSuppColor = entityBox.CellBusSuppColor;
				CellFlipFlopColor = entityBox.CellFlipFlopColor;
				CellLatchColor = entityBox.CellLatchColor;
				CellOtherColor = entityBox.CellOtherColor;
				UnitRegfileColor = entityBox.UnitRegfileColor;
				UnitMemoryColor = entityBox.UnitMemoryColor;
				UnitCustomColor = entityBox.UnitCustomColor;
				SelectionColor = entityBox.SelectionColor;
				ViasOverrideColor = entityBox.ViasOverrideColor;
				WireOverrideColor = entityBox.WireOverrideColor;
				CellOverrideColor = entityBox.CellOverrideColor;
				RegionOverrideColor = entityBox.RegionOverrideColor;
				HighZColor = entityBox.HighZColor;
				ZeroColor = entityBox.ZeroColor;
				OneColor = entityBox.OneColor;
			}

			public void Save()
			{
				savedEntityBox.SelectionBoxColor = SelectionBoxColor;
				savedEntityBox.ViasInputColor = ViasInputColor;
				savedEntityBox.ViasOutputColor = ViasOutputColor;
				savedEntityBox.ViasInoutColor = ViasInoutColor;
				savedEntityBox.ViasConnectColor = ViasConnectColor;
				savedEntityBox.ViasFloatingColor = ViasFloatingColor;
				savedEntityBox.ViasPowerColor = ViasPowerColor;
				savedEntityBox.ViasGroundColor = ViasGroundColor;
				savedEntityBox.WireInterconnectColor = WireInterconnectColor;
				savedEntityBox.WirePowerColor = WirePowerColor;
				savedEntityBox.WireGroundColor = WireGroundColor;
				savedEntityBox.CellNotColor = CellNotColor;
				savedEntityBox.CellBufferColor = CellBufferColor;
				savedEntityBox.CellMuxColor = CellMuxColor;
				savedEntityBox.CellLogicColor = CellLogicColor;
				savedEntityBox.CellAdderColor = CellAdderColor;
				savedEntityBox.CellBusSuppColor = CellBusSuppColor;
				savedEntityBox.CellFlipFlopColor = CellFlipFlopColor;
				savedEntityBox.CellLatchColor = CellLatchColor;
				savedEntityBox.CellOtherColor = CellOtherColor;
				savedEntityBox.UnitRegfileColor = UnitRegfileColor;
				savedEntityBox.UnitMemoryColor = UnitMemoryColor;
				savedEntityBox.UnitCustomColor = UnitCustomColor;
				savedEntityBox.SelectionColor = SelectionColor;
				savedEntityBox.ViasOverrideColor = ViasOverrideColor;
				savedEntityBox.WireOverrideColor = WireOverrideColor;
				savedEntityBox.CellOverrideColor = CellOverrideColor;
				savedEntityBox.RegionOverrideColor = RegionOverrideColor;
				savedEntityBox.HighZColor = HighZColor;
				savedEntityBox.ZeroColor = ZeroColor;
				savedEntityBox.OneColor = OneColor;
			}

		}


		[Serializable()]
		public class PrioritySettings
		{
			public int ViasPriority { get; set; }
			public int WirePriority { get; set; }
			public int CellPriority { get; set; }
			public int BeaconPriority { get; set; }
			public int RegionPriority { get; set; }
			public bool AutoPriority { get; set; }

			private EntityBox savedEntityBox;
			public PrioritySettings() { }

			public PrioritySettings(EntityBox entityBox)
			{
				if (entityBox == null)
					return;

				savedEntityBox = entityBox;

				ViasPriority = entityBox.ViasPriority;
				WirePriority = entityBox.WirePriority;
				CellPriority = entityBox.CellPriority;
				BeaconPriority = entityBox.BeaconPriority;
				RegionPriority = entityBox.RegionPriority;
				AutoPriority = entityBox.AutoPriority;
			}

			public void Save()
			{
				savedEntityBox.ViasPriority = ViasPriority;
				savedEntityBox.WirePriority = WirePriority;
				savedEntityBox.CellPriority = CellPriority;
				savedEntityBox.BeaconPriority = BeaconPriority;
				savedEntityBox.RegionPriority = RegionPriority;
				savedEntityBox.AutoPriority = AutoPriority;
			}
		}


		[Serializable()]
		public class OpacitySettings
		{
			public int ViasOpacity { get; set; }
			public int WireOpacity { get; set; }
			public int CellOpacity { get; set; }

			private EntityBox savedEntityBox;

			public OpacitySettings() { }

			public OpacitySettings(EntityBox entityBox)
			{
				if (entityBox == null)
					return;

				savedEntityBox = entityBox;

				ViasOpacity = entityBox.ViasOpacity;
				WireOpacity = entityBox.WireOpacity;
				CellOpacity = entityBox.CellOpacity;
			}

			public void Save()
			{
				savedEntityBox.ViasOpacity = ViasOpacity;
				savedEntityBox.WireOpacity = WireOpacity;
				savedEntityBox.CellOpacity = CellOpacity;
			}
		}


		[Serializable()]
		public class SizeSettings
		{
			public int ViasBaseSize { get; set; }
			public int WireBaseSize { get; set; }

			private EntityBox savedEntityBox;

			public SizeSettings() { }

			public SizeSettings(EntityBox entityBox)
			{
				if (entityBox == null)
					return;

				savedEntityBox = entityBox;

				ViasBaseSize = entityBox.ViasBaseSize;
				WireBaseSize = entityBox.WireBaseSize;
			}

			public void Save()
			{
				savedEntityBox.ViasBaseSize = ViasBaseSize;
				savedEntityBox.WireBaseSize = WireBaseSize;
			}
		}


		[Serializable()]
		public class ShapeSettings
		{
			public ViasShape ViasShape { get; set; }

			private EntityBox savedEntityBox;

			public ShapeSettings() { }

			public ShapeSettings(EntityBox entityBox)
			{
				if (entityBox == null)
					return;

				savedEntityBox = entityBox;

				ViasShape = entityBox.ViasShape;
			}

			public void Save()
			{
				savedEntityBox.ViasShape = ViasShape;
			}
		}


		[Serializable()]
		public class CollabMcpSettings
		{
			[Description("Enable collaboration via CollabMCP")]
			public bool Enabled { get; set; }
			[Description("URL of the CollabMCP server")]
			public string ServerUrl { get; set; } = "http://localhost:5000";
			[Description("API key for authentication with the CollabMCP server")]
			public string ApiKey { get; set; } = string.Empty;
			[Description("Unique identifier for the current user")]
			public string UserId { get; set; }
			[Description("Session ID for the current collaboration session")]
			public string SessionId { get; set; }
			[Description("Display name shown to other collaborators")]
			public string Username { get; set; }
			[Description("Delay in milliseconds before attempting to reconnect after a connection loss")]
			public int ReconnectDelayMs { get; set; } = 2000;
			[Description("Maximum number of reconnection attempts before giving up")]
			public int MaxReconnectAttempts { get; set; } = 50;

			private CollabSettings _collabSettings;

			public CollabMcpSettings() { }

			public CollabMcpSettings(CollabSettings collabSettings)
			{
				if (collabSettings == null)
					return;

				_collabSettings = collabSettings;

				Enabled = collabSettings.Enabled;
				ServerUrl = collabSettings.ServerUrl;
				ApiKey = collabSettings.ApiKey;
				UserId = collabSettings.UserId;
				SessionId = collabSettings.SessionId;
				Username = collabSettings.Username;
				ReconnectDelayMs = collabSettings.ReconnectDelayMs;
				MaxReconnectAttempts = collabSettings.MaxReconnectAttempts;
			}

			public void Save()
			{
				if (_collabSettings == null)
					return;

				_collabSettings.Enabled = Enabled;
				_collabSettings.ServerUrl = ServerUrl;
				_collabSettings.ApiKey = ApiKey;
				_collabSettings.UserId = UserId;
				_collabSettings.SessionId = SessionId;
				_collabSettings.Username = Username;
				_collabSettings.ReconnectDelayMs = ReconnectDelayMs;
				_collabSettings.MaxReconnectAttempts = MaxReconnectAttempts;
			}
		}


		public static void LoadSettings (EntityBox entityBox)
		{
			Properties.Settings settings = Properties.Settings.Default;

			// Load global settings

			GlobalSettings global = new GlobalSettings(entityBox);

			global.SelectEntitiesAfterAdd = settings.SelectEntitiesAfterAdd;
			global.Grayscale = settings.Grayscale;
			global.OptimizeTilemap = settings.OptimizeTilemap;
			global.Lambda = settings.Lambda;
			global.HideGrid = settings.HideGrid;
			global.GridSize = settings.GridSize;
			global.SnapToGrid = settings.SnapToGrid;
			global.HideLambdaMetrics = settings.HideLambdaMetrics;
			global.CellTextAlignment = (TextAlignment)settings.CellTextAlignment;
			global.ViasTextAlignment = (TextAlignment)settings.ViasTextAlignment;
			global.WireTextAlignment = (TextAlignment)settings.WireTextAlignment;
			global.ViasGroundText = settings.ViasGroundText;
			global.ViasPowerText = settings.ViasPowerText;
			global.SelectCellWithPorts = settings.SelectCellWithPorts;

			global.MinimapEnabled = settings.MinimapEnabled;
			global.MinimapSizePercent = settings.MinimapSizePercent;
			global.MinimapPosition = settings.MinimapPosition;
			global.MinimapViewportColor = settings.MinimapViewportColor;
			global.MinimapViewportOpacity = settings.MinimapViewportOpacity;
			global.MinimapMinSize = settings.MinimapMinSize;
			global.ViasNeighborRadius = settings.ViasNeighborRadius;

			global.Save();

			// Load color settings 

			ColorSettings color = new ColorSettings(entityBox);

			color.SelectionBoxColor = settings.SelectionBoxColor;
			color.ViasInputColor = settings.ViasInputColor;
			color.ViasOutputColor = settings.ViasOutputColor;
			color.ViasInoutColor = settings.ViasInoutColor;
			color.ViasConnectColor = settings.ViasConnectColor;
			color.ViasFloatingColor = settings.ViasFloatingColor;
			color.ViasPowerColor = settings.ViasPowerColor;
			color.ViasGroundColor = settings.ViasGroundColor;
			color.WireInterconnectColor = settings.WireInterconnectColor;
			color.WirePowerColor = settings.WirePowerColor;
			color.WireGroundColor = settings.WireGroundColor;
			color.CellNotColor = settings.CellNotColor;
			color.CellBufferColor = settings.CellBufferColor;
			color.CellMuxColor = settings.CellMuxColor;
			color.CellLogicColor = settings.CellLogicColor;
			color.CellAdderColor = settings.CellAdderColor;
			color.CellBusSuppColor = settings.CellBusSuppColor;
			color.CellFlipFlopColor = settings.CellFlipFlopColor;
			color.CellLatchColor = settings.CellLatchColor;
			color.CellOtherColor = settings.CellOtherColor;
			color.UnitRegfileColor = settings.UnitRegfileColor;
			color.UnitMemoryColor = settings.UnitMemoryColor;
			color.UnitCustomColor = settings.UnitCustomColor;
			color.SelectionColor = settings.SelectionColor;
			color.ViasOverrideColor = settings.ViasOverrideColor;
			color.WireOverrideColor = settings.WireOverrideColor;
			color.CellOverrideColor = settings.CellOverrideColor;
			color.RegionOverrideColor = settings.RegionOverrideColor;
			color.ZeroColor = settings.ZeroColor;
			color.OneColor = settings.OneColor;
			color.HighZColor = settings.HighZColor;

			color.Save();

			// Load priority settings

			PrioritySettings priority = new PrioritySettings(entityBox);

			priority.ViasPriority = settings.ViasPriority;
			priority.WirePriority = settings.WirePriority;
			priority.CellPriority = settings.CellPriority;
			priority.BeaconPriority = settings.BeaconPriority;
			priority.RegionPriority = settings.RegionPriority;
			priority.AutoPriority = settings.AutoPriority;

			priority.Save();

			// Load opacity settings

			OpacitySettings opacity = new OpacitySettings(entityBox);

			opacity.ViasOpacity = settings.ViasOpacity;
			opacity.WireOpacity = settings.WireOpacity;
			opacity.CellOpacity = settings.CellOpacity;

			opacity.Save();

			// Load size settings

			SizeSettings size = new SizeSettings(entityBox);

			size.WireBaseSize = settings.WireBaseSize;
			size.ViasBaseSize =	settings.ViasBaseSize;

			size.Save();

			// Load shape settings

			ShapeSettings shape = new ShapeSettings(entityBox);

			shape.ViasShape = (ViasShape)settings.ViasShape;

			shape.Save();

			entityBox.Invalidate();
		}

		public static void SaveSettings (EntityBox entityBox)
		{
			Properties.Settings settings = Properties.Settings.Default;

			// Save global settings

			GlobalSettings global = new GlobalSettings(entityBox);

			settings.SelectEntitiesAfterAdd = global.SelectEntitiesAfterAdd;
			settings.Grayscale = global.Grayscale;
			settings.OptimizeTilemap = global.OptimizeTilemap;
			settings.Lambda = global.Lambda;
			settings.HideGrid = global.HideGrid;
			settings.GridSize = global.GridSize;
			settings.SnapToGrid = global.SnapToGrid;
			settings.HideLambdaMetrics = global.HideLambdaMetrics;
			settings.CellTextAlignment = (int)global.CellTextAlignment;
			settings.ViasTextAlignment = (int)global.ViasTextAlignment;
			settings.WireTextAlignment = (int)global.WireTextAlignment;
			settings.ViasGroundText = global.ViasGroundText;
			settings.ViasPowerText = global.ViasPowerText;
			settings.SelectCellWithPorts = global.SelectCellWithPorts;

			settings.MinimapEnabled = global.MinimapEnabled;
			settings.MinimapSizePercent = global.MinimapSizePercent;
			settings.MinimapPosition = global.MinimapPosition;
			settings.MinimapViewportColor = global.MinimapViewportColor;
			settings.MinimapViewportOpacity = global.MinimapViewportOpacity;
			settings.MinimapMinSize = global.MinimapMinSize;
			settings.ViasNeighborRadius = global.ViasNeighborRadius;

			// Save color settings

			ColorSettings color = new ColorSettings(entityBox);

			settings.SelectionBoxColor = color.SelectionBoxColor;
			settings.ViasInputColor = color.ViasInputColor;
			settings.ViasOutputColor = color.ViasOutputColor;
			settings.ViasInoutColor = color.ViasInoutColor;
			settings.ViasConnectColor = color.ViasConnectColor;
			settings.ViasFloatingColor = color.ViasFloatingColor;
			settings.ViasPowerColor = color.ViasPowerColor;
			settings.ViasGroundColor = color.ViasGroundColor;
			settings.WireInterconnectColor = color.WireInterconnectColor;
			settings.WirePowerColor = color.WirePowerColor;
			settings.WireGroundColor = color.WireGroundColor;
			settings.CellNotColor = color.CellNotColor;
			settings.CellBufferColor = color.CellBufferColor;
			settings.CellMuxColor = color.CellMuxColor;
			settings.CellLogicColor = color.CellLogicColor;
			settings.CellAdderColor = color.CellAdderColor;
			settings.CellBusSuppColor = color.CellBusSuppColor;
			settings.CellFlipFlopColor = color.CellFlipFlopColor;
			settings.CellLatchColor = color.CellLatchColor;
			settings.CellOtherColor = color.CellOtherColor;
			settings.UnitRegfileColor = color.UnitRegfileColor;
			settings.UnitMemoryColor = color.UnitMemoryColor;
			settings.UnitCustomColor = color.UnitCustomColor;
			settings.SelectionColor = color.SelectionColor;
			settings.ViasOverrideColor = color.ViasOverrideColor;
			settings.WireOverrideColor = color.WireOverrideColor;
			settings.CellOverrideColor = color.CellOverrideColor;
			settings.RegionOverrideColor = color.RegionOverrideColor;
			settings.ZeroColor = color.ZeroColor;
			settings.OneColor = color.OneColor;
			settings.HighZColor = color.HighZColor;

			// Save priority settings

			PrioritySettings priority = new PrioritySettings(entityBox);

			settings.ViasPriority = priority.ViasPriority;
			settings.WirePriority = priority.WirePriority;
			settings.CellPriority = priority.CellPriority;
			settings.BeaconPriority = priority.BeaconPriority;
			settings.RegionPriority = priority.RegionPriority;
			settings.AutoPriority = priority.AutoPriority;

			// Save opacity settings

			OpacitySettings opacity = new OpacitySettings(entityBox);

			settings.ViasOpacity = opacity.ViasOpacity;
			settings.WireOpacity = opacity.WireOpacity;
			settings.CellOpacity = opacity.CellOpacity;

			// Save size settings

			SizeSettings size = new SizeSettings(entityBox);

			settings.WireBaseSize = size.WireBaseSize;
			settings.ViasBaseSize = size.ViasBaseSize;

			// Save shape settings

			ShapeSettings shape = new ShapeSettings(entityBox);

			settings.ViasShape = (int)shape.ViasShape;

			settings.Save();
		}

		public static void LoadSettingsFromFile(string filename, EntityBox entityBox, CollabMcpSettings collabMcpSettings = null)
		{
			SerializedSettings settings = new SerializedSettings();

			XmlSerializer ser = new XmlSerializer(typeof(SerializedSettings));

			using (FileStream fs = new FileStream(filename, FileMode.Open))
			{
				settings = (SerializedSettings)ser.Deserialize(fs);
			}

			// Load global settings

			GlobalSettings global = new GlobalSettings(entityBox);

			global.SelectEntitiesAfterAdd = settings.globalSettings.SelectEntitiesAfterAdd;
			global.Grayscale = settings.globalSettings.Grayscale;
			global.OptimizeTilemap = settings.globalSettings.OptimizeTilemap;
			global.Lambda = settings.globalSettings.Lambda;
			global.HideGrid = settings.globalSettings.HideGrid;
			global.GridSize = settings.globalSettings.GridSize;
			global.SnapToGrid = settings.globalSettings.SnapToGrid;
			global.HideLambdaMetrics = settings.globalSettings.HideLambdaMetrics;
			global.CellTextAlignment = (TextAlignment)settings.globalSettings.CellTextAlignment;
			global.ViasTextAlignment = (TextAlignment)settings.globalSettings.ViasTextAlignment;
			global.WireTextAlignment = (TextAlignment)settings.globalSettings.WireTextAlignment;
			global.ViasGroundText = settings.globalSettings.ViasGroundText;
			global.ViasPowerText = settings.globalSettings.ViasPowerText;
			global.SelectCellWithPorts = settings.globalSettings.SelectCellWithPorts;

			global.MinimapEnabled = settings.globalSettings.MinimapEnabled;
			global.MinimapSizePercent = settings.globalSettings.MinimapSizePercent;
			global.MinimapPosition = settings.globalSettings.MinimapPosition;
			global.MinimapViewportColor = settings.globalSettings.MinimapViewportColor;
			global.MinimapViewportOpacity = settings.globalSettings.MinimapViewportOpacity;
			global.MinimapMinSize = settings.globalSettings.MinimapMinSize;
			global.ViasNeighborRadius = settings.globalSettings.ViasNeighborRadius;

			global.Save();

			// Load CollabMCP settings

			if (collabMcpSettings != null)
			{
				collabMcpSettings.Enabled = settings.collabMcpSettings.Enabled;
				collabMcpSettings.ServerUrl = settings.collabMcpSettings.ServerUrl;
				collabMcpSettings.ApiKey = settings.collabMcpSettings.ApiKey;
				collabMcpSettings.UserId = settings.collabMcpSettings.UserId;
				collabMcpSettings.SessionId = settings.collabMcpSettings.SessionId;
				collabMcpSettings.Username = settings.collabMcpSettings.Username;
				collabMcpSettings.ReconnectDelayMs = settings.collabMcpSettings.ReconnectDelayMs;
				collabMcpSettings.MaxReconnectAttempts = settings.collabMcpSettings.MaxReconnectAttempts;
			}

			// Load color settings 

			ColorSettings color = new ColorSettings(entityBox);

			color.SelectionBoxColor = settings.colorSettings.SelectionBoxColor;
			color.ViasInputColor = settings.colorSettings.ViasInputColor;
			color.ViasOutputColor = settings.colorSettings.ViasOutputColor;
			color.ViasInoutColor = settings.colorSettings.ViasInoutColor;
			color.ViasConnectColor = settings.colorSettings.ViasConnectColor;
			color.ViasFloatingColor = settings.colorSettings.ViasFloatingColor;
			color.ViasPowerColor = settings.colorSettings.ViasPowerColor;
			color.ViasGroundColor = settings.colorSettings.ViasGroundColor;
			color.WireInterconnectColor = settings.colorSettings.WireInterconnectColor;
			color.WirePowerColor = settings.colorSettings.WirePowerColor;
			color.WireGroundColor = settings.colorSettings.WireGroundColor;
			color.CellNotColor = settings.colorSettings.CellNotColor;
			color.CellBufferColor = settings.colorSettings.CellBufferColor;
			color.CellMuxColor = settings.colorSettings.CellMuxColor;
			color.CellLogicColor = settings.colorSettings.CellLogicColor;
			color.CellAdderColor = settings.colorSettings.CellAdderColor;
			color.CellBusSuppColor = settings.colorSettings.CellBusSuppColor;
			color.CellFlipFlopColor = settings.colorSettings.CellFlipFlopColor;
			color.CellLatchColor = settings.colorSettings.CellLatchColor;
			color.CellOtherColor = settings.colorSettings.CellOtherColor;
			color.UnitRegfileColor = settings.colorSettings.UnitRegfileColor;
			color.UnitMemoryColor = settings.colorSettings.UnitMemoryColor;
			color.UnitCustomColor = settings.colorSettings.UnitCustomColor;
			color.SelectionColor = settings.colorSettings.SelectionColor;
			color.ViasOverrideColor = settings.colorSettings.ViasOverrideColor;
			color.WireOverrideColor = settings.colorSettings.WireOverrideColor;
			color.CellOverrideColor = settings.colorSettings.CellOverrideColor;
			color.RegionOverrideColor = settings.colorSettings.RegionOverrideColor;
			color.ZeroColor = settings.colorSettings.ZeroColor;
			color.OneColor = settings.colorSettings.OneColor;
			color.HighZColor = settings.colorSettings.HighZColor;

			color.Save();

			// Load priority settings

			PrioritySettings priority = new PrioritySettings(entityBox);

			priority.ViasPriority = settings.prioritySettings.ViasPriority;
			priority.WirePriority = settings.prioritySettings.WirePriority;
			priority.CellPriority = settings.prioritySettings.CellPriority;
			priority.BeaconPriority = settings.prioritySettings.BeaconPriority;
			priority.RegionPriority = settings.prioritySettings.RegionPriority;
			priority.AutoPriority = settings.prioritySettings.AutoPriority;

			priority.Save();

			// Load opacity settings

			OpacitySettings opacity = new OpacitySettings(entityBox);

			opacity.ViasOpacity = settings.opacitySettings.ViasOpacity;
			opacity.WireOpacity = settings.opacitySettings.WireOpacity;
			opacity.CellOpacity = settings.opacitySettings.CellOpacity;

			opacity.Save();

			// Load size settings

			SizeSettings size = new SizeSettings(entityBox);

			size.WireBaseSize = settings.sizeSettings.WireBaseSize;
			size.ViasBaseSize = settings.sizeSettings.ViasBaseSize;

			size.Save();

			// Load shape settings

			ShapeSettings shape = new ShapeSettings(entityBox);

			shape.ViasShape = settings.shapeSettings.ViasShape;

			shape.Save();

			entityBox.Invalidate();
		}

		public static void SaveSettingsToFile(string filename, EntityBox entityBox, CollabMcpSettings collabMcpSettings = null)
		{
			SerializedSettings settings = new SerializedSettings();

			// Save global settings

			GlobalSettings global = new GlobalSettings(entityBox);

			settings.globalSettings.SelectEntitiesAfterAdd = global.SelectEntitiesAfterAdd;
			settings.globalSettings.Grayscale = global.Grayscale;
			settings.globalSettings.OptimizeTilemap = global.OptimizeTilemap;
			settings.globalSettings.Lambda = global.Lambda;
			settings.globalSettings.HideGrid = global.HideGrid;
			settings.globalSettings.GridSize = global.GridSize;
			settings.globalSettings.SnapToGrid = global.SnapToGrid;
			settings.globalSettings.HideLambdaMetrics = global.HideLambdaMetrics;
			settings.globalSettings.CellTextAlignment = global.CellTextAlignment;
			settings.globalSettings.ViasTextAlignment = global.ViasTextAlignment;
			settings.globalSettings.WireTextAlignment = global.WireTextAlignment;
			settings.globalSettings.ViasGroundText = global.ViasGroundText;
			settings.globalSettings.ViasPowerText = global.ViasPowerText;
			settings.globalSettings.SelectCellWithPorts = global.SelectCellWithPorts;

			settings.globalSettings.MinimapEnabled = global.MinimapEnabled;
			settings.globalSettings.MinimapSizePercent = global.MinimapSizePercent;
			settings.globalSettings.MinimapPosition = global.MinimapPosition;
			settings.globalSettings.MinimapViewportColor = new XmlColor(global.MinimapViewportColor);
			settings.globalSettings.MinimapViewportOpacity = global.MinimapViewportOpacity;
			settings.globalSettings.MinimapMinSize = global.MinimapMinSize;
			settings.globalSettings.ViasNeighborRadius = global.ViasNeighborRadius;

			// Save CollabMCP settings

			settings.collabMcpSettings.Enabled = collabMcpSettings?.Enabled ?? false;
			settings.collabMcpSettings.ServerUrl = collabMcpSettings?.ServerUrl ?? "http://localhost:5000";
			settings.collabMcpSettings.ApiKey = collabMcpSettings?.ApiKey ?? string.Empty;
			settings.collabMcpSettings.UserId = collabMcpSettings?.UserId ?? string.Empty;
			settings.collabMcpSettings.SessionId = collabMcpSettings?.SessionId ?? string.Empty;
			settings.collabMcpSettings.Username = collabMcpSettings?.Username ?? string.Empty;
			settings.collabMcpSettings.ReconnectDelayMs = collabMcpSettings?.ReconnectDelayMs ?? 2000;
			settings.collabMcpSettings.MaxReconnectAttempts = collabMcpSettings?.MaxReconnectAttempts ?? 50;

			// Save color settings

			ColorSettings color = new ColorSettings(entityBox);

			settings.colorSettings.SelectionBoxColor = color.SelectionBoxColor;
			settings.colorSettings.ViasInputColor = color.ViasInputColor;
			settings.colorSettings.ViasOutputColor = color.ViasOutputColor;
			settings.colorSettings.ViasInoutColor = color.ViasInoutColor;
			settings.colorSettings.ViasConnectColor = color.ViasConnectColor;
			settings.colorSettings.ViasFloatingColor = color.ViasFloatingColor;
			settings.colorSettings.ViasPowerColor = color.ViasPowerColor;
			settings.colorSettings.ViasGroundColor = color.ViasGroundColor;
			settings.colorSettings.WireInterconnectColor = color.WireInterconnectColor;
			settings.colorSettings.WirePowerColor = color.WirePowerColor;
			settings.colorSettings.WireGroundColor = color.WireGroundColor;
			settings.colorSettings.CellNotColor = color.CellNotColor;
			settings.colorSettings.CellBufferColor = color.CellBufferColor;
			settings.colorSettings.CellMuxColor = color.CellMuxColor;
			settings.colorSettings.CellLogicColor = color.CellLogicColor;
			settings.colorSettings.CellAdderColor = color.CellAdderColor;
			settings.colorSettings.CellBusSuppColor = color.CellBusSuppColor;
			settings.colorSettings.CellFlipFlopColor = color.CellFlipFlopColor;
			settings.colorSettings.CellLatchColor = color.CellLatchColor;
			settings.colorSettings.CellOtherColor = color.CellOtherColor;
			settings.colorSettings.UnitRegfileColor = color.UnitRegfileColor;
			settings.colorSettings.UnitMemoryColor = color.UnitMemoryColor;
			settings.colorSettings.UnitCustomColor = color.UnitCustomColor;
			settings.colorSettings.SelectionColor = color.SelectionColor;
			settings.colorSettings.ViasOverrideColor = color.ViasOverrideColor;
			settings.colorSettings.WireOverrideColor = color.WireOverrideColor;
			settings.colorSettings.CellOverrideColor = color.CellOverrideColor;
			settings.colorSettings.RegionOverrideColor = color.RegionOverrideColor;
			settings.colorSettings.ZeroColor = color.ZeroColor;
			settings.colorSettings.OneColor = color.OneColor;
			settings.colorSettings.HighZColor = color.HighZColor;

			// Save priority settings

			PrioritySettings priority = new PrioritySettings(entityBox);

			settings.prioritySettings.ViasPriority = priority.ViasPriority;
			settings.prioritySettings.WirePriority = priority.WirePriority;
			settings.prioritySettings.CellPriority = priority.CellPriority;
			settings.prioritySettings.BeaconPriority = priority.BeaconPriority;
			settings.prioritySettings.RegionPriority = priority.RegionPriority;
			settings.prioritySettings.AutoPriority = priority.AutoPriority;

			// Save opacity settings

			OpacitySettings opacity = new OpacitySettings(entityBox);

			settings.opacitySettings.ViasOpacity = opacity.ViasOpacity;
			settings.opacitySettings.WireOpacity = opacity.WireOpacity;
			settings.opacitySettings.CellOpacity = opacity.CellOpacity;

			// Save size settings

			SizeSettings size = new SizeSettings(entityBox);

			settings.sizeSettings.WireBaseSize = size.WireBaseSize;
			settings.sizeSettings.ViasBaseSize = size.ViasBaseSize;

			// Save shape settings

			ShapeSettings shape = new ShapeSettings(entityBox);

			settings.shapeSettings.ViasShape = shape.ViasShape;

			XmlSerializer ser = new XmlSerializer(typeof(SerializedSettings));

			using (FileStream fs = new FileStream(filename, FileMode.Create))
			{
				ser.Serialize(fs, settings);
			}
		}


		public class XmlColor
		{
			private Color color_ = Color.Black;

			public XmlColor() { }
			public XmlColor(Color c) { color_ = c; }


			public Color ToColor()
			{
				return color_;
			}

			public void FromColor(Color c)
			{
				color_ = c;
			}

			public static implicit operator Color(XmlColor x)
			{
				return x.ToColor();
			}

			public static implicit operator XmlColor(Color c)
			{
				return new XmlColor(c);
			}

			[XmlAttribute]
			public string Web
			{
				get { return ColorTranslator.ToHtml(color_); }
				set
				{
					try
					{
						if (Alpha == 0xFF) // preserve named color value if possible
							color_ = ColorTranslator.FromHtml(value);
						else
							color_ = Color.FromArgb(Alpha, ColorTranslator.FromHtml(value));
					}
					catch (Exception)
					{
						color_ = Color.Black;
					}
				}
			}

			[XmlAttribute]
			public byte Alpha
			{
				get { return color_.A; }
				set
				{
					if (value != color_.A) // avoid hammering named color if no alpha change
						color_ = Color.FromArgb(value, color_);
				}
			}

			public bool ShouldSerializeAlpha() { return Alpha < 0xFF; }
		}
	}
}
