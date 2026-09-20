using System.Collections.Generic;

namespace Saga.Board
{
	/// <summary>Counting spaces between two points.</summary>
	public static class Distance
	{
		/// <summary>Number of spaces from a to b, or -1 when no countable route exists.</summary>
		public static int Count( BoardModel board, Sq a, Sq b, int limit = 60,
			bool targetOnBlockingIsCountable = false )
		{
			if ( a == b ) return 0;
			if ( !board.Exists( a ) || !board.Exists( b ) ) return -1;

			var seen = new HashSet<Sq> { a };
			var frontier = new List<Sq> { a };
			for ( int step = 1; step <= limit; step++ )
			{
				var next = new List<Sq>();
				foreach ( var cur in frontier )
				{
					foreach ( var n in board.Neighbours( cur ) )
					{
						if ( seen.Contains( n ) ) continue;
						bool exempt = n == b && targetOnBlockingIsCountable;
						if ( !CanCountInto( board, cur, n, exempt ) ) continue;
						if ( n == b ) return step;
						seen.Add( n );
						next.Add( n );
					}
				}
				if ( next.Count == 0 ) break;
				frontier = next;
			}
			return -1;
		}

		/// <summary>Spaces may not be counted through blocking terrain, walls or closed doors.</summary>
		private static bool CanCountInto( BoardModel board, Sq from, Sq to,
			bool ignoreSquareBlocking = false )
		{
			if ( !board.Exists( to ) ) return false;
			if ( !ignoreSquareBlocking && board.SquareBlocksLos( to ) )
				return false;   // blocking terrain

			if ( Sq.AreOrthogonal( from, to ) )
				return !BoardModel.EdgeBlocksLos( board.Edge( from, to ) );

			// Diagonal: needs a clear orthogonal route around the shared corner,
			// the same corner rule adjacency uses.
			return board.AreAdjacent( from, to );
		}
	}
}
