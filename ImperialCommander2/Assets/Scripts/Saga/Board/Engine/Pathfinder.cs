using System;
using System.Collections.Generic;

namespace Saga.Board
{
	/// <summary>Occupancy and capability inputs for a movement query.</summary>
	public sealed class MoveOptions
	{
		/// <summary>Friendly and neutral figures may be moved through at NO additional cost.</summary>
		public Func<Sq, bool> Friendly = _ => false;

		/// <summary>Hostile figures may be moved through, but entering their space costs ONE ADDITIONAL movement point.</summary>
		public Func<Sq, bool> Hostile = _ => false;

		/// <summary>The Massive keyword.</summary>
		public bool Massive;

		/// <summary>Spaces held by OTHER Massive figures.</summary>
		public Func<Sq, bool> OtherMassive = _ => false;

		/// <summary>Figure footprint.</summary>
		public Footprint Footprint = Footprint.Small1x1;

		public Facing StartFacing = Facing.NorthSouth;
	}

	/// <summary>A movement state: where the figure's anchor is and how it is turned.</summary>
	public readonly struct MoveState : IEquatable<MoveState>
	{
		public readonly Sq Anchor;
		public readonly Facing Facing;

		public MoveState( Sq anchor, Facing facing )
		{
			Anchor = anchor;
			Facing = facing;
		}

		public bool Equals( MoveState o ) => Anchor == o.Anchor && Facing == o.Facing;
		public override bool Equals( object o ) => o is MoveState m && Equals( m );
		public override int GetHashCode() => unchecked(Anchor.GetHashCode() * 31) ^ (int)Facing;
		public override string ToString() => $"{Anchor}/{Facing}";
	}

	/// <summary>Result of a reachability query: cost to every reachable state, and the predecessor chain needed to replay a path step by step.</summary>
	public sealed class Reach
	{
		public readonly Dictionary<MoveState, int> Cost = new Dictionary<MoveState, int>();

		/// <summary>ALL predecessors that reach a state at its minimum cost, not just the first one found.</summary>
		public readonly Dictionary<MoveState, List<MoveState>> Preds
			= new Dictionary<MoveState, List<MoveState>>();

		public MoveState Start;

		/// <summary>Cheapest cost to stand on a square in any facing, or -1.</summary>
		public int CostTo( Sq s )
		{
			int best = -1;
			foreach ( var kv in Cost )
			{
				if ( kv.Key.Anchor != s ) continue;
				if ( best < 0 || kv.Value < best ) best = kv.Value;
			}
			return best;
		}

		public bool CanReach( Sq s ) => CostTo( s ) >= 0;

		/// <summary>Squares a figure could legally finish its movement on.</summary>
		public IEnumerable<Sq> EndSquares( Func<Sq, bool> canEndOn )
		{
			var seen = new HashSet<Sq>();
			foreach ( var kv in Cost )
			{
				var s = kv.Key.Anchor;
				if ( !seen.Add( s ) ) continue;
				if ( canEndOn( s ) ) yield return s;
			}
		}

		/// <summary>The straightest minimum-cost path to a state.</summary>
		public List<MoveState> PathTo( MoveState end )
		{
			var path = new List<MoveState>();
			if ( !Cost.ContainsKey( end ) ) return path;

			var cur = end;
			path.Add( cur );
			int dc = 0, dr = 0;   // direction we are currently travelling, backwards

			var guard = 0;
			while ( !cur.Equals( Start ) && Preds.TryGetValue( cur, out var options )
					&& options.Count > 0 && guard++ < 10000 )
			{
				MoveState best = options[0];
				int bestScore = int.MinValue;
				foreach ( var p in options )
				{
					int pdc = cur.Anchor.C - p.Anchor.C;
					int pdr = cur.Anchor.R - p.Anchor.R;

					// Continuing in the same direction scores highest; a rotation
					// in place is neutral; anything else is a turn.
					int score;
					if ( pdc == 0 && pdr == 0 ) score = 1;
					else if ( pdc == dc && pdr == dr ) score = 3;
					else if ( pdc == 0 || pdr == 0 ) score = 2;   // orthogonal over diagonal
					else score = 0;

					if ( score > bestScore )
					{
						bestScore = score;
						best = p;
					}
				}

				dc = cur.Anchor.C - best.Anchor.C;
				dr = cur.Anchor.R - best.Anchor.R;
				cur = best;
				path.Add( cur );
			}
			path.Reverse();
			return path;
		}

		public List<MoveState> PathTo( Sq s )
		{
			MoveState best = default;
			int bestCost = -1;
			foreach ( var kv in Cost )
			{
				if ( kv.Key.Anchor != s ) continue;
				if ( bestCost < 0 || kv.Value < bestCost ) { bestCost = kv.Value; best = kv.Key; }
			}
			return bestCost < 0 ? new List<MoveState>() : PathTo( best );
		}
	}

	/// <summary>Movement over the board.</summary>
	public static class Pathfinder
	{
		public static Reach Compute( BoardModel b, Sq start, int movementPoints, MoveOptions opt = null )
		{
			opt = opt ?? new MoveOptions();
			var reach = new Reach();
			var startState = new MoveState( start, opt.StartFacing );
			reach.Start = startState;
			reach.Cost[startState] = 0;

			// Small integer costs, so a bucket queue beats a comparison heap and
			// keeps ordering deterministic.
			var buckets = new List<List<MoveState>>();
			void Push( MoveState s, int c )
			{
				while ( buckets.Count <= c ) buckets.Add( new List<MoveState>() );
				buckets[c].Add( s );
			}
			Push( startState, 0 );

			for ( int cost = 0; cost <= movementPoints && cost < buckets.Count; cost++ )
			{
				var bucket = buckets[cost];
				for ( int i = 0; i < bucket.Count; i++ )
				{
					var cur = bucket[i];
					if ( !reach.Cost.TryGetValue( cur, out var known ) || known != cost ) continue;

					foreach ( var (next, step) in Moves( b, cur, opt ) )
					{
						int nc = cost + step;
						if ( nc > movementPoints ) continue;

						if ( reach.Cost.TryGetValue( next, out var prev ) )
						{
							if ( prev < nc ) continue;
							// Equal cost: record this as another optimal route in,
							// so PathTo can choose the straightest among them.
							if ( prev == nc )
							{
								reach.Preds[next].Add( cur );
								continue;
							}
						}
						reach.Cost[next] = nc;
						reach.Preds[next] = new List<MoveState> { cur };
						Push( next, nc );
					}
				}
			}
			return reach;
		}

		/// <summary>Legal single moves from a state, with their movement cost.</summary>
		private static IEnumerable<(MoveState, int)> Moves( BoardModel b, MoveState cur, MoveOptions opt )
		{
			bool small = opt.Footprint == Footprint.Small1x1;

			for ( int dr = -1; dr <= 1; dr++ )
			{
				for ( int dc = -1; dc <= 1; dc++ )
				{
					if ( dc == 0 && dr == 0 ) continue;
					bool diagonal = dc != 0 && dr != 0;
					if ( diagonal && !small ) continue;   // large figures move orthogonally only

					var dest = cur.Anchor.Step( dc, dr );
					var next = new MoveState( dest, cur.Facing );
					if ( !CanOccupy( b, next, opt ) ) continue;
					if ( !StepAllowed( b, cur, next, opt ) ) continue;
					yield return (next, EnterCost( b, next, opt ));
				}
			}

			// Rotation costs 1 MP and the new footprint must share at least half
			// the spaces it previously occupied.
			if ( !small && opt.Footprint != Footprint.Large2x2 )
			{
				var rotated = new MoveState( cur.Anchor,
					cur.Facing == Facing.NorthSouth ? Facing.EastWest : Facing.NorthSouth );
				if ( CanOccupy( b, rotated, opt ) && SharesHalf( cur, rotated, opt ) )
					yield return (rotated, 1);
			}
		}

		/// <summary>Cost to enter, taken as the most expensive space the footprint lands on, so a large figure straddling difficult terrain pays for it.</summary>
		private static int EnterCost( BoardModel b, MoveState s, MoveOptions opt )
		{
			// "Massive figures can enter spaces containing hostile figures
			//  and/or difficult terrain at no additional movement cost."
			//  (Consolidated Rules p.41). Both surcharges are waived, so every
			//  step costs the base 1.
			if ( opt.Massive ) return 1;

			int cost = b.LargeFigurePaysWorstSpace ? 1 : int.MaxValue;
			bool hostile = false;
			foreach ( var c in Cells( s, opt.Footprint ) )
			{
				int e = b.EnterCost( c );
				if ( b.LargeFigurePaysWorstSpace ) { if ( e > cost ) cost = e; }
				else if ( e < cost ) cost = e;
				if ( opt.Hostile( c ) ) hostile = true;
			}
			if ( cost == int.MaxValue ) cost = 1;
			return cost + (hostile ? 1 : 0);
		}

		private static bool CanOccupy( BoardModel b, MoveState s, MoveOptions opt )
		{
			foreach ( var c in Cells( s, opt.Footprint ) )
			{
				if ( !b.Exists( c ) || !b.IsActive( c ) ) return false;
				var f = b.Flags( c );
				if ( (f & SquareFlags.Void) != 0 ) return false;
				if ( !opt.Massive )
				{
					if ( (f & (SquareFlags.Blocking | SquareFlags.Impassable | SquareFlags.Pit)) != 0 )
						return false;
				}
				else if ( opt.OtherMassive( c ) )
				{
					// "Massive figures cannot enter spaces containing other
					//  Massive figures." (Consolidated Rules p.41)
					return false;
				}
			}
			return true;
		}

		/// <summary>Edge legality for the step, applied to every cell of the footprint that actually crosses a boundary.</summary>
		private static bool StepAllowed( BoardModel b, MoveState from, MoveState to, MoveOptions opt )
		{
			var a = from.Anchor;
			var t = to.Anchor;

			if ( Sq.AreOrthogonal( a, t ) )
			{
				foreach ( var (ca, ct) in CellPairs( from, to, opt.Footprint ) )
				{
					if ( !b.Exists( ca ) || !b.Exists( ct ) ) return false;
					var e = b.Edge( ca, ct );
					if ( opt.Massive && (e == EdgeType.Blocking || e == EdgeType.Impassable) ) continue;
					if ( BoardModel.EdgeBlocksMovement( e ) ) return false;
				}
				return true;
			}

			// Diagonal, small figures only. Requires a clear orthogonal route
			// around the shared corner.
			return b.CanStep( a, t );
		}

		private static bool SharesHalf( MoveState a, MoveState b, MoveOptions opt )
		{
			var before = new HashSet<Sq>( Cells( a, opt.Footprint ) );
			int shared = 0, total = 0;
			foreach ( var c in Cells( b, opt.Footprint ) )
			{
				total++;
				if ( before.Contains( c ) ) shared++;
			}
			return total > 0 && shared * 2 >= total;
		}

		/// <summary>Squares covered by a footprint anchored at a state.</summary>
		public static IEnumerable<Sq> Cells( MoveState s, Footprint f )
		{
			switch ( f )
			{
				case Footprint.Small1x1:
					yield return s.Anchor;
					break;
				case Footprint.Medium1x2:
					yield return s.Anchor;
					yield return s.Facing == Facing.NorthSouth ? s.Anchor.South : s.Anchor.East;
					break;
				case Footprint.Large2x2:
					yield return s.Anchor;
					yield return s.Anchor.East;
					yield return s.Anchor.South;
					yield return s.Anchor.South.East;
					break;
				case Footprint.Huge2x3:
					for ( int i = 0; i < (s.Facing == Facing.NorthSouth ? 3 : 2); i++ )
					{
						for ( int j = 0; j < (s.Facing == Facing.NorthSouth ? 2 : 3); j++ )
							yield return s.Anchor.Step( j, i );
					}
					break;
			}
		}

		/// <summary>Pairs of (source cell, destination cell) for an orthogonal step, so each crossed edge is checked exactly once.</summary>
		private static IEnumerable<(Sq, Sq)> CellPairs( MoveState from, MoveState to, Footprint f )
		{
			var src = new List<Sq>( Cells( from, f ) );
			var dst = new List<Sq>( Cells( to, f ) );
			int dc = to.Anchor.C - from.Anchor.C;
			int dr = to.Anchor.R - from.Anchor.R;
			foreach ( var s in src )
			{
				var moved = s.Step( dc, dr );
				if ( dst.Contains( moved ) || src.Contains( moved ) )
					yield return (s, moved);
			}
		}

		/// <summary>A figure may pass through a friendly figure but not finish its movement there.</summary>
		public static Func<Sq, bool> CanEndOn( BoardModel b, MoveOptions opt )
		{
			if ( opt != null && opt.Massive )
				return s => b.Exists( s ) && b.IsActive( s )
					&& (b.Flags( s ) & SquareFlags.Void) == 0
					&& !opt.OtherMassive( s );

			return s => b.IsEnterable( s )
				&& !(opt != null && opt.Friendly( s ))
				&& !(opt != null && opt.Hostile( s ));
		}
	}
}
