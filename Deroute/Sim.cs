

using System.Windows.Forms;

public class DerouteSim
{
	private EntityBox box = null;

	public DerouteSim(EntityBox parent_box)
	{
		box = parent_box;
	}

	public void Step()
	{
		foreach (Entity entity in box.GetEntities())
		{
			switch (entity.Val)
			{
				case LogicValue.X:
				case LogicValue.Z:
					entity.Val = LogicValue.Zero;
					break;
				case LogicValue.Zero:
					entity.Val = LogicValue.One;
					break;
				case LogicValue.One:
					entity.Val = LogicValue.Z;
					break;
			}
		}
		box.Invalidate();
	}

}
