using System;
using System.Collections.Generic;

namespace Saga.Board
{
	/// <summary>Translates the game's mission data into the rules engine's board model.</summary>
	public static class SagaBoardBridge
	{
		/// <summary>One tile as the engine needs to see it.</summary>
		public struct TileInput
		{
			public string Expansion;
			public string TileId;
			public string Side;
			public int X;
			public int Y;
			public int Rotation;
			public string SectionGuid;
		}

		/// <summary>One door as the engine needs to see it.</summary>
		public struct DoorInput
		{
			public int X;
			public int Y;
			public int Rotation;
			public bool Open;
		}

		/// <summary>The board for the currently active sections, or null before a mission has been set up.</summary>
		public static BoardModel Current { get; private set; }

		/// <summary>Diagnostics from the last build, for the debug overlay.</summary>
		public static BoardBuilder.BuildResult LastBuild { get; private set; }

		/// <summary>Rebuild the board from the mission's tiles, honouring which sections are currently active.</summary>
		public static BoardModel Rebuild(
			IEnumerable<TileInput> tiles,
			IEnumerable<DoorInput> doors,
			Func<string, string, (int w, int h)?> dimensions,
			TerrainLibrary terrain,
			Func<string, bool> isSectionActive )
		{
			var placements = new List<TilePlacement>();
			foreach ( var t in tiles )
			{
				placements.Add( new TilePlacement
				{
					Expansion = t.Expansion,
					TileId = t.TileId,
					Side = t.Side,
					X = t.X,
					Y = t.Y,
					Rotation = t.Rotation,
					SectionGuid = t.SectionGuid,
				} );
			}

			var doorList = new List<DoorPlacement>();
			foreach ( var d in doors )
				doorList.Add( new DoorPlacement
				{ X = d.X, Y = d.Y, Rotation = d.Rotation, Open = d.Open } );

			LastBuild = BoardBuilder.Build( placements.ToArray(), dimensions,
				doorList.ToArray(), terrain, isSectionActive );
			Current = LastBuild.Board;
			return Current;
		}

		/// <summary>Where a figure standing on this square sits in world space.</summary>
		/// <remarks>
		/// TileRenderer.LoadTile places a tile at
		/// (entityPosition.X / 10, 0, -entityPosition.Y / 10), and a board square
		/// is entityPosition / 10, so one Unity unit is one board square and
		/// screen Y runs along -Z. The half-square offset centres the figure in
		/// the space rather than on its north-west corner.
		/// </remarks>
		public static (float x, float y, float z) SquareToWorld( Sq square, float height = 0f )
			=> (square.C + 0.5f, height, -(square.R + 0.5f));

		/// <summary>The square a world position falls in. Inverse of SquareToWorld.</summary>
		/// <summary>Squares a base spans along each axis, for drawing it at size.</summary>
		public static (int w, int h) FootprintSpan( Footprint footprint, Facing facing = Facing.NorthSouth )
		{
			switch ( footprint )
			{
				case Footprint.Medium1x2: return facing == Facing.NorthSouth ? (1, 2) : (2, 1);
				case Footprint.Large2x2: return (2, 2);
				case Footprint.Huge2x3: return facing == Facing.NorthSouth ? (2, 3) : (3, 2);
				default: return (1, 1);
			}
		}

		/// <summary>
		/// World position of the CENTRE of a figure's base, which is where its
		/// token is drawn. A 1x1 is the square's centre; a 2x2 anchored at
		/// (c,r) is the corner shared by its four squares.
		/// </summary>
		public static (float x, float y, float z) FootprintCenter( Sq anchor, Footprint footprint,
			float height = 0f, Facing facing = Facing.NorthSouth )
		{
			var (w, h) = FootprintSpan( footprint, facing );
			return (anchor.C + w / 2f, height, -(anchor.R + h / 2f));
		}

		public static Sq WorldToSquare( float x, float z )
			=> new Sq( (int)Math.Floor( x ), (int)Math.Floor( -z ) );

		/// <summary>
		/// The offset a door prefab adds to its stored position, by rotation.
		/// </summary>
		/// <remarks>
		/// A door's stored position is not where it renders: the prefab shifts
		/// it diagonally by one space to the lattice point -- the shared corner
		/// of four squares -- that the door actually sits on. BoardBuilder
		/// applies the same shift itself, so anything reading a door back off
		/// the board has to take it away again first or the door lands one
		/// space out along both axes.
		/// </remarks>
		public static (int x, int y) DoorOffset( int rotation )
		{
			int r = (rotation % 360 + 360) % 360;
			return (r == 90 || r == 180 ? -1 : 1,
					r == 180 || r == 270 ? -1 : 1);
		}

		/// <summary>Recover a door's stored position from the lattice point it renders on.</summary>
		public static (int x, int y) DoorLatticeToPlacement( int latticeC, int latticeR, int rotation )
		{
			var (xmod, ymod) = DoorOffset( rotation );
			return (latticeC - xmod, latticeR - ymod);
		}

		/// <summary>The lattice point a door renders on, from its stored position.</summary>
		public static (int c, int r) DoorPlacementToLattice( int x, int y, int rotation )
		{
			var (xmod, ymod) = DoorOffset( rotation );
			return (x + xmod, y + ymod);
		}

		/// <summary>A figure's path as world positions, for animating the move.</summary>
		public static List<(float x, float y, float z)> PathToWorld(
			IEnumerable<Sq> path, float height = 0f )
		{
			var points = new List<(float, float, float)>();
			if ( path == null ) return points;
			foreach ( var sq in path ) points.Add( SquareToWorld( sq, height ) );
			return points;
		}

		/// <summary>Forget the board.</summary>
		public static void Clear()
		{
			Current = null;
			LastBuild = null;
		}
	}
}
