using System;
using System.Collections.Generic;
using System.Linq;
using Saga.Board;

namespace Saga.Tracking
{
	/// <summary>Why a square will not take a hero.</summary>
	public enum PlacementResult
	{
		Ok,
		OffBoard,
		NotEnterable,
		Occupied,
	}

	/// <summary>Where heroes start, and whether a square the players pick will take one.</summary>
	public static class HeroPlacement
	{
		/// <summary>
		/// Highlights whose name marks a Rebel entrance.
		/// </summary>
		/// <remarks>
		/// Highlights are also used for mission events -- "Reveal R2-1",
		/// "AT-ST", "Shuttle" -- so the name has to be read. Across the 138
		/// shipped missions, 136 carry at least one highlight and 127 of those
		/// are literally "Entrance", with the rest qualified versions of it
		/// ("Entrance 1", "Han Entrance", "Corridor Entrance").
		/// </remarks>
		public static bool IsEntrance( string name )
			=> !string.IsNullOrEmpty( name )
			   && name.IndexOf( "entrance", StringComparison.OrdinalIgnoreCase ) >= 0;

		/// <summary>
		/// Squares to seed the heroes on at setup, nearest the entrance first.
		/// </summary>
		/// <remarks>
		/// A highlight marks one square, but a party is four figures, so the
		/// rest are spread outwards onto whatever is enterable and free. This
		/// is a starting suggestion the players correct, not an assertion about
		/// where they actually put their minis.
		/// </remarks>
		public static List<Sq> SuggestStarts(
			BoardModel board,
			IEnumerable<(string Name, int C, int R)> highlights,
			int count,
			IEnumerable<Sq> occupied = null )
		{
			var starts = new List<Sq>();
			if ( board == null || count <= 0 ) return starts;

			var taken = new HashSet<Sq>( occupied ?? Enumerable.Empty<Sq>() );
			var entrances = (highlights ?? Enumerable.Empty<(string, int, int)>())
				.Where( h => IsEntrance( h.Name ) )
				.Select( h => new Sq( h.C, h.R ) )
				.ToList();

			if ( entrances.Count == 0 ) return starts;

			// Breadth-first from the entrances, so the party lands together and
			// on the near side of any wall rather than scattered by raw distance.
			var seen = new HashSet<Sq>( entrances );
			var frontier = new List<Sq>( entrances );

			while ( frontier.Count > 0 && starts.Count < count )
			{
				var next = new List<Sq>();
				foreach ( var sq in frontier.OrderBy( s => s.C ).ThenBy( s => s.R ) )
				{
					if ( starts.Count < count && board.IsEnterable( sq ) && !taken.Contains( sq ) )
					{
						starts.Add( sq );
						taken.Add( sq );
					}

					foreach ( var nb in board.Neighbours( sq ) )
					{
						if ( !seen.Add( nb ) ) continue;
						if ( board.CanStep( sq, nb ) ) next.Add( nb );
					}
				}
				frontier = next;
			}

			return starts;
		}

		/// <summary>Will this square take a hero?</summary>
		public static PlacementResult CanPlace( BoardModel board, Sq square,
			IEnumerable<Sq> occupied = null )
		{
			if ( board == null || !board.Exists( square ) || !board.IsActive( square ) )
				return PlacementResult.OffBoard;
			if ( !board.IsEnterable( square ) )
				return PlacementResult.NotEnterable;
			if ( occupied != null && occupied.Contains( square ) )
				return PlacementResult.Occupied;
			return PlacementResult.Ok;
		}

		/// <summary>Every square currently holding a figure.</summary>
		public static HashSet<Sq> OccupiedSquares(
			IEnumerable<HeroCombatState> heroes,
			IEnumerable<GroupCombatState> groups )
		{
			var set = new HashSet<Sq>();
			foreach ( var h in heroes ?? Enumerable.Empty<HeroCombatState>() )
			{
				if ( !h.InPlay || h.PosC == null || h.PosR == null ) continue;
				set.Add( new Sq( h.PosC.Value, h.PosR.Value ) );
			}
			foreach ( var g in groups ?? Enumerable.Empty<GroupCombatState>() )
			{
				// A large figure holds every square of its base, not just the
				// anchor. Recording only the anchor let the rest of a 2x2 base
				// read as free, so other figures were seated inside it.
				var footprint = (g.Profile ?? UnitProfile.Default).Footprint;
				foreach ( var slot in g.Figures )
				{
					if ( !slot.Alive || !slot.HasPosition ) continue;
					foreach ( var cell in Cells( new Sq( slot.PosC.Value, slot.PosR.Value ), footprint ) )
						set.Add( cell );
				}
			}
			return set;
		}

		/// <summary>The squares a figure of this size covers when anchored here.</summary>
		/// <remarks>
		/// The tracker does not yet record which way a long base is turned, so
		/// a 1x2 or 2x3 is taken as standing north-south. A 2x2 -- the Nexu,
		/// and most of what actually deploys -- is the same either way.
		/// </remarks>
		public static IEnumerable<Sq> Cells( Sq anchor, Footprint footprint )
			=> Pathfinder.Cells( new MoveState( anchor, Facing.NorthSouth ), footprint );

		/// <summary>
		/// Will a figure of this size stand here? Every square of the base
		/// must exist, be enterable, and be free.
		/// </summary>
		/// <remarks>
		/// This is what was missing when the Nexu was seated: its anchor square
		/// was fine and the other three squares of its base ran off the tile,
		/// so the planner could never fit it anywhere and it held all mission.
		/// </remarks>
		public static bool Fits( BoardModel board, Sq anchor, Footprint footprint,
			IEnumerable<Sq> occupied = null )
		{
			if ( board == null ) return false;
			var taken = occupied as ICollection<Sq> ?? occupied?.ToList();
			foreach ( var cell in Cells( anchor, footprint ) )
			{
				if ( !board.IsEnterable( cell ) ) return false;
				if ( taken != null && taken.Contains( cell ) ) return false;
			}
			return true;
		}

		/// <summary>
		/// Nearest square that will take a hero, for snapping a dragged pin.
		/// </summary>
		/// <remarks>
		/// A finger on a touch screen lands between squares as often as on one,
		/// so a drop onto a wall snaps outward rather than being rejected.
		/// Returns null only when nothing within reach is free.
		/// </remarks>
		public static Sq? Snap( BoardModel board, Sq wanted,
			IEnumerable<Sq> occupied = null, int searchRadius = 3,
			Footprint footprint = Footprint.Small1x1 )
		{
			if ( board == null ) return null;
			var taken = occupied as ICollection<Sq> ?? occupied?.ToList();

			// A large figure snaps to the nearest anchor its whole base fits
			// on, which is what a player dragging a Nexu means by "here".
			if ( footprint != Footprint.Small1x1 )
			{
				Sq? best = null;
				int bestD = int.MaxValue;
				for ( int dc = -searchRadius; dc <= searchRadius; dc++ )
					for ( int dr = -searchRadius; dr <= searchRadius; dr++ )
					{
						var anchor = wanted.Step( dc, dr );
						if ( !Fits( board, anchor, footprint, taken ) ) continue;
						int d = Math.Max( Math.Abs( dc ), Math.Abs( dr ) ) * 10 + Math.Abs( dc ) + Math.Abs( dr );
						if ( d < bestD ) { bestD = d; best = anchor; }
					}
				return best;
			}

			if ( CanPlace( board, wanted, taken ) == PlacementResult.Ok ) return wanted;

			for ( int ring = 1; ring <= searchRadius; ring++ )
			{
				Sq? best = null;
				for ( int dc = -ring; dc <= ring; dc++ )
				{
					for ( int dr = -ring; dr <= ring; dr++ )
					{
						if ( Math.Max( Math.Abs( dc ), Math.Abs( dr ) ) != ring ) continue;
						var candidate = new Sq( wanted.C + dc, wanted.R + dr );
						if ( CanPlace( board, candidate, taken ) != PlacementResult.Ok ) continue;
						// Ties resolve the same way every time, so a drop in the
						// same spot always lands on the same square.
						if ( best == null
							 || candidate.C < best.Value.C
							 || (candidate.C == best.Value.C && candidate.R < best.Value.R) )
							best = candidate;
					}
				}
				if ( best != null ) return best;
			}
			return null;
		}
	}
}
