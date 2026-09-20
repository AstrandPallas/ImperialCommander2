using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Saga.Board
{
	/// <summary>Renders board state and a plan as text.</summary>
	public static class BoardRenderer
	{
		public static string Render( BoardModel board,
			IDictionary<Sq, char> figures = null,
			IEnumerable<Sq> path = null,
			Sq? target = null )
		{
			var squares = board.Squares.ToList();
			if ( squares.Count == 0 ) return "(empty board)";

			int minC = squares.Min( s => s.C ), maxC = squares.Max( s => s.C );
			int minR = squares.Min( s => s.R ), maxR = squares.Max( s => s.R );

			var pathList = path?.ToList() ?? new List<Sq>();
			var pathIndex = new Dictionary<Sq, int>();
			for ( int i = 0; i < pathList.Count; i++ )
				if ( !pathIndex.ContainsKey( pathList[i] ) ) pathIndex[pathList[i]] = i;

			var sb = new StringBuilder();

			// Column ruler, so squares can be named unambiguously in a review.
			sb.Append( "    " );
			for ( int c = minC; c <= maxC; c++ ) sb.Append( ( c % 10 ).ToString() ).Append( ' ' );
			sb.Append( '\n' );

			for ( int r = minR; r <= maxR; r++ )
			{
				// north edge row
				sb.Append( "   " );
				for ( int c = minC; c <= maxC; c++ )
				{
					sb.Append( '+' );
					sb.Append( EdgeGlyph( board.Edge( new Sq( c, r ), EdgeDir.N ), true ) );
				}
				sb.Append( "+\n" );

				// square row
				sb.Append( ( r % 10 ).ToString().PadLeft( 2 ) ).Append( ' ' );
				for ( int c = minC; c <= maxC; c++ )
				{
					sb.Append( EdgeGlyph( board.Edge( new Sq( c, r ), EdgeDir.W ), false ) );
					sb.Append( SquareGlyph( board, new Sq( c, r ), figures, pathIndex, target ) );
				}
				var east = new Sq( maxC + 1, r );
				sb.Append( EdgeGlyph( board.Edge( east, EdgeDir.W ), false ) );
				sb.Append( '\n' );
			}

			// closing edge row
			sb.Append( "   " );
			for ( int c = minC; c <= maxC; c++ )
			{
				sb.Append( '+' );
				sb.Append( EdgeGlyph( board.Edge( new Sq( c, maxR + 1 ), EdgeDir.N ), true ) );
			}
			sb.Append( "+\n" );
			return sb.ToString();
		}

		private static char SquareGlyph( BoardModel board, Sq s,
			IDictionary<Sq, char> figures, Dictionary<Sq, int> pathIndex, Sq? target )
		{
			if ( figures != null && figures.TryGetValue( s, out var f ) ) return f;
			if ( target.HasValue && target.Value == s ) return 'T';
			if ( pathIndex.TryGetValue( s, out var i ) )
				return i == 0 ? '*' : (char)('0' + (i % 10));

			var flags = board.Flags( s );
			if ( (flags & SquareFlags.Difficult) != 0 ) return 'd';
			if ( (flags & SquareFlags.Blocking) != 0 ) return 'X';
			if ( (flags & SquareFlags.Impassable) != 0 ) return 'I';
			if ( (flags & SquareFlags.Pit) != 0 ) return 'P';
			if ( (flags & SquareFlags.Void) != 0 ) return '-';
			return board.Exists( s ) ? '.' : ' ';
		}

		private static char EdgeGlyph( EdgeType t, bool horizontal )
		{
			switch ( t )
			{
				case EdgeType.Wall: return horizontal ? '-' : '|';
				case EdgeType.Blocking: return horizontal ? '=' : '#';
				case EdgeType.Impassable: return ':';
				case EdgeType.DoorClosed: return 'D';
				case EdgeType.DoorOpen: return 'o';
				default: return ' ';
			}
		}

		/// <summary>Renders a full activation as an adjudicable record: the board, the orders, the costs, and the reasoning behind each.</summary>
		public static string RenderPlan( BoardModel board, ActivationPlan plan,
			IDictionary<Sq, char> figures = null )
		{
			var sb = new StringBuilder();
			sb.Append( "TARGET: " );
			if ( plan.GroupTarget?.Chosen == null ) sb.Append( "(none)\n" );
			else
			{
				sb.Append( plan.GroupTarget.Chosen.Name )
				  .Append( "   by rule: " ).Append( plan.GroupTarget.Rule ).Append( '\n' );
				foreach ( var t in plan.GroupTarget.Trace ) sb.Append( "   . " ).Append( t ).Append( '\n' );
				if ( plan.NeedsPlayerDecision )
					sb.Append( "   !! TIE - players decide between: " )
					  .Append( string.Join( ", ", plan.GroupTarget.Tied.Select( x => x.Name ) ) )
					  .Append( '\n' );
			}

			foreach ( var fp in plan.Figures )
			{
				sb.Append( '\n' ).Append( fp ).Append( '\n' );
				sb.Append( "   actions: " ).Append( fp.ActionsAvailable )
				  .Append( "   movement spent: " ).Append( fp.MovementSpent ).Append( '\n' );
				if ( fp.Path.Count > 1 )
					sb.Append( "   path: " )
					  .Append( string.Join( " -> ", fp.Path.Select( p => p.ToString() ) ) ).Append( '\n' );
				if ( fp.Attack != null )
					sb.Append( "   attack: " ).Append( fp.Attack ).Append( '\n' );
				foreach ( var t in fp.Trace ) sb.Append( "   . " ).Append( t ).Append( '\n' );

				sb.Append( Render( board, figures, fp.Path,
					fp.Target != null ? fp.Target.Position : (Sq?)null ) );
			}
			return sb.ToString();
		}
	}
}
