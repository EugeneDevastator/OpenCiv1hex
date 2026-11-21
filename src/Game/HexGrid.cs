using OpenCivOne.Graphics;

namespace OpenCivOne
{
	/// <summary>
	/// Geometry helpers for the experimental hex map.
	///
	/// The world is displayed as columns of 16x16 cells, where every odd column is
	/// shifted down by half a cell (8 pixels). This is the classic "odd-q" hex
	/// layout. The map is still stored as a rectangular (column, row) array, so a
	/// cell has six neighbours:
	///
	///   - north and south (same column),
	///   - east and west (always the neighbouring column, at the same row),
	///   - the two diagonals that point up on even columns, or down on odd columns.
	///
	/// In other words, of the eight nominal compass directions used by the original
	/// square-grid game, exactly six are neighbours of any given cell; the two
	/// diagonals pointing towards the "wrong" half of the column are not.
	///
	/// Direction indices are the same as <see cref="OpenCivOneGame.MoveDirections"/>:
	/// 1 = north, 2 = north-east, 3 = east, 4 = south-east, 5 = south, 6 = south-west,
	/// 7 = west, 8 = north-west.
	/// </summary>
	public static class HexGrid
	{
		/// <summary>Distance in pixels between two map columns.</summary>
		public const int ColumnWidth = 16;

		/// <summary>Distance in pixels between two cells in the same column.</summary>
		public const int RowHeight = 16;

		/// <summary>Vertical offset of every odd column, in pixels.</summary>
		public const int ColumnStagger = 8;

		/// <summary>
		/// The eight nominal direction offsets, indexed like MoveDirections.
		/// Index 0 is the zero offset used by loops that include the cell itself.
		/// </summary>
		public static readonly GPoint[] DirectionOffsets =
		{
			new GPoint(0, 0),
			new GPoint(0, -1), new GPoint(1, -1), new GPoint(1, 0), new GPoint(1, 1),
			new GPoint(0, 1), new GPoint(-1, 1), new GPoint(-1, 0), new GPoint(-1, -1)
		};

		/// <summary>True if the given column is shifted down by half a cell.</summary>
		public static bool IsOddColumn(int column)
		{
			// Bitwise operations keep the parity correct for negative columns too.
			return (column & 1) != 0;
		}

		/// <summary>The vertical pixel offset of a column.</summary>
		public static int ColumnPixelOffset(int column)
		{
			return IsOddColumn(column) ? ColumnStagger : 0;
		}

		/// <summary>
		/// True if moving by the given offset is a single hex step from a cell in
		/// the given column.
		/// </summary>
		public static bool IsValidOffset(int column, int offsetX, int offsetY)
		{
			if (offsetX == 0)
			{
				return offsetY == -1 || offsetY == 1;
			}

			if (offsetY == 0)
			{
				return offsetX == -1 || offsetX == 1;
			}

			if (Math.Abs(offsetX) != 1 || Math.Abs(offsetY) != 1)
			{
				return false;
			}

			// Upwards diagonals neighbour an even column, downwards diagonals an odd one.
			return (offsetY < 0) == !IsOddColumn(column);
		}

		/// <summary>
		/// True if the given direction index is a hex step from a cell in the given
		/// column. Index 0 is never a step, as it is the cell itself.
		/// </summary>
		public static bool IsValidDirection(int column, int directionIndex)
		{
			if (directionIndex < 1 || directionIndex > 8)
			{
				return false;
			}

			GPoint offset = DirectionOffsets[directionIndex];

			return IsValidOffset(column, offset.X, offset.Y);
		}

		/// <summary>
		/// The six direction indices that are neighbours of a cell in the given
		/// column.
		/// </summary>
		public static IEnumerable<int> GetDirectionIndices(int column)
		{
			for (int i = 1; i <= 8; i++)
			{
				if (IsValidDirection(column, i))
				{
					yield return i;
				}
			}
		}

		/// <summary>Converts a map column and row to cube coordinates (the X axis).</summary>
		private static int ToCubeQ(int column)
		{
			return column;
		}

		/// <summary>Converts a map column and row to cube coordinates (the Z axis).</summary>
		private static int ToCubeR(int column, int row)
		{
			// odd-q offset to axial: every two columns shift the rows by one.
			return row - ((column - (column & 1)) / 2);
		}

		/// <summary>
		/// Hex distance in cells between two positions, ignoring the horizontal map
		/// wrap.
		/// </summary>
		public static int Distance(int x1, int y1, int x2, int y2)
		{
			int dq = ToCubeQ(x2) - ToCubeQ(x1);
			int dr = ToCubeR(x2, y2) - ToCubeR(x1, y1);
			int ds = -dq - dr;

			return Math.Max(Math.Abs(dq), Math.Max(Math.Abs(dr), Math.Abs(ds)));
		}

		/// <summary>
		/// Hex distance in cells between two positions on a map that wraps around
		/// the given width.
		/// </summary>
		public static int WrappedDistance(int x1, int y1, int x2, int y2, int mapWidth)
		{
			int dx = x2 - x1;

			int distance = Distance(x1, y1, x1 + dx, y2);
			distance = Math.Min(distance, Distance(x1, y1, x1 + dx - mapWidth, y2));
			distance = Math.Min(distance, Distance(x1, y1, x1 + dx + mapWidth, y2));

			return distance;
		}

		/// <summary>
		/// The direction index (1..8) that steps from one cell to an adjacent cell,
		/// or -1 when the two cells are not neighbours. The offset is expected to be
		/// normalised for the horizontal map wrap.
		/// </summary>
		public static int GetDirectionIndex(int fromColumn, int offsetX, int offsetY)
		{
			for (int i = 1; i <= 8; i++)
			{
				GPoint offset = DirectionOffsets[i];

				if (offset.X == offsetX && offset.Y == offsetY && IsValidOffset(fromColumn, offsetX, offsetY))
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>The direction index opposite to the given one.</summary>
		public static int GetOppositeDirectionIndex(int directionIndex)
		{
			return ((directionIndex - 1 + 4) % 8) + 1;
		}

		/// <summary>How many hex steps away a city can work a tile.</summary>
		public const int CityRadius = 2;

		/// <summary>
		/// Number of city workable-tile slots: the 6 ring-1 tiles, the 12 ring-2
		/// tiles and the city tile itself.
		/// </summary>
		public const int CityOffsetCount = 19;

		/// <summary>The slot of the city's own tile in the city offset list.</summary>
		public const int CityCenterOffsetIndex = CityOffsetCount - 1;

		private static readonly IReadOnlyList<GPoint>[] CityOffsetsByParity =
		{
			BuildCityOffsets(0),
			BuildCityOffsets(1)
		};

		/// <summary>
		/// The 19 city-tile offsets for a city in the given column. The order is
		/// stable for a city: ring 1 in direction order, ring 2 in scan order, then
		/// the city tile itself. The offsets differ between even and odd columns
		/// because of the column stagger, so they must be resolved for each city.
		/// </summary>
		public static IReadOnlyList<GPoint> GetCityOffsets(int column)
		{
			return CityOffsetsByParity[column & 1];
		}

		private static GPoint[] BuildCityOffsets(int parityColumn)
		{
			List<GPoint> offsets = new List<GPoint>(CityOffsetCount);

			foreach (int directionIndex in GetDirectionIndices(parityColumn))
			{
				offsets.Add(DirectionOffsets[directionIndex]);
			}

			for (int dx = -CityRadius; dx <= CityRadius; dx++)
			{
				for (int dy = -CityRadius; dy <= CityRadius; dy++)
				{
					if (Distance(parityColumn, 0, parityColumn + dx, dy) == CityRadius)
					{
						offsets.Add(new GPoint(dx, dy));
					}
				}
			}

			offsets.Add(new GPoint(0, 0)); // the city tile itself

			return offsets.ToArray();
		}
	}
}
