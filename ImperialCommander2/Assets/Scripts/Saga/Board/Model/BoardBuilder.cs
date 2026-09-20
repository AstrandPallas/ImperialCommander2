using System;
using System.Collections.Generic;

namespace Saga.Board
{
	/// <summary>One map tile placed in a mission.</summary>
	public struct TilePlacement
	{
		public string Expansion;
		public string TileId;
		public string Side;
		/// <summary>Square coordinates: entityPosition / 10.</summary>
		public int X, Y;
		/// <summary>0, 90, 180 or 270, clockwise about the tile's top-left.</summary>
		public int Rotation;
		public string SectionGuid;
	}

	/// <summary>A door entity placed in a mission.</summary>
	public struct DoorPlacement
	{
		/// <summary>Square coordinates: entityPosition / 10, BEFORE the prefab's diagonal offset is applied.</summary>
		public int X, Y;
		public int Rotation;
		public bool Open;
		public string SectionGuid;
	}

	/// <summary>Builds a BoardModel from mission geometry.</summary>
	public static class BoardBuilder
	{
		/// <summary>Squares covered by a tile, per the validated rotation table.</summary>
		public static IEnumerable<Sq> TileSquares( int x, int y, int w, int h, int rotation )
		{
			int rot = ((rotation % 360) + 360) % 360;
			int c0, c1, r0, r1;
			switch ( rot )
			{
				case 0: c0 = x; c1 = x + w; r0 = y; r1 = y + h; break;
				case 90: c0 = x - h; c1 = x; r0 = y; r1 = y + w; break;
				case 180: c0 = x - w; c1 = x; r0 = y - h; r1 = y; break;
				case 270: c0 = x; c1 = x + h; r0 = y - w; r1 = y; break;
				default:
					throw new ArgumentException( $"non-orthogonal tile rotation {rotation}" );
			}
			for ( int c = c0; c < c1; c++ )
				for ( int r = r0; r < r1; r++ )
					yield return new Sq( c, r );
		}

		/// <summary>The lattice point a door actually sits on.</summary>
		public static (int cx, int cy) DoorLattice( int x, int y, int rotation )
		{
			int rot = ((rotation % 360) + 360) % 360;
			switch ( rot )
			{
				case 0: return (x + 1, y + 1);
				case 90: return (x - 1, y + 1);
				case 180: return (x - 1, y - 1);
				case 270: return (x + 1, y - 1);
				default:
					throw new ArgumentException( $"non-orthogonal door rotation {rotation}" );
			}
		}

		/// <summary>The two edges a door covers.</summary>
		public static IEnumerable<(Sq owner, EdgeDir dir)> DoorEdges( int x, int y, int rotation )
		{
			var (cx, cy) = DoorLattice( x, y, rotation );
			int rot = ((rotation % 360) + 360) % 360;
			if ( rot == 0 || rot == 180 )
			{
				yield return (new Sq( cx - 1, cy ), EdgeDir.N);
				yield return (new Sq( cx, cy ), EdgeDir.N);
			}
			else
			{
				yield return (new Sq( cx, cy - 1 ), EdgeDir.W);
				yield return (new Sq( cx, cy ), EdgeDir.W);
			}
		}

		public sealed class BuildResult
		{
			public BoardModel Board;
			/// <summary>Squares claimed by more than one tile.</summary>
			public List<Sq> Overlaps = new List<Sq>();
			public Dictionary<Sq, string> SectionOf = new Dictionary<Sq, string>();

			/// <summary>Which tile face each board square came from, and where it sits inside that face's UNROTATED grid.</summary>
			public Dictionary<Sq, TileRef> TileOf = new Dictionary<Sq, TileRef>();

			public List<string> Warnings = new List<string>();
		}

		/// <summary>A board square's origin: which tile face, and where on it.</summary>
		public struct TileRef
		{
			public string Expansion;
			public string TileId;
			public string Side;
			/// <summary>Column in the tile's unrotated grid.</summary>
			public int LocalC;
			/// <summary>Row in the tile's unrotated grid.</summary>
			public int LocalR;

			public string Face => Expansion + "_" + TileId + Side;
			public override string ToString() => Face + " (" + LocalC + "," + LocalR + ")";
		}

		/// <summary>Build the board.</summary>
		public static BuildResult Build(
			IEnumerable<TilePlacement> tiles,
			Func<string, string, (int w, int h)?> dimensions,
			IEnumerable<DoorPlacement> doors = null,
			TerrainLibrary terrain = null,
			Func<string, bool> isSectionActive = null )
		{
			var result = new BuildResult { Board = new BoardModel() };
			var board = result.Board;
			var claimed = new HashSet<Sq>();

			foreach ( var t in tiles )
			{
				if ( isSectionActive != null && !string.IsNullOrEmpty( t.SectionGuid )
					 && !isSectionActive( t.SectionGuid ) )
					continue;

				var dims = dimensions( t.Expansion, t.TileId );
				if ( dims == null )
				{
					result.Warnings.Add( $"no dimensions for {t.Expansion}_{t.TileId}{t.Side}" );
					continue;
				}
				var (w, h) = dims.Value;
				var tileTerrain = terrain?.For( t.Expansion, t.TileId, t.Side );

				int index = 0;
				foreach ( var sq in TileSquares( t.X, t.Y, w, h, t.Rotation ) )
				{
					var local = ToLocal( sq, t.X, t.Y, w, h, t.Rotation );

					var flags = SquareFlags.Normal;
					if ( tileTerrain != null )
						flags = tileTerrain.FlagsAt( local.c, local.r );

					if ( (flags & SquareFlags.Void) != 0 ) continue;

					if ( !claimed.Add( sq ) )
					{
						result.Overlaps.Add( sq );
						if ( result.SectionOf.TryGetValue( sq, out var prevSection )
							 && prevSection != t.SectionGuid )
							result.Warnings.Add( $"{sq}: sections {prevSection} and "
								+ $"{t.SectionGuid} both claim this square" );
						else
							result.Warnings.Add( $"{sq}: overlapping tiles within one section" );
					}

					// Union semantics on overlap: the stronger claim wins, so a
					// tile that prints a wall is never softened by its neighbour.
					if ( board.Exists( sq ) )
						flags = Stronger( board.Flags( sq ), flags );
					board.SetSquare( sq, flags );
					if ( !string.IsNullOrEmpty( t.SectionGuid ) ) result.SectionOf[sq] = t.SectionGuid;
					result.TileOf[sq] = new TileRef
					{
						Expansion = t.Expansion,
						TileId = t.TileId,
						Side = t.Side,
						LocalC = local.c,
						LocalR = local.r,
					};
					index++;
				}

				// Authored edges, rotated into board space. Applied after the
				// squares so an edge never lands on a square this tile skipped.
				if ( tileTerrain?.Edges != null )
				{
					foreach ( var e in tileTerrain.Edges )
					{
						var boardSq = ToBoard( e.C, e.R, t.X, t.Y, w, h, t.Rotation );
						var (owner, dir) = Canonical( boardSq, RotateDir( e.Dir, t.Rotation ) );
						// Union semantics: never soften an edge a neighbour already
						// claimed as more restrictive.
						var existing = board.Edge( owner, dir );
						board.SetEdge( owner, dir, StrongerEdge( existing, e.Type ) );
					}
				}
			}

			if ( doors != null )
			{
				foreach ( var d in doors )
				{
					foreach ( var (owner, dir) in DoorEdges( d.X, d.Y, d.Rotation ) )
						board.SetEdge( owner, dir, d.Open ? EdgeType.DoorOpen : EdgeType.DoorClosed );
				}
			}
			return result;
		}

		/// <summary>Map a tile-local square to its board square, for a placement.</summary>
		public static Sq ToBoard( int lc, int lr, int x, int y, int w, int h, int rotation )
		{
			switch ( ((rotation % 360) + 360) % 360 )
			{
				case 0: return new Sq( x + lc, y + lr );
				case 90: return new Sq( x - 1 - lr, y + lc );
				case 180: return new Sq( x - 1 - lc, y - 1 - lr );
				default: return new Sq( x + lr, y - 1 - lc );
			}
		}

		/// <summary>Rotate a tile-local edge direction into board space.</summary>
		public static string RotateDir( string dir, int rotation )
		{
			var order = new[] { "N", "E", "S", "W" };
			int i = Array.IndexOf( order, (dir ?? "N").ToUpperInvariant() );
			if ( i < 0 ) return "N";
			int steps = (((rotation % 360) + 360) % 360) / 90;
			return order[(i + steps) % 4];
		}

		/// <summary>Normalise any direction to the canonical (owner, N|W) form.</summary>
		public static (Sq owner, EdgeDir dir) Canonical( Sq s, string dir )
		{
			switch ( (dir ?? "N").ToUpperInvariant() )
			{
				case "N": return (s, EdgeDir.N);
				case "W": return (s, EdgeDir.W);
				case "S": return (s.South, EdgeDir.N);
				default: return (s.East, EdgeDir.W);
			}
		}

		/// <summary>Map a board square back into the tile's unrotated local grid, inverting the rotation table.</summary>
		public static (int c, int r) ToLocal( Sq sq, int x, int y, int w, int h, int rotation )
		{
			int rot = ((rotation % 360) + 360) % 360;
			switch ( rot )
			{
				case 0: return (sq.C - x, sq.R - y);
				case 90: return (sq.R - y, (x - 1) - sq.C);
				case 180: return ((x - 1) - sq.C, (y - 1) - sq.R);
				case 270: return ((y - 1) - sq.R, sq.C - x);
				default: return (0, 0);
			}
		}

		/// <summary>Edge strength order, used when two tiles both claim an edge.</summary>
		private static EdgeType StrongerEdge( EdgeType a, EdgeType b )
		{
			int Rank( EdgeType t )
			{
				switch ( t )
				{
					case EdgeType.Wall: return 5;
					case EdgeType.DoorClosed: return 4;
					case EdgeType.Blocking: return 3;
					case EdgeType.Impassable: return 2;
					case EdgeType.DoorOpen: return 1;
					case EdgeType.Doorway: return 1;
					default: return 0;
				}
			}
			return Rank( a ) >= Rank( b ) ? a : b;
		}

		private static SquareFlags Stronger( SquareFlags a, SquareFlags b )
		{
			// Ordered by how much they restrict play.
			foreach ( var f in new[] { SquareFlags.Blocking, SquareFlags.Impassable,
									   SquareFlags.Pit, SquareFlags.Difficult } )
			{
				if ( (a & f) != 0 || (b & f) != 0 ) return f;
			}
			if ( (a & SquareFlags.Void) != 0 && (b & SquareFlags.Void) != 0 ) return SquareFlags.Void;
			return SquareFlags.Normal;
		}
	}

	/// <summary>An authored edge in a tile's unrotated local frame.</summary>
	public struct TileEdge
	{
		public int C, R;
		/// <summary>"N", "E", "S" or "W" in the tile's own orientation.</summary>
		public string Dir;
		public EdgeType Type;
	}

	/// <summary>Per-tile authored terrain, keyed by expansion, id and side.</summary>
	public sealed class TileTerrain
	{
		public string Expansion, TileId, Side;
		public int Width, Height;
		/// <summary>Row strings in the tile's unrotated frame.</summary>
		public string[] Rows = Array.Empty<string>();

		/// <summary>Edges printed on a single side of a space.</summary>
		public TileEdge[] Edges = Array.Empty<TileEdge>();

		public SquareFlags FlagsAt( int c, int r )
		{
			if ( r < 0 || r >= Rows.Length ) return SquareFlags.Normal;
			var row = Rows[r];
			if ( c < 0 || c >= row.Length ) return SquareFlags.Normal;
			switch ( row[c] )
			{
				case 'd': return SquareFlags.Difficult;
				case 'X': return SquareFlags.Blocking;
				case 'I': return SquareFlags.Impassable;
				case 'P': return SquareFlags.Pit;
				case '-': return SquareFlags.Void;
				default: return SquareFlags.Normal;
			}
		}
	}

	public sealed class TerrainLibrary
	{
		private readonly Dictionary<string, TileTerrain> _tiles =
			new Dictionary<string, TileTerrain>( StringComparer.OrdinalIgnoreCase );

		private static string Key( string exp, string id, string side ) => $"{exp}_{id}{side}";

		public void Add( TileTerrain t ) => _tiles[Key( t.Expansion, t.TileId, t.Side )] = t;

		public TileTerrain For( string exp, string id, string side )
			=> _tiles.TryGetValue( Key( exp, id, side ), out var t ) ? t : null;

		public int Count => _tiles.Count;

		/// <summary>Every loaded tile face, for diagnostics and verification.</summary>
		public IEnumerable<TileTerrain> All => _tiles.Values;
	}
}
