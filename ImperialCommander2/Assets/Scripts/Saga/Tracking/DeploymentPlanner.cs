using System.Collections.Generic;
using System.Linq;
using Saga.Board;

namespace Saga.Tracking
{
	/// <summary>A deployment point, scored for how useful arriving there would be.</summary>
	public sealed class DeploymentChoice
	{
		public string Name;
		public Sq Square;

		/// <summary>Counted spaces to the nearest Rebel, or -1 when none can be counted.</summary>
		public int ToNearestRebel = -1;

		/// <summary>Straight-line spaces to the nearest unresolved objective, or -1.</summary>
		public int ToNearestObjective = -1;

		/// <summary>Why this point was or was not chosen, in one line.</summary>
		public string Reason = "";

		public override string ToString()
			=> $"{Name} at {Square}: {Reason}";
	}

	/// <summary>
	/// Where a newly deployed group should arrive and stand.
	/// </summary>
	/// <remarks>
	/// This is a SUGGESTION and nothing more. Which deployment point a mission
	/// means is frequently decided by its own rules text, which the app cannot
	/// read, so the players correct it by dragging exactly as they correct a
	/// hero's position. What this replaces is not a considered choice but the
	/// first point in the list, which was arbitrary.
	///
	/// None of it is a game rule. The Rules Reference does not say where the
	/// Imperial player should deploy, and the reasoning here -- arrive nearest
	/// the Rebels, prefer squares that can actually see them -- is ordinary
	/// tactical judgement recorded as a house rule.
	/// </remarks>
	public static class DeploymentPlanner
	{
		/// <summary>
		/// Score every active deployment point and return them, best first.
		/// </summary>
		/// <remarks>
		/// Counted distance to the nearest Rebel decides it: a group that
		/// arrives across the map from the fight does nothing for several
		/// rounds, which is the one outcome an Imperial player would never
		/// choose. A point with no countable route to any Rebel sorts last
		/// rather than being discarded -- it may be the only one the mission
		/// allows.
		/// </remarks>
		public static List<DeploymentChoice> RankPoints(
			BoardModel board,
			IEnumerable<(string Name, int C, int R)> points,
			IEnumerable<HeroCombatState> heroes,
			ObjectiveMap objectives = null )
		{
			var ranked = new List<DeploymentChoice>();
			if ( board == null ) return ranked;

			var rebels = (heroes ?? Enumerable.Empty<HeroCombatState>())
				.Where( h => h != null && h.InPlay && h.PosC != null && h.PosR != null )
				.Select( h => new Sq( h.PosC.Value, h.PosR.Value ) )
				.ToList();
			var map = objectives ?? ObjectiveMap.Empty;

			foreach ( var p in points ?? Enumerable.Empty<(string, int, int)>() )
			{
				var square = new Sq( p.C, p.R );
				var choice = new DeploymentChoice { Name = p.Name, Square = square };

				foreach ( var r in rebels )
				{
					int d = Distance.Count( board, square, r );
					if ( d < 0 ) continue;
					if ( choice.ToNearestRebel < 0 || d < choice.ToNearestRebel )
						choice.ToNearestRebel = d;
				}

				choice.ToNearestObjective = map.DistanceToNearest( square );

				choice.Reason = choice.ToNearestRebel < 0
					? "no counted route to any Rebel"
					: choice.ToNearestRebel + " spaces from the nearest Rebel"
					  + (choice.ToNearestObjective >= 0
						  ? ", " + choice.ToNearestObjective + " from an objective" : "");

				ranked.Add( choice );
			}

			ranked.Sort( ( a, b ) =>
			{
				// Unreachable points go last, but are still offered.
				bool au = a.ToNearestRebel < 0, bu = b.ToNearestRebel < 0;
				if ( au != bu ) return au ? 1 : -1;
				if ( !au )
				{
					int c = a.ToNearestRebel.CompareTo( b.ToNearestRebel );
					if ( c != 0 ) return c;
				}

				int ao = a.ToNearestObjective < 0 ? int.MaxValue : a.ToNearestObjective;
				int bo = b.ToNearestObjective < 0 ? int.MaxValue : b.ToNearestObjective;
				if ( ao != bo ) return ao.CompareTo( bo );

				// Deterministic, so the same board always suggests the same point.
				int s = a.Square.C.CompareTo( b.Square.C );
				return s != 0 ? s : a.Square.R.CompareTo( b.Square.R );
			} );

			return ranked;
		}

		/// <summary>
		/// Seat a group at a deployment point.
		/// </summary>
		/// <remarks>
		/// Squares are taken from a breadth-first walk outward over legal
		/// steps, so the group arrives together and on the near side of any
		/// wall. Among the squares found at the same remove, one that can see
		/// a Rebel is preferred: a figure deployed into a blind corner spends
		/// its first activation walking out of it.
		///
		/// Figures already on the board are never displaced. If fewer squares
		/// are found than the group has figures, the caller is told, rather
		/// than figures being stacked or silently dropped.
		/// </remarks>
		public static List<Sq> PlaceGroup(
			BoardModel board, Sq point, int figures,
			IEnumerable<Sq> occupied = null,
			IEnumerable<HeroCombatState> heroes = null,
			Footprint footprint = Footprint.Small1x1 )
		{
			var placed = new List<Sq>();
			if ( board == null || figures <= 0 ) return placed;

			var taken = new HashSet<Sq>( occupied ?? Enumerable.Empty<Sq>() );
			var rebels = (heroes ?? Enumerable.Empty<HeroCombatState>())
				.Where( h => h != null && h.InPlay && h.PosC != null && h.PosR != null )
				.Select( h => new Sq( h.PosC.Value, h.PosR.Value ) )
				.ToList();

			var seen = new HashSet<Sq> { point };
			var frontier = new List<Sq> { point };

			while ( frontier.Count > 0 && placed.Count < figures )
			{
				// Everything at this remove costs the group the same, so the
				// line of sight is what separates them.
				// A square only qualifies if the WHOLE base fits on it: for a
				// 2x2 that is four squares, all existing, enterable and free.
				var ring = frontier
					.Where( sq => HeroPlacement.Fits( board, sq, footprint, taken ) )
					.OrderByDescending( sq => SeesAnyRebel( board, sq, rebels ) ? 1 : 0 )
					.ThenBy( sq => sq.C )
					.ThenBy( sq => sq.R )
					.ToList();

				foreach ( var sq in ring )
				{
					if ( placed.Count >= figures ) break;
					if ( !HeroPlacement.Fits( board, sq, footprint, taken ) ) continue;
					placed.Add( sq );
					foreach ( var cell in HeroPlacement.Cells( sq, footprint ) ) taken.Add( cell );
				}

				var next = new List<Sq>();
				foreach ( var sq in frontier.OrderBy( s => s.C ).ThenBy( s => s.R ) )
					foreach ( var nb in board.Neighbours( sq ) )
					{
						if ( !seen.Add( nb ) ) continue;
						if ( board.CanStep( sq, nb ) ) next.Add( nb );
					}
				frontier = next;
			}

			return placed;
		}

		private static bool SeesAnyRebel( BoardModel board, Sq from, List<Sq> rebels )
		{
			foreach ( var r in rebels )
				if ( LineOfSight.HasLos( board, from, r ) ) return true;
			return false;
		}
	}
}
