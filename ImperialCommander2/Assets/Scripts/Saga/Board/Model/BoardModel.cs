using System;
using System.Collections.Generic;

namespace Saga.Board
{
	/// <summary>The board: which squares exist, what terrain occupies them, and what sits on the boundary between them.</summary>
	public sealed class BoardModel
	{
		private readonly Dictionary<Sq, SquareFlags> _squares = new Dictionary<Sq, SquareFlags>();
		private readonly Dictionary<(Sq, EdgeDir), EdgeType> _edges = new Dictionary<(Sq, EdgeDir), EdgeType>();
		private readonly HashSet<Sq> _inactive = new HashSet<Sq>();

		/// <summary>How diagonal movement past a corner is adjudicated.</summary>
		public bool StrictDiagonals { get; set; } = true;

		/// <summary>Whether a corner that sits ON a barrier may trace THROUGH it.</summary>
		public bool StrictBarrierCorners { get; set; } = true;

		/// <summary>How a large figure's movement cost is taken from the several spaces it occupies.</summary>
		public bool LargeFigurePaysWorstSpace { get; set; } = true;

		public int Count => _squares.Count;
		public IEnumerable<Sq> Squares => _squares.Keys;

		// ---------- squares ----------

		public void SetSquare( Sq s, SquareFlags flags ) => _squares[s] = flags;

		public bool Exists( Sq s ) => _squares.ContainsKey( s );

		public SquareFlags Flags( Sq s )
			=> _squares.TryGetValue( s, out var f ) ? f : SquareFlags.None;

		/// <summary>A square in a map section that has not been revealed yet is on the board but must not be pathed through.</summary>
		public void SetActive( Sq s, bool active )
		{
			if ( active ) _inactive.Remove( s );
			else _inactive.Add( s );
		}

		public bool IsActive( Sq s ) => !_inactive.Contains( s );

		/// <summary>Can a figure ever stand here, ignoring other figures?.</summary>
		public bool IsEnterable( Sq s )
		{
			if ( !Exists( s ) || !IsActive( s ) ) return false;
			var f = Flags( s );
			if ( (f & SquareFlags.Void) != 0 ) return false;
			if ( (f & SquareFlags.Blocking) != 0 ) return false;
			if ( (f & SquareFlags.Impassable) != 0 ) return false;
			if ( (f & SquareFlags.Pit) != 0 ) return false;
			return true;
		}

		/// <summary>Movement points to enter this space.</summary>
		public int EnterCost( Sq s )
			=> (Flags( s ) & SquareFlags.Difficult) != 0 ? 2 : 1;

		/// <summary>Blocking terrain stops line of sight; impassable does not.</summary>
		public bool SquareBlocksLos( Sq s )
			=> (Flags( s ) & SquareFlags.Blocking) != 0;

		// ---------- edges ----------

		/// <summary>Resolve the canonical owner of the edge between two orthogonally adjacent squares.</summary>
		public static bool TryEdgeKey( Sq a, Sq b, out (Sq, EdgeDir) key )
		{
			if ( a.C == b.C && b.R == a.R + 1 ) { key = (b, EdgeDir.N); return true; }
			if ( a.C == b.C && a.R == b.R + 1 ) { key = (a, EdgeDir.N); return true; }
			if ( a.R == b.R && b.C == a.C + 1 ) { key = (b, EdgeDir.W); return true; }
			if ( a.R == b.R && a.C == b.C + 1 ) { key = (a, EdgeDir.W); return true; }
			key = default;
			return false;
		}

		public void SetEdge( Sq a, Sq b, EdgeType type )
		{
			if ( !TryEdgeKey( a, b, out var key ) )
				throw new ArgumentException( $"{a} and {b} are not orthogonal neighbours" );
			SetEdge( key.Item1, key.Item2, type );
		}

		public void SetEdge( Sq owner, EdgeDir dir, EdgeType type ) => _edges[(owner, dir)] = type;

		public EdgeType Edge( Sq a, Sq b )
			=> TryEdgeKey( a, b, out var key ) && _edges.TryGetValue( key, out var t ) ? t : EdgeType.Open;

		public EdgeType Edge( Sq owner, EdgeDir dir )
			=> _edges.TryGetValue( (owner, dir), out var t ) ? t : EdgeType.Open;

		/// <summary>Walls and closed doors sever adjacency.</summary>
		public static bool EdgeBreaksAdjacency( EdgeType t )
			=> t == EdgeType.Wall || t == EdgeType.DoorClosed;

		public static bool EdgeBlocksMovement( EdgeType t )
			=> t == EdgeType.Wall || t == EdgeType.DoorClosed
			   || t == EdgeType.Blocking || t == EdgeType.Impassable;

		/// <summary>Impassable is explicitly transparent to line of sight.</summary>
		public static bool EdgeBlocksLos( EdgeType t )
			=> t == EdgeType.Wall || t == EdgeType.DoorClosed || t == EdgeType.Blocking;

		// ---------- adjacency ----------

		/// <summary>Rules adjacency, used for melee range and "closest figure" questions.</summary>
		public bool AreAdjacent( Sq a, Sq b )
		{
			if ( a == b ) return false;
			if ( !Exists( a ) || !Exists( b ) ) return false;

			if ( Sq.AreOrthogonal( a, b ) )
				return !EdgeBreaksAdjacency( Edge( a, b ) );

			if ( !Sq.AreDiagonal( a, b ) ) return false;
			return DiagonalOpen( a, b, EdgeBreaksAdjacency );
		}

		/// <summary>Can a figure step from a to b, ignoring occupancy and cost?.</summary>
		public bool CanStep( Sq a, Sq b )
		{
			if ( !IsEnterable( b ) || !Exists( a ) ) return false;

			if ( Sq.AreOrthogonal( a, b ) )
				return !EdgeBlocksMovement( Edge( a, b ) );

			if ( !Sq.AreDiagonal( a, b ) ) return false;
			return DiagonalOpen( a, b, EdgeBlocksMovement );
		}

		/// <summary>A diagonal move or adjacency passes the corner shared by a and b.</summary>
		private bool DiagonalOpen( Sq a, Sq b, Func<EdgeType, bool> blocked )
		{
			var viaRow = new Sq( b.C, a.R );   // horizontal first
			var viaCol = new Sq( a.C, b.R );   // vertical first

			bool routeRow = Exists( viaRow )
				&& !blocked( Edge( a, viaRow ) ) && !blocked( Edge( viaRow, b ) );
			bool routeCol = Exists( viaCol )
				&& !blocked( Edge( a, viaCol ) ) && !blocked( Edge( viaCol, b ) );

			return StrictDiagonals ? (routeRow || routeCol) : (routeRow || routeCol);
		}

		/// <summary>Orthogonal and diagonal neighbours that exist on the board.</summary>
		public IEnumerable<Sq> Neighbours( Sq s )
		{
			for ( int dr = -1; dr <= 1; dr++ )
			{
				for ( int dc = -1; dc <= 1; dc++ )
				{
					if ( dc == 0 && dr == 0 ) continue;
					var n = s.Step( dc, dr );
					if ( Exists( n ) ) yield return n;
				}
			}
		}
	}
}
