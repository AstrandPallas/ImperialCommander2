using System.Collections.Generic;

namespace Saga.Board
{
	/// <summary>
	/// The squares a mission is actually fought over: terminals, crates and
	/// other objective tokens still in play.
	/// </summary>
	/// <remarks>
	/// This is TACTICAL PREFERENCE, not a rule. Nothing here makes a move legal
	/// or illegal, and no claim in this file is traceable to the rulebook --
	/// the Rules Reference says nothing about where a good Imperial player
	/// should stand. It only replaces an arbitrary choice with a defensible
	/// one.
	///
	/// That distinction is why this sits apart from the priority chain. Which
	/// Rebel is targeted stays exactly as documented -- closest healthy, then
	/// least health remaining, then most total health, then closest overall,
	/// with ties still handed to the players. What changes is only which of
	/// several EQUALLY GOOD squares a figure ends on, a choice that was
	/// previously settled by column and then row, which is to say by nothing.
	///
	/// A resolved token is not contested: once a crate is opened, standing on
	/// it achieves nothing.
	/// </remarks>
	public sealed class ObjectiveMap
	{
		private readonly HashSet<Sq> _squares = new HashSet<Sq>();

		public int Count => _squares.Count;

		public IEnumerable<Sq> Squares => _squares;

		public static readonly ObjectiveMap Empty = new ObjectiveMap();

		public void Add( Sq square ) => _squares.Add( square );

		public bool IsObjective( Sq square ) => _squares.Contains( square );

		/// <summary>
		/// How strongly a square contests the mission's objectives.
		/// </summary>
		/// <remarks>
		/// Standing on the token is worth more than standing beside it, and
		/// everything else is worth nothing. Deliberately coarse: a finer
		/// gradient would start to outrank the things that genuinely matter,
		/// and this is only ever consulted after the shot and the movement
		/// cost have already been compared.
		/// </remarks>
		public int Contest( Sq square )
		{
			if ( _squares.Count == 0 ) return 0;
			if ( _squares.Contains( square ) ) return 2;
			foreach ( var o in _squares )
				if ( Sq.Chebyshev( square, o ) == 1 ) return 1;
			return 0;
		}

		/// <summary>Spaces to the nearest objective, or -1 when there are none.</summary>
		/// <remarks>
		/// Straight-line, not counted: this only ever orders candidate squares
		/// against each other, so it needs to be cheap and monotonic rather
		/// than exact, and a true count would be a pathfind per square.
		/// </remarks>
		public int DistanceToNearest( Sq square )
		{
			int best = -1;
			foreach ( var o in _squares )
			{
				int d = Sq.Chebyshev( square, o );
				if ( best < 0 || d < best ) best = d;
			}
			return best;
		}
	}
}
