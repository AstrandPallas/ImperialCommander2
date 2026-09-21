using System;
using System.Collections.Generic;
using System.Linq;

namespace Saga.Board
{
	/// <summary>One enemy figure in an activating group.</summary>
	public sealed class EnemyFigure
	{
		public string Id;
		public string Name;
		public Sq Position;
		public int Speed = 4;
		public AttackKind AttackKind = AttackKind.Ranged;
		public Footprint Footprint = Footprint.Small1x1;
		public bool Massive;

		/// <summary>The Mobile keyword, which shares Massive's terrain rules but none of its rules about figures.</summary>
		public bool Mobile;

		/// <summary>The Reach keyword. Named so it cannot be read as <see cref="Saga.Board.Reach"/>.</summary>
		public bool HasReach;

		/// <summary>A Stunned figure must spend one action to remove the condition, leaving it only one action this activation.</summary>
		public bool Stunned;
	}

	/// <summary>What one figure should do, and why.</summary>
	public sealed class FigurePlan
	{
		public EnemyFigure Figure;
		public Sq Start;
		public Sq End;
		public List<Sq> Path = new List<Sq>();
		public int MovementSpent;
		public int ActionsAvailable;
		public bool Moved;
		public TargetCandidate Target;
		public AttackAssessment Attack;
		public List<string> Trace = new List<string>();

		public bool WillAttack => Attack != null && Attack.CanDeclare;

		public override string ToString()
		{
			var move = Moved ? $"move {MovementSpent} to {End}" : $"hold at {End}";
			var atk = WillAttack ? $", attack {Target?.Name} (needs accuracy {Attack.RequiredAccuracy})"
								 : ", no attack";
			return $"{Figure.Name}: {move}{atk}";
		}
	}

	public sealed class ActivationPlan
	{
		public TargetDecision GroupTarget;
		public List<FigurePlan> Figures = new List<FigurePlan>();
		public bool NeedsPlayerDecision => GroupTarget != null && GroupTarget.NeedsPlayerDecision;
	}

	/// <summary>Turns board state into per-figure orders.</summary>
	public sealed class PlanOverride
	{
		/// <summary>Figures the players have already placed themselves, by figure id.</summary>
		public Dictionary<string, Sq> PlacedAt = new Dictionary<string, Sq>();

		/// <summary>Force the group's target, overriding the priority chain.</summary>
		public string TargetId;

		/// <summary>Why, in the players' words.</summary>
		public string Reason;

		public bool IsEmpty => PlacedAt.Count == 0 && string.IsNullOrEmpty( TargetId );
	}

	public static class ActivationPlanner
	{
		public static ActivationPlan Plan(
			BoardModel board,
			IEnumerable<EnemyFigure> group,
			IEnumerable<TargetCandidate> rebels,
			string[] preferredTraits = null,
			IEnumerable<FigureVisibility> allFigures = null,
			PlanOverride overrides = null )
		{
			var plan = new ActivationPlan();
			var figures = group.Where( f => f != null ).ToList();
			var targets = rebels.Where( r => r != null && r.InPlay ).ToList();
			if ( figures.Count == 0 || targets.Count == 0 ) return plan;

			var placed = new HashSet<string>();
			if ( overrides != null )
			{
				foreach ( var f in figures )
				{
					if ( f.Id == null || !overrides.PlacedAt.TryGetValue( f.Id, out var at ) )
						continue;
					f.Position = at;
					placed.Add( f.Id );
				}
			}

			// Hostiles for movement purposes are the Rebel figures; friendlies
			// are the rest of the activating group.
			var rebelSquares = new HashSet<Sq>( targets.Select( t => t.Position ) );
			var groupSquares = new HashSet<Sq>( figures.Select( f => f.Position ) );

			// Massive figures are tracked separately from the rest of the group.
			// Consolidated Rules p.41 lets a Massive figure finish on top of
			// ordinary figures, pushing them clear, but "Massive figures cannot
			// enter spaces containing other Massive figures" -- so the one thing
			// it must still path around is another of its own kind.
			var massiveSquares = new HashSet<Sq>(
				figures.Where( f => f.Massive ).Select( f => f.Position ) );
			var visibility = allFigures?.ToList();

			// The group's target is chosen once, from the figure best placed to
			// judge -- the one closest to the Rebels.
			var lead = figures
				.OrderBy( f => targets.Min( t => Sq.Chebyshev( f.Position, t.Position ) ) )
				.First();
			plan.GroupTarget = TargetSelector.Select( board, lead.Position, targets, preferredTraits,
				OptionsFor( lead, rebelSquares, groupSquares, massiveSquares ) );

			if ( overrides != null && !string.IsNullOrEmpty( overrides.TargetId ) )
			{
				var forced = targets.FirstOrDefault( t => t.Id == overrides.TargetId );
				if ( forced != null )
				{
					plan.GroupTarget.Trace.Add( "OVERRIDDEN by the players: target is "
						+ forced.Name
						+ (string.IsNullOrEmpty( overrides.Reason )
							? "" : " -- " + overrides.Reason) );
					plan.GroupTarget.Chosen = forced;
					plan.GroupTarget.Rule = "players decide";
					// A forced choice is not a tie, so clear the prompt.
					plan.GroupTarget.Tied.Clear();
				}
				else
				{
					plan.GroupTarget.Trace.Add( "override named target '" + overrides.TargetId
						+ "', which is not in play -- ignored" );
				}
			}

			if ( plan.GroupTarget.Chosen == null ) return plan;

			// Activate nearest first, so the figures that can actually engage
			// commit their squares before the ones trailing behind.
			var order = figures
				.OrderBy( f => Sq.Chebyshev( f.Position, plan.GroupTarget.Chosen.Position ) )
				.ThenBy( f => f.Id )
				.ToList();

			foreach ( var fig in order )
			{
				var fp = placed.Contains( fig.Id )
					? HeldWherePlayersPutIt( board, fig, plan.GroupTarget.Chosen, targets,
						visibility, overrides )
					: PlanFigure( board, fig, plan.GroupTarget.Chosen, targets,
						rebelSquares, groupSquares, massiveSquares, visibility );
				plan.Figures.Add( fp );

				// Commit this figure's destination so later figures path around
				// it rather than through the square it now occupies.
				groupSquares.Remove( fig.Position );
				groupSquares.Add( fp.End );
				if ( fig.Massive )
				{
					massiveSquares.Remove( fig.Position );
					massiveSquares.Add( fp.End );
				}
				if ( visibility != null )
				{
					var v = visibility.FirstOrDefault( x => x.Id == fig.Id );
					if ( v != null ) v.Position = fp.End;
				}
			}
			return plan;
		}

		/// <summary>A figure the players have already placed.</summary>
		private static FigurePlan HeldWherePlayersPutIt(
			BoardModel board, EnemyFigure fig, TargetCandidate groupTarget,
			List<TargetCandidate> allTargets, List<FigureVisibility> visibility,
			PlanOverride overrides )
		{
			var fp = new FigurePlan
			{
				Figure = fig,
				Start = fig.Position,
				End = fig.Position,
				Target = groupTarget,
				ActionsAvailable = fig.Stunned ? 1 : 2,
				Moved = false,
				MovementSpent = 0,
			};
			fp.Path = new List<Sq> { fig.Position };

			fp.Trace.Add( "placed by the players at " + fig.Position
				+ (overrides != null && !string.IsNullOrEmpty( overrides.Reason )
					? " -- " + overrides.Reason : "") );

			// Can it attack the group target from where it now stands?
			var best = AttackEvaluator.Assess( board, fig.Position, groupTarget.Position,
				fig.AttackKind, null, null, fig.HasReach );
			if ( best.CanDeclare )
			{
				fp.Attack = best;
				fp.Trace.Add( "can still attack " + groupTarget.Name + " from there" );
				return fp;
			}

			// If not, is anything else in reach? A figure the players moved is
			// still a figure that gets to act.
			foreach ( var other in allTargets )
			{
				if ( other == groupTarget || !other.InPlay ) continue;
				var alt = AttackEvaluator.Assess( board, fig.Position, other.Position,
					fig.AttackKind, null, null, fig.HasReach );
				if ( !alt.CanDeclare ) continue;
				fp.Target = other;
				fp.Attack = alt;
				fp.Trace.Add( "cannot reach " + groupTarget.Name + " from there; attacks "
					+ other.Name + " instead" );
				return fp;
			}

			fp.Trace.Add( "no attack is possible from there" );
			return fp;
		}

		private static MoveOptions OptionsFor( EnemyFigure f,
			HashSet<Sq> rebels, HashSet<Sq> allies, HashSet<Sq> massive )
			=> new MoveOptions
			{
				Hostile = rebels.Contains,
				Friendly = allies.Contains,
				Footprint = f.Footprint,
				Massive = f.Massive,
				Mobile = f.Mobile,
				// Its own square is excluded by the caller, so this is strictly
				// the OTHER Massive figures.
				OtherMassive = massive == null ? (Func<Sq, bool>)(_ => false)
					: massive.Contains,
			};

		private static FigurePlan PlanFigure(
			BoardModel board, EnemyFigure fig, TargetCandidate groupTarget,
			List<TargetCandidate> allTargets,
			HashSet<Sq> rebelSquares, HashSet<Sq> groupSquares,
			HashSet<Sq> massiveSquares, List<FigureVisibility> visibility )
		{
			var fp = new FigurePlan
			{
				Figure = fig,
				Start = fig.Position,
				End = fig.Position,
				Target = groupTarget,
				ActionsAvailable = fig.Stunned ? 1 : 2,
			};
			if ( fig.Stunned )
				fp.Trace.Add( "Stunned: one action spent removing the condition, one remains" );

			foreach ( var cell in Pathfinder.Cells(
				new MoveState( fig.Position, Facing.NorthSouth ), fig.Footprint ) )
			{
				if ( board.Exists( cell ) ) continue;
				fp.Trace.Add( $"cannot plan: {fig.Name}'s footprint does not fit at "
					+ $"{fig.Position} ({cell} is off the board)" );
				fp.Path = new List<Sq> { fig.Position };
				return fp;
			}

			var allies = new HashSet<Sq>( groupSquares );
			allies.Remove( fig.Position );
			var others = new HashSet<Sq>( massiveSquares );
			others.Remove( fig.Position );
			var opt = OptionsFor( fig, rebelSquares, allies, others );
			var canEnd = Pathfinder.CanEndOn( board, opt );

			// One action attacks, so the rest are available for movement.
			int moveActions = System.Math.Max( 0, fp.ActionsAvailable - 1 );
			int budget = moveActions * fig.Speed;
			var reach = Pathfinder.Compute( board, fig.Position, budget, opt );

			var blockers = visibility == null
				? null
				: Visibility.BlockersFor( board, fig.Position, visibility, groupTarget.Position );

			// An ability may put the target out of sight at range; that has to
			// reach the attack evaluation, not just the blocker set.
			System.Func<TargetCandidate, FigureVisibility> visOf = t => visibility?
				.FirstOrDefault( v => v.Id == t.Id || v.Position == t.Position );

			// Prefer a position that attacks the group's target.
			var spots = AttackEvaluator.FiringPositions( board, reach, groupTarget.Position,
				fig.AttackKind, canEnd, blockers, visOf( groupTarget ), fig.HasReach );
			if ( spots.Count > 0 )
			{
				var best = ChooseSpot( spots );
				Commit( fp, reach, best.square, best.moveCost, best.attack );
				fp.Trace.Add( $"attacks the group target {groupTarget.Name} from {best.square} "
					+ $"for {best.moveCost} movement" );
				return fp;
			}
			fp.Trace.Add( $"cannot reach a position to attack {groupTarget.Name}" );

			// Otherwise attack whoever it can reach.
			foreach ( var alt in allTargets.Where( t => t != groupTarget ) )
			{
				var altSpots = AttackEvaluator.FiringPositions( board, reach, alt.Position,
					fig.AttackKind, canEnd, blockers, visOf( alt ), fig.HasReach );
				if ( altSpots.Count == 0 ) continue;
				var best = ChooseSpot( altSpots );
				fp.Target = alt;
				Commit( fp, reach, best.square, best.moveCost, best.attack );
				fp.Trace.Add( $"falls back to {alt.Name}, reachable from {best.square}" );
				return fp;
			}
			fp.Trace.Add( "no target can be attacked this activation" );

			// Nothing attackable: close the distance instead. Both actions may
			// now be spent moving.
			int fullBudget = fp.ActionsAvailable * fig.Speed;
			var advance = Pathfinder.Compute( board, fig.Position, fullBudget, opt );
			var goal = groupTarget.Position;

			// When the target is sealed off -- behind a closed door, or in a map
			// section not yet revealed -- counting spaces to it returns "no
			// route". That must not stop the figure advancing: an Imperial
			// player walks up to the door. So fall back to straight-line
			// distance, which still points the right way when no countable route
			// exists. Returning "hold position" here was a real bug: a group
			// separated by a door simply stood still all mission.
			bool countable = Distance.Count( board, fig.Position, goal ) >= 0;
			System.Func<Sq, int> progress = sq =>
			{
				if ( countable )
				{
					int d = Distance.Count( board, sq, goal );
					if ( d >= 0 ) return d;
				}
				return Sq.Chebyshev( sq, goal );
			};

			Sq bestSq = fig.Position;
			int bestDist = progress( fig.Position );
			int bestCost = 0;

			foreach ( var sq in advance.EndSquares( canEnd ) )
			{
				int d = progress( sq );
				int cost = advance.CostTo( sq );
				if ( d < bestDist || (d == bestDist && cost < bestCost) )
				{
					bestDist = d;
					bestSq = sq;
					bestCost = cost;
				}
			}
			Commit( fp, advance, bestSq, bestCost, null );
			fp.Trace.Add( countable
				? $"advances toward {groupTarget.Name}, ending {bestDist} spaces away"
				: $"{groupTarget.Name} is sealed off; advances on straight-line distance, "
				  + $"ending {bestDist} spaces away" );
			return fp;
		}

		/// <summary>Among positions that can attack, prefer the easiest shot, then the least movement.</summary>
		private static (Sq square, int moveCost, AttackAssessment attack) ChooseSpot(
			List<(Sq square, int moveCost, AttackAssessment attack)> spots )
		{
			return spots
				.OrderBy( s => s.attack.RequiredAccuracy )
				.ThenBy( s => s.moveCost )
				.ThenBy( s => s.square.C )
				.ThenBy( s => s.square.R )
				.First();
		}

		private static void Commit( FigurePlan fp, Reach reach, Sq end, int cost,
			AttackAssessment attack )
		{
			fp.End = end;
			fp.MovementSpent = cost;
			fp.Moved = end != fp.Start;
			fp.Attack = attack;
			fp.Path = reach.PathTo( end ).Select( m => m.Anchor ).ToList();
			if ( fp.Path.Count == 0 ) fp.Path = new List<Sq> { fp.Start };
		}
	}
}
