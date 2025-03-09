using System.Drawing;
using System.Collections.Generic;

namespace System.Windows.Forms
{
	public delegate void EntityBoxEventHandler(object sender, EventArgs e);
	public delegate void EntityBoxEntityEventHandler(object sender, Entity entity, EventArgs e);
	public delegate void EntityBoxFrameDoneHandler(object sender, long ms_time, EventArgs e);
	public delegate void EntityBoxPlaneGeometryHandler(object sender, PointF orig, PointF size, EventArgs e);

	public enum EntitySelection
	{
		Vias,
		Wire,
		Cell,
		All,
	}

	public enum EntityMode
	{
		Selection = 0,

		ViasInput = 0x80,
		ViasOutput,
		ViasInout,
		ViasConnect,
		ViasFloating,
		ViasPower,
		ViasGround,
		WireInterconnect,
		WirePower,
		WireGround,
		CellNot,
		CellBuffer,
		CellMux,
		CellLogic,
		CellAdder,
		CellBusSupp,
		CellFlipFlop,
		CellLatch,
		CellOther,
		UnitRegfile,
		UnitMemory,
		UnitCustom,
		Beacon,
	}

	public partial class EntityBox : Control
	{
		public Entity root;
		private Entity insertionNode;            // Destination for add/paste operations (root by default)

		private Image beaconImage = null;
		private float _lambda;
		private int _zoom;
		private float _ScrollX;
		private float _ScrollY;
		private float SavedScrollX;
		private float SavedScrollY;
		private int SavedMouseX;
		private int SavedMouseY;
		private int LastMouseX;
		private int LastMouseY;
		private int DragStartMouseX;
		private int DragStartMouseY;
		private int SelectStartMouseX;
		private int SelectStartMouseY;
		private bool ScrollingBegin = false;
		private bool DrawingBegin = false;
		private bool DraggingBegin = false;
		private bool SelectionBegin = false;
		private EntityMode drawMode = EntityMode.Selection;
		private bool hideImage;
		private bool hideVias;
		private bool hideWires;
		private bool hideCells;
		private bool hideGrid;
		private float gridSize;
		private bool snapToGrid;
		private bool hideLambdaMetrics;
		private bool hideRegions;
		private PropertyGrid entityGrid;
		private List<Entity> selected;
		private float draggingDist;
		private Color selectionBoxColor;
		private BufferedGraphics gfx = null;
		private BufferedGraphicsContext context;
		private bool selectEntitiesAfterAdd;
		private bool wireSelectionAutoTraverse;
		private long UnserializeLastStamp = 0;
		private Point LastRMB = new Point(0, 0);
		private bool DrawInProgress;
		private List<Entity> copied = new List<Entity>();
		private PointF TopLeftCopied;
		private bool DrawStats = false;
		private bool selectCellWithPorts = true;

		public event EntityBoxEventHandler OnScrollChanged = null;
		public event EntityBoxEventHandler OnZoomChanged = null;
		public event EntityBoxEventHandler OnEntityCountChanged = null;
		public event EntityBoxEntityEventHandler OnEntityLabelEdit = null;
		public event EntityBoxEntityEventHandler OnEntitySelect = null;
		public event EntityBoxEntityEventHandler OnEntityAdd = null;
		public event EntityBoxEntityEventHandler OnEntityRemove = null;
		public event EntityBoxEntityEventHandler OnEntityScroll = null;
		public event EntityBoxEntityEventHandler OnDestinationNodeChanged = null;
		public event EntityBoxFrameDoneHandler OnFrameDone = null;
		public event EntityBoxEntityEventHandler OnModuleChanged = null;
		public event EntityBoxPlaneGeometryHandler OnSelectionBox = null;
		public event EntityBoxEventHandler OnImageLoad = null;

		public EntityBox()
		{
			BackColor = SystemColors.WindowFrame;
			ForeColor = Color.Snow;

			root = new Entity();
			root.Type = EntityType.Root;

			SetDestinationNode(root);

			Lambda = 5.0F;
			Zoom = 100;
			_imageOpacity = 100;
   			gridSize = 5;
			hideImage = false;
			hideVias = false;
			hideWires = false;
			hideCells = false;
			selectionBoxColor = Color.Red;
			entityGrid = null;
			SelectEntitiesAfterAdd = true;

			DefaultEntityAppearance();

			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		}

		/// <summary>
		/// Get a list of all entities. The hierarchy is destroyed (you get a regular linear list).
		/// </summary>
		/// <returns>A list of all entities.</returns>
		public List<Entity> GetEntities()
		{
			List<Entity> list = new List<Entity>();

			foreach (var entity in root.Children)
			{
				GetEntitiesRecursive(entity, list);
			}

			return list;
		}

		public void SetDestinationNode(Entity node)
		{
			insertionNode = node;
			OnDestinationNodeChanged?.Invoke(this, node, EventArgs.Empty);
		}

		public Entity GetDestinationNode ()
		{
			return insertionNode;
		}

		public void GetEntitiesRecursive(Entity parent, List<Entity> list)
		{
			list.Add(parent);

			foreach (var entity in parent.Children)
			{
				GetEntitiesRecursive(entity, list);
			}
		}


		public void EnsureVisible ( Entity entity )
		{
			Point screen = LambdaToScreen(entity.LambdaX, entity.LambdaY);

			if ( screen.X < WireBaseSize * 2 || screen.Y < WireBaseSize * 2 ||
				screen.X >= Width - WireBaseSize * 2 || screen.Y >= Height - WireBaseSize * 2)
			{
				PointF center = ScreenToLambda(Width, Height);
				float zf = (float)Zoom / 100F;

				ScrollX = -entity.LambdaX + (Width/2/ (zf*Lambda));
				ScrollY = -entity.LambdaY + (Height/2 / (zf*Lambda));

				Invalidate();
			}
		}

		public int GetViasCount()
		{
			List<Entity> _entities = GetEntities();

			int Count = 0;
			foreach (Entity entity in _entities)
			{
				if (entity.IsVias())
					Count++;
			}
			return Count;
		}

		public int GetWireCount()
		{
			List<Entity> _entities = GetEntities();

			int Count = 0;
			foreach (Entity entity in _entities)
			{
				if (entity.IsWire())
					Count++;
			}
			return Count;
		}

		public int GetCellCount()
		{
			List<Entity> _entities = GetEntities();

			int Count = 0;
			foreach (Entity entity in _entities)
			{
				if (entity.IsCell())
					Count++;
			}
			return Count;
		}

		public List<Entity> GetCells()
		{
			List<Entity> _entities = GetEntities();

			List<Entity> cells = new List<Entity>();
			foreach (Entity entity in _entities)
			{
				if (entity.IsCell())
					cells.Add(entity);
			}
			return cells;
		}

		public int GetBeaconCount ()
		{
			List<Entity> _entities = GetEntities();

			int NumBeacons = 0;
			foreach (Entity entity in _entities)
			{
				if (entity.Type == EntityType.Beacon)
					NumBeacons++;
			}
			return NumBeacons;
		}

		public List<Entity> GetBeacons ()
		{
			List<Entity> _entities = GetEntities();

			List<Entity> beacons = new List<Entity>();
			foreach (Entity entity in _entities)
			{
				if (entity.Type == EntityType.Beacon)
					beacons.Add(entity);
			}
			return beacons;
		}

		public void ScrollToBeacon (Entity beacon)
		{
			_ScrollX = 0;
			_ScrollY = 0;

			Point screen = LambdaToScreen(beacon.LambdaX, beacon.LambdaY);

			screen.X -= Width / 2;
			screen.Y -= Height / 2;

			PointF lambda = ScreenToLambda(screen.X, screen.Y);

			_ScrollX = -lambda.X;
			_ScrollY = -lambda.Y;

			Invalidate();

			if (OnScrollChanged != null)
				OnScrollChanged(this, EventArgs.Empty);
		}

		public void LabelEdited (Entity entity)
		{
			if (OnEntityLabelEdit != null)
				OnEntityLabelEdit(this, entity, EventArgs.Empty);
		}

		public void ModuleEdited(Entity entity)
		{
			if (OnModuleChanged != null)
				OnModuleChanged(this, entity, EventArgs.Empty);
		}

		//
		// Lambda Transformation
		//

		public void LambdaScale(float scale)
		{
			//
			// Grab selected entities
			//

			List<Entity> sourceList = GetSelected();

			if (sourceList.Count == 0)
				return;

			//
			// Scale
			//

			foreach (Entity entity in sourceList )
			{
				if (entity.PathPoints != null && entity.PathPoints.Count != 0)
				{
					for (int i = 0; i < entity.PathPoints.Count; i++ )
					{
						PointF scaled = new PointF();

						scaled.X = entity.PathPoints[i].X * scale;
						scaled.Y = entity.PathPoints[i].Y * scale;

						entity.PathPoints[i] = scaled;
					}
				}

				entity.LambdaX *= scale;
				entity.LambdaY *= scale;
				entity.LambdaEndX *= scale;
				entity.LambdaEndY *= scale;
				entity.LambdaWidth *= scale;
				entity.LambdaHeight *= scale;
			}

			Invalidate();
		}



	}       // EntityBox


}       // namespace
