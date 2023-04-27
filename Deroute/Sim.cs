using System.Windows.Forms;
using System.Collections.Generic;

public class DerouteSim
{
	private EntityBox box = null;
	private Dictionary<Entity, List<LogicValue>> dump = new Dictionary<Entity, List<LogicValue>>();
	private long phase_counter = 0;

	public DerouteSim(EntityBox parent_box)
	{
		box = parent_box;
	}

	public long GetPhaseCounter()
	{
		return phase_counter;
	}

	public void Reset()
	{
		phase_counter = 0;

		foreach (Entity entity in box.GetEntities())
		{
			entity.Val = LogicValue.X;
			entity.PrevVal = LogicValue.X;
		}
		box.Invalidate();

		dump = new Dictionary<Entity, List<LogicValue>>();
	}

	public Dictionary<Entity, List<LogicValue>> GetDump()
	{
		return dump;
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

		SampleScopedValues();

		phase_counter++;

		box.Invalidate();
	}

	private void SampleScopedValues()
	{
		foreach (Entity entity in box.GetEntities())
		{
			if (entity.Scope)
			{
				if (!dump.ContainsKey(entity))
				{
					dump.Add(entity, new List<LogicValue>());
				}
				dump[entity].Add(entity.Val);
			}
		}

		// Fill in with the Z value those signals that are in the dump, but which have Scope turned off

		foreach (var entry in dump)
		{
			if (!entry.Key.Scope)
			{
				dump[entry.Key].Add(LogicValue.Z);
			}
		}
	}

}
