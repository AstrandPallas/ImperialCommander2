using System.Collections.Generic;

namespace Saga.Board
{
	/// <summary>A figure as line of sight sees it: where it stands, whether it blocks, and any ability that removes it from sight at range.</summary>
	public sealed class FigureVisibility
	{
		public string Id;
		public Sq Position;

		/// <summary>Distance at or beyond which this figure is NOT considered to be in a hostile figure's line of sight.</summary>
		public int HiddenAtOrBeyond = -1;

		/// <summary>Figures block line of sight by default.</summary>
		public bool BlocksLineOfSight = true;

		/// <summary>The Massive keyword.</summary>
		public bool Massive;

		public bool IsHiddenFrom( BoardModel board, Sq observer )
		{
			if ( HiddenAtOrBeyond < 0 ) return false;
			int d = Distance.Count( board, observer, Position, 60, Massive );
			if ( d < 0 ) return true;               // cannot even be counted to
			return d >= HiddenAtOrBeyond;
		}
	}

	/// <summary>Applies figure abilities on top of geometric line of sight.</summary>
	public static class Visibility
	{
		/// <summary>Figures that block line of sight for this particular observer, excluding the observer and the intended target.</summary>
		public static HashSet<Sq> BlockersFor(
			BoardModel board, Sq observer, IEnumerable<FigureVisibility> figures,
			Sq? target = null, bool endpointIsMassive = false )
		{
			var set = new HashSet<Sq>();
			if ( figures == null ) return set;

			// "Figures do not block line of sight to or from a Massive figure."
			// (Consolidated Rules p.41). When either end of the line is Massive,
			// no figure screens it, so the blocker set is simply empty.
			if ( endpointIsMassive ) return set;
			foreach ( var f in figures )
			{
				if ( f == null || !f.BlocksLineOfSight ) continue;
				if ( f.Position == observer ) continue;
				if ( target.HasValue && f.Position == target.Value ) continue;
				if ( f.IsHiddenFrom( board, observer ) ) continue;   // hidden figures do not block
				set.Add( f.Position );
			}
			return set;
		}

		/// <summary>Can the observer see this figure, accounting for both geometry and abilities?.</summary>
		public static bool CanSee(
			BoardModel board, Sq observer, FigureVisibility target,
			IEnumerable<FigureVisibility> others = null )
		{
			if ( target == null ) return false;
			if ( target.IsHiddenFrom( board, observer ) ) return false;

			var blockers = BlockersFor( board, observer, others, target.Position,
				target.Massive );

			// The rest of that entry -- "line of sight can be traced to that
			// figure, spaces can be counted to that figure, and adjacent
			// figures can attack that figure" for a Massive figure standing on
			// blocking terrain -- needs no special case here. HasLos already
			// excludes both endpoints from the blocker set, so a target never
			// blocks the shot at it, whatever terrain it stands on.
			return LineOfSight.HasLos( board, observer, target.Position,
				blockers.Contains );
		}
	}
}
