using OpenCivOne.Graphics;

namespace OpenCivOne.Tests
{
	/// <summary>
	/// The map uses a column-staggered ("odd-q") hex layout: every odd column is
	/// shifted down by half a cell.
	/// </summary>
	public class HexGridTests
	{
		[Fact]
		public void EvenColumnsHaveTheTwoUpwardDiagonals()
		{
			Assert.Equal(new[] { 1, 2, 3, 5, 7, 8 }, HexGrid.GetDirectionIndices(0));
			Assert.Equal(new[] { 1, 2, 3, 5, 7, 8 }, HexGrid.GetDirectionIndices(78));
		}

		[Fact]
		public void OddColumnsHaveTheTwoDownwardDiagonals()
		{
			Assert.Equal(new[] { 1, 3, 4, 5, 6, 7 }, HexGrid.GetDirectionIndices(1));
			Assert.Equal(new[] { 1, 3, 4, 5, 6, 7 }, HexGrid.GetDirectionIndices(79));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(39)]
		public void EveryNeighbourIsOneStepAway(int column)
		{
			foreach (int directionIndex in HexGrid.GetDirectionIndices(column))
			{
				GPoint offset = HexGrid.DirectionOffsets[directionIndex];

				Assert.Equal(1, HexGrid.Distance(column, 20, column + offset.X, 20 + offset.Y));
			}
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		public void NonNeighbouringDiagonalIsNotAStep(int column)
		{
			// Upward diagonals are neighbours of even columns, downward ones of odd columns.
			int invalidDirection = column % 2 == 0 ? 4 : 2;

			Assert.False(HexGrid.IsValidDirection(column, invalidDirection));
			Assert.True(HexGrid.IsValidDirection(column, invalidDirection == 4 ? 2 : 4));
		}

		[Fact]
		public void DistanceIsSymmetric()
		{
			Assert.Equal(3, HexGrid.Distance(10, 10, 13, 11));
			Assert.Equal(3, HexGrid.Distance(13, 11, 10, 10));
		}

		[Fact]
		public void DistanceWrapsAroundTheMap()
		{
			// Columns 79 and 0 are adjacent, and so are the diagonal pair across the seam.
			Assert.Equal(1, HexGrid.WrappedDistance(0, 10, 79, 10, 80));
			Assert.Equal(1, HexGrid.WrappedDistance(79, 10, 0, 11, 80));
			Assert.Equal(1, HexGrid.WrappedDistance(0, 10, 79, 9, 80));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		public void CityWorksTwoHexRingsAndTheCityTile(int column)
		{
			IReadOnlyList<GPoint> offsets = HexGrid.GetCityOffsets(column);

			Assert.Equal(HexGrid.CityOffsetCount, offsets.Count);
			Assert.Equal(new GPoint(0, 0), offsets[HexGrid.CityCenterOffsetIndex]);
			Assert.Equal(offsets.Count, offsets.Distinct().Count());

			for (int i = 0; i < 6; i++)
			{
				Assert.Equal(1, HexGrid.Distance(column, 0, column + offsets[i].X, offsets[i].Y));
			}

			for (int i = 6; i < HexGrid.CityCenterOffsetIndex; i++)
			{
				Assert.Equal(2, HexGrid.Distance(column, 0, column + offsets[i].X, offsets[i].Y));
			}

			Assert.All(offsets, offset => Assert.True(Math.Abs(offset.X) <= 2 && Math.Abs(offset.Y) <= 2));
		}

		[Fact]
		public void CityRingFollowsTheColumnStagger()
		{
			// The inner ring has the upward diagonals on even columns and the downward
			// diagonals on odd columns.
			Assert.Contains(new GPoint(1, -1), HexGrid.GetCityOffsets(2).Take(6));
			Assert.DoesNotContain(new GPoint(1, -1), HexGrid.GetCityOffsets(1).Take(6));
			Assert.Contains(new GPoint(1, 1), HexGrid.GetCityOffsets(1).Take(6));
			Assert.DoesNotContain(new GPoint(1, 1), HexGrid.GetCityOffsets(2).Take(6));
		}

		[Fact]
		public void OddColumnsAreShiftedDownHalfACell()
		{
			Assert.Equal(0, HexGrid.ColumnPixelOffset(0));
			Assert.Equal(8, HexGrid.ColumnPixelOffset(1));
			Assert.Equal(0, HexGrid.ColumnPixelOffset(78));
			Assert.Equal(8, HexGrid.ColumnPixelOffset(79));
		}
	}
}
