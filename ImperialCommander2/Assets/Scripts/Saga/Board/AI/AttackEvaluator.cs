using System.Collections.Generic;

namespace Saga.Board
{
	public enum AttackKind
	{
		Melee = 0,
		Ranged = 1,
	}

	/// <summary>Whether an attack may be declared from a given space, and how demanding it would be.</summary>
	public sealed class AttackAssessment
	{
		public bool CanDeclare;
		public AttackKind Kind;
		public int Distance = -1;

		/// <summary>Accuracy the dice must meet or beat.</summary>
		public int RequiredAccuracy;

		public string Reason = "";

		public override string ToString()
			=> CanDeclare
				? $"{Kind} at distance {Distance} (needs accuracy {RequiredAccuracy})"
				: $"cannot attack: {Reason}";
	}

	/// <summary>Can this figure attack that one from here? Rules Reference: "Melee attacks can only target figures adjacent to the attacker.</summary>
	public static class AttackEvaluator
	{
		public static AttackAssessment Assess(
			BoardModel board, Sq attacker, Sq target, AttackKind kind,
			IEnumerable<Sq> blockingFigures = null,
			FigureVisibility targetVisibility = null,
			bool hasReach = false )
		{
			var a = new AttackAssessment { Kind = kind };

			if ( !board.Exists( attacker ) || !board.Exists( target ) )
			{
				a.Reason = "one of the spaces is off the board";
				return a;
			}

			if ( targetVisibility != null && targetVisibility.IsHiddenFrom( board, attacker ) )
			{
				a.Reason = "target is not considered to be in line of sight at this range";
				return a;
			}

			var occupied = blockingFigures == null ? null : new HashSet<Sq>( blockingFigures );
			System.Func<Sq, bool> blocks = occupied == null
				? (System.Func<Sq, bool>)null
				: s => occupied.Contains( s );

			if ( kind == AttackKind.Melee )
			{
				// "Melee attacks can target a hostile figure or object adjacent to
				//  or in the same space as the attacker. Accuracy is not required
				//  while performing melee attacks." (Consolidated Rules p.41).
				//  Note there is no line of sight requirement on this one.
				if ( board.AreAdjacent( attacker, target ) )
				{
					a.CanDeclare = true;
					a.Distance = 1;
					a.RequiredAccuracy = 0;
					a.Reason = "adjacent";
					return a;
				}

				if ( !hasReach )
				{
					a.Reason = "melee requires an adjacent target";
					return a;
				}

				// "A figure with the Reach keyword may perform melee attacks that
				//  target figures or objects up to 2 spaces away. The target of the
				//  attack must be within 2 spaces and in line of sight. An attack
				//  with Reach does not require Accuracy." (Consolidated Rules p.51).
				//  Reach adds a way to attack rather than taking the ordinary
				//  adjacent one away, so it is only consulted once adjacency fails.
				if ( !LineOfSight.HasLos( board, attacker, target, blocks ) )
				{
					a.Reason = "reach needs line of sight to the target";
					return a;
				}

				int reachDistance = Distance.Count( board, attacker, target, 2,
					targetVisibility != null && targetVisibility.CountableOnBlockingTerrain );
				if ( reachDistance < 0 || reachDistance > 2 )
				{
					a.Reason = "reach targets up to 2 spaces away";
					return a;
				}

				a.CanDeclare = true;
				a.Distance = reachDistance;
				a.RequiredAccuracy = 0;
				a.Reason = $"reach at distance {reachDistance}";
				return a;
			}

			if ( !LineOfSight.HasLos( board, attacker, target, blocks ) )
			{
				a.Reason = "no line of sight";
				return a;
			}

			int d = Distance.Count( board, attacker, target );
			if ( d < 0 )
			{
				a.Reason = "target cannot be counted to";
				return a;
			}

			a.CanDeclare = true;
			a.Distance = d;
			a.RequiredAccuracy = d;
			a.Reason = $"in line of sight at distance {d}";
			return a;
		}

		/// <summary>Every space in a reachability set from which the attack could be declared, cheapest first.</summary>
		public static List<(Sq square, int moveCost, AttackAssessment attack)> FiringPositions(
			BoardModel board, Reach reach, Sq target, AttackKind kind,
			System.Func<Sq, bool> canEndOn, IEnumerable<Sq> blockingFigures = null,
			FigureVisibility targetVisibility = null, bool hasReach = false )
		{
			var blockers = blockingFigures == null ? null : new List<Sq>( blockingFigures );
			var results = new List<(Sq, int, AttackAssessment)>();
			var seen = new HashSet<Sq>();

			foreach ( var kv in reach.Cost )
			{
				var s = kv.Key.Anchor;
				if ( !seen.Add( s ) ) continue;
				if ( !canEndOn( s ) ) continue;
				var assessment = Assess( board, s, target, kind, blockers, targetVisibility,
					hasReach );
				if ( !assessment.CanDeclare ) continue;
				results.Add( (s, reach.CostTo( s ), assessment) );
			}

			results.Sort( ( x, y ) =>
			{
				int c = x.Item2.CompareTo( y.Item2 );
				if ( c != 0 ) return c;
				c = x.Item3.RequiredAccuracy.CompareTo( y.Item3.RequiredAccuracy );
				if ( c != 0 ) return c;
				c = x.Item1.C.CompareTo( y.Item1.C );
				return c != 0 ? c : x.Item1.R.CompareTo( y.Item1.R );
			} );
			return results;
		}
	}
}
