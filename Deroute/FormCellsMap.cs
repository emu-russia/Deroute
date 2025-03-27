using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DerouteSharp
{
	public partial class FormCellsMap : Form
	{
		EntityBox entityBox;

		enum WorkspaceRowArrangement
		{
			Vertical,
			Horizontal,
		}

		public FormCellsMap(EntityBox parentEntityBox)
		{
			InitializeComponent();
			entityBox = parentEntityBox;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			WorkspaceRowArrangement arrange = radioButton1.Checked ? WorkspaceRowArrangement.Vertical : WorkspaceRowArrangement.Horizontal;
			textBox1.Text = BuildCellsMap(arrange);
		}

		string BuildCellsMap(WorkspaceRowArrangement arrange)
		{
			List<Entity> cells = entityBox.GetCells();
			List<RowEntry> rows = RecalcRows(cells, arrange);

			string text = "";

			foreach (var row in rows)
			{
				Console.WriteLine("row: {0}, x: {1}, y: {2}", row.index, row.planeX, row.planeY);
			}

			foreach (var row in rows)
			{
				foreach (var cell in cells)
				{
					if (cell.Label == "")
					{
						continue;
					}

					List<Entity> row_cells = new List<Entity>();

					if (arrange == WorkspaceRowArrangement.Vertical)
					{
						if (row.planeX >= cell.LambdaX && row.planeX <= (cell.LambdaX + cell.LambdaWidth))
						{
							row_cells.Add(cell);
						}
						row_cells = row_cells.OrderBy(o => o.LambdaY).ToList();
					}
					else if (arrange == WorkspaceRowArrangement.Horizontal)
					{
						if (row.planeY >= cell.LambdaY && row.planeY <= (cell.LambdaY + cell.LambdaHeight))
						{
							row_cells.Add(cell);
						}
						row_cells = row_cells.OrderBy(o => o.LambdaX).ToList();
					}

					foreach (var row_cell in row_cells)
					{
						text += row_cell.Label.Split(' ').FirstOrDefault() + " ";
					}
					text += "\n";
				}
			}

			return text;
		}

		List<RowEntry> RecalcRows(List<Entity> cells, WorkspaceRowArrangement arrange)
		{
			List<RowEntry> list = new List<RowEntry>();

			if (cells.Count == 0)
			{
				return list;
			}

			// Get patterns XY/WH pairs

			List<CoordSize_Pair> pairs = new List<CoordSize_Pair>();
			foreach (var cell in cells)
			{
				CoordSize_Pair pair = new CoordSize_Pair();
				pair.xy = arrange == WorkspaceRowArrangement.Vertical ? cell.LambdaX : cell.LambdaY;
				pair.wh = arrange == WorkspaceRowArrangement.Vertical ? cell.LambdaWidth : cell.LambdaHeight;
				pairs.Add(pair);
			}

			if (pairs.Count == 0)
			{
				return list;
			}

			// Sort minmax

			pairs = pairs.OrderBy(o => o.xy).ToList();

			// Remove overlapping

			CoordSize_Pair next = new CoordSize_Pair();
			next.xy = pairs[0].xy;
			next.wh = pairs[0].wh;

			for (int i = 1; i < pairs.Count; i++)
			{
				CoordSize_Pair pair = pairs[i];

				if (pair.xy >= next.xy &&
					pair.xy < (next.xy + next.wh))
				{
					pair.xy = 0;
				}
				else
				{
					next.xy = pairs[i].xy;
					next.wh = pairs[i].wh;
				}
			}

			// Add rows

			int rowIndex = 0;

			for (int i = 0; i < pairs.Count; i++)
			{
				CoordSize_Pair pair = pairs[i];

				if (pair.xy != 0)
				{
					RowEntry entry = new RowEntry();

					entry.index = rowIndex++;
					entry.planeX = arrange == WorkspaceRowArrangement.Vertical ? pair.xy : 0;
					entry.planeY = arrange == WorkspaceRowArrangement.Vertical ? 0 : pair.xy;

					list.Add(entry);
				}
			}

			return list;
		}

		struct RowEntry
		{
			public float index;
			public float planeX;
			public float planeY;
		};

		struct CoordSize_Pair
		{
			public float xy;        // X or Y coordinate
			public float wh;        // Width or Height (size)
		};
	}
}
