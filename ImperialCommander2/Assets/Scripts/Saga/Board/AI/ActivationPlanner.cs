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
			PlanOverride overrides = null,
			ObjectiveMap objectives = null,
			IEnumerable<string> instructions = null )
		{
			var plan = new ActivationPlan();
			// The card's own lines, tried in order. Null means the caller has
			// none, and the generic move-and-attack is used throughout.
			var intents = instructions == null ? null : Instructions.ParseAll( instructions );
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
					// Either the players or the card ({R1}) can force the target;
					// the reason says which.
					plan.GroupTarget.Trace.Add( "OVERRIDDEN: target is "
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
						rebelSquares, groupSquares, massiveSquares, visibility,
						objectives ?? ObjectiveMap.Empty, intents );
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
			HashSet<Sq> massiveSquares, List<FigureVisibility> visibility,
			ObjectiveMap objectives = null, List<ActivationIntent> intents = null )
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

			// The card's own lines come first. A group does the FIRST line it
			// can; only when none applies does the generic plan below decide.
			if ( intents != null && intents.Count > 0
				&& TryIntents( board, fp, fig, groupTarget, allTargets, opt, canEnd,
					blockers, visOf, objectives, intents ) )
				return fp;

			// Prefer a position that attacks the group's target.
			var spots = AttackEvaluator.FiringPositions( board, reach, groupTarget.Position,
				fig.AttackKind, canEnd, blockers, visOf( groupTarget ), fig.HasReach );
			if ( spots.Count > 0 )
			{
				var best = ChooseSpot( spots, objectives );
				Commit( fp, reach, best.square, best.moveCost, best.attack );
				fp.Trace.Add( $"attacks the group target {groupTarget.Name} from {best.square} "
					+ $"for {best.moveCost} movement"
					+ ContestNote( objectives, best.square ) );
				return fp;
			}
			fp.Trace.Add( $"cannot reach a position to attack {groupTarget.Name}" );

			// Otherwise attack whoever it can reach.
			foreach ( var alt in allTargets.Where( t => t != groupTarget ) )
			{
				var altSpots = AttackEvaluator.FiringPositions( board, reach, alt.Position,
					fig.AttackKind, canEnd, blockers, visOf( alt ), fig.HasReach );
				if ( altSpots.Count == 0 ) continue;
				var best = ChooseSpot( altSpots, objectives );
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

			var map = objectives ?? ObjectiveMap.Empty;
			Sq bestSq = fig.Position;
			int bestDist = progress( fig.Position );
			int bestCost = 0;
			int bestContest = map.Contest( fig.Position );

			foreach ( var sq in advance.EndSquares( canEnd ) )
			{
				int d = progress( sq );
				int cost = advance.CostTo( sq );
				int contest = map.Contest( sq );

				// Closing on the target comes first, then spending less to do
				// it; contesting an objective only breaks what is left, which
				// is where a figure would otherwise have stopped arbitrarily.
				bool better = d < bestDist
					|| (d == bestDist && cost < bestCost)
					|| (d == bestDist && cost == bestCost && contest > bestContest);
				if ( !better ) continue;

				bestDist = d;
				bestSq = sq;
				bestCost = cost;
				bestContest = contest;
			}
			Commit( fp, advance, bestSq, bestCost, null );
			fp.Trace.Add( (countable
				? $"advances toward {groupTarget.Name}, ending {bestDist} spaces away"
				: $"{groupTarget.Name} is sealed off; advances on straight-line distance, "
				  + $"ending {bestDist} spaces away")
				+ ContestNote( map, bestSq ) );
			return fp;
		}

		/// <summary>Says so in the trace when the square was chosen for an objective.</summary>
		private static string ContestNote( ObjectiveMap objectives, Sq square )
		{
			var map = objectives ?? ObjectiveMap.Empty;
			int contest = map.Contest( square );
			if ( contest >= 2 ) return ", standing on an objective";
			if ( contest == 1 ) return ", beside an objective";
			return "";
		}

		/// <summary>Among positions that can attack, prefer the easiest shot, then the least movement.</summary>
		private static (Sq square, int moveCost, AttackAssessment attack) ChooseSpot(
			List<(Sq square, int moveCost, AttackAssessment attack)> spots,
			ObjectiveMap objectives = null )
		{
			var map = objectives ?? ObjectiveMap.Empty;
			// The shot and the movement still decide it. Contesting an
			// objective only separates squares that were otherwise equal, and
			// used to be separated by column and then row -- that is, by
			// nothing. The final two keys stay so the choice is deterministic.
			return spots
				.OrderBy( s => s.attack.RequiredAccuracy )
				.ThenBy( s => s.moveCost )
				.ThenByDescending( s => map.Contest( s.square ) )
				.ThenBy( s => s.square.C )
				.ThenBy( s => s.square.R )
				.First();
		}

		/// <summary>
		/// Walk the card's lines in order and carry out the first the board can.
		/// </summary>
		/// <remarks>
		/// Each kind is judged by whether it is ACHIEVABLE on this board, which
		/// is what the players are doing at the table when they read "Pounce 6
		/// on Jyn" and decide whether the Nexu can. A movement line is always
		/// achievable, since standing still is a legal move; an attack line is
		/// achievable only if a firing position exists; a placement line only
		/// if an empty legal square within reach is adjacent to what it asks
		/// for. Lines the board cannot judge are skipped and said so.
		/// </remarks>
		private static bool TryIntents(
			BoardModel board, FigurePlan fp, EnemyFigure fig, TargetCandidate groupTarget,
			List<TargetCandidate> allTargets, MoveOptions opt, Func<Sq, bool> canEnd,
			HashSet<Sq> blockers, Func<TargetCandidate, FigureVisibility> visOf,
			ObjectiveMap objectives, List<ActivationIntent> intents )
		{
			var map = objectives ?? ObjectiveMap.Empty;
			foreach ( var intent in intents )
			{
				if ( intent.Kind == IntentKind.Passive ) continue;
				if ( intent.Kind == IntentKind.Unparsed )
				{
					fp.Trace.Add( $"line {intent.Line + 1} is for the players to judge: "
						+ Instructions.Plain( intent.Raw ) );
					continue;
				}

				// "A Stunned figure must spend one action to remove the
				// condition", so it has one left. A line costing two is out
				// of reach, and a move-then-attack line can only be done if
				// the attack works from where the figure already stands.
				bool oneAction = fp.ActionsAvailable < 2;
				if ( oneAction && intent.Actions >= 2 )
				{
					fp.Trace.Add( $"line {intent.Line + 1} needs {intent.Actions} actions; "
						+ "Stunned leaves one" );
					continue;
				}

				switch ( intent.Kind )
				{
					case IntentKind.AttackOnly:
					{
						var here = AttackEvaluator.Assess( board, fig.Position, groupTarget.Position,
							fig.AttackKind, blockers, visOf( groupTarget ), fig.HasReach );
						if ( !here.CanDeclare )
						{
							fp.Trace.Add( $"line {intent.Line + 1} (attack): cannot attack "
								+ $"{groupTarget.Name} from here -- {here.Reason}" );
							continue;
						}
						fp.Path = new List<Sq> { fp.Start };
						fp.Attack = here;
						fp.Trace.Add( $"line {intent.Line + 1}: attacks {groupTarget.Name} without moving" );
						return true;
					}

					case IntentKind.MoveAttack:
					case IntentKind.Engage:
					{
						var reach = Pathfinder.Compute( board, fig.Position, intent.Move, opt );
						if ( intent.Kind == IntentKind.Engage && intent.Minimum > 1 )
						{
							// End adjacent to as many Rebels as possible, at
							// least the minimum. Then attack whoever it can.
							var best = reach.EndSquares( canEnd )
								.Select( sq => (sq, n: allTargets.Count( t => t.InPlay
									&& board.AreAdjacent( sq, t.Position ) )) )
								.Where( x => x.n >= intent.Minimum )
								.OrderByDescending( x => x.n )
								.ThenBy( x => reach.CostTo( x.sq ) )
								.ThenBy( x => x.sq.C ).ThenBy( x => x.sq.R )
								.FirstOrDefault();
							if ( best.n < intent.Minimum || (oneAction && best.sq != fig.Position) )
							{
								fp.Trace.Add( $"line {intent.Line + 1} (engage): no square within "
									+ $"{intent.Move} is adjacent to {intent.Minimum} Rebels"
									+ (oneAction ? " without moving (Stunned)" : "") );
								continue;
							}
							var atk = allTargets.Where( t => t.InPlay )
								.Select( t => (t, a: AttackEvaluator.Assess( board, best.sq, t.Position,
									fig.AttackKind, blockers, visOf( t ), fig.HasReach )) )
								.FirstOrDefault( x => x.a.CanDeclare );
							Commit( fp, reach, best.sq, reach.CostTo( best.sq ), atk.a );
							if ( atk.t != null ) fp.Target = atk.t;
							fp.Trace.Add( $"line {intent.Line + 1}: moves {reach.CostTo( best.sq )} to engage "
								+ $"{best.n} Rebels" + (atk.t != null ? ", attacks " + atk.t.Name : "") );
							return true;
						}

						var spots = AttackEvaluator.FiringPositions( board, reach, groupTarget.Position,
							fig.AttackKind, canEnd, blockers, visOf( groupTarget ), fig.HasReach );
						if ( oneAction )
						{
							// Moving and attacking are two actions; with one left
							// only a shot from the current square counts.
							spots = spots.Where( sp => sp.moveCost == 0 ).ToList();
							if ( spots.Count == 0 )
							{
								fp.Trace.Add( $"line {intent.Line + 1}: Stunned, and cannot attack "
									+ $"{groupTarget.Name} without moving" );
								continue;
							}
						}
						if ( spots.Count == 0 )
						{
							fp.Trace.Add( $"line {intent.Line + 1} (move {intent.Move} to attack): "
								+ $"no square within {intent.Move} can attack {groupTarget.Name}" );
							continue;
						}
						var spot = ChooseSpot( spots, map );
						Commit( fp, reach, spot.square, spot.moveCost, spot.attack );
						fp.Trace.Add( $"line {intent.Line + 1}: moves {spot.moveCost} of {intent.Move} "
							+ $"and attacks {groupTarget.Name} from {spot.square}"
							+ ContestNote( map, spot.square ) );
						return true;
					}

					case IntentKind.PlaceAdjacentAttack:
					{
						// "Place" ignores movement: any empty legal square within
						// N COUNTED spaces qualifies, terrain and cost aside. The
						// figure must end adjacent to the target (or to the
						// stated number of Rebels), and the base must fit.
						var candidates = new List<(Sq sq, int d, int n)>();
						foreach ( var sq in board.Squares )
						{
							if ( !canEnd( sq ) ) continue;
							if ( !FootprintFits( board, sq, fig.Footprint ) ) continue;
							int d = Distance.Count( board, fig.Position, sq, intent.Within );
							if ( d < 0 || d > intent.Within ) continue;
							int n = allTargets.Count( t => t.InPlay && board.AreAdjacent( sq, t.Position ) );
							bool ok = intent.Minimum > 1
								? n >= intent.Minimum
								: board.AreAdjacent( sq, groupTarget.Position );
							if ( ok ) candidates.Add( (sq, d, n) );
						}
						if ( candidates.Count == 0 )
						{
							fp.Trace.Add( $"line {intent.Line + 1} (place within {intent.Within}): "
								+ "no empty space within reach is adjacent to "
								+ (intent.Minimum > 1 ? intent.Minimum + " Rebels" : groupTarget.Name) );
							continue;
						}
						var pick = candidates
							.Select( c => (c, a: AttackEvaluator.Assess( board, c.sq, groupTarget.Position,
								fig.AttackKind, blockers, visOf( groupTarget ), fig.HasReach )) )
							.OrderByDescending( x => x.a.CanDeclare )
							.ThenByDescending( x => x.c.n )
							.ThenBy( x => x.c.d )
							.ThenBy( x => x.c.sq.C ).ThenBy( x => x.c.sq.R )
							.First();

						// A placement is not a walk: the path is start and end,
						// and the token jumps rather than slides.
						fp.End = pick.c.sq;
						fp.Moved = pick.c.sq != fp.Start;
						fp.MovementSpent = 0;
						fp.Path = new List<Sq> { fp.Start, pick.c.sq };
						fp.Attack = pick.a.CanDeclare ? pick.a : null;
						fp.Trace.Add( $"line {intent.Line + 1}: PLACED at {pick.c.sq}, {pick.c.d} spaces "
							+ $"away, adjacent to {pick.c.n} Rebel(s)"
							+ (pick.a.CanDeclare ? ", attacks " + groupTarget.Name : ", no attack from there") );
						return true;
					}

					case IntentKind.MoveToward:
					case IntentKind.Reposition:
					{
						var reach = Pathfinder.Compute( board, fig.Position, intent.Move, opt );
						var goal = groupTarget.Position;
						bool countable = Distance.Count( board, fig.Position, goal ) >= 0;
						Func<Sq, int> progress = sq =>
						{
							if ( countable )
							{
								int d = Distance.Count( board, sq, goal );
								if ( d >= 0 ) return d;
							}
							return Sq.Chebyshev( sq, goal );
						};

						// Reposition M: anything within M of the target is as
						// good as anything else within M, so the cheaper and the
						// objective-contesting square wins among those.
						int within = intent.Kind == IntentKind.Reposition ? intent.Within : 0;
						Func<Sq, int> score = sq => within > 0 ? Math.Max( progress( sq ), within ) : progress( sq );

						Sq bestSq = fig.Position;
						int bestScore = score( fig.Position );
						int bestCost = 0;
						int bestContest = map.Contest( fig.Position );
						foreach ( var sq in reach.EndSquares( canEnd ) )
						{
							int sc = score( sq ), cost = reach.CostTo( sq ), contest = map.Contest( sq );
							bool better = sc < bestScore
								|| (sc == bestScore && contest > bestContest)
								|| (sc == bestScore && contest == bestContest && cost < bestCost);
							if ( !better ) continue;
							bestSq = sq; bestScore = sc; bestCost = cost; bestContest = contest;
						}
						Commit( fp, reach, bestSq, bestCost, null );
						fp.Trace.Add( $"line {intent.Line + 1}: moves {bestCost} of {intent.Move} "
							+ (intent.Kind == IntentKind.Reposition
								? $"to reposition {intent.Within}, ending {progress( bestSq )} from {groupTarget.Name}"
								: $"toward {groupTarget.Name}, ending {progress( bestSq )} away")
							+ ContestNote( map, bestSq ) );
						return true;
					}
				}
			}

			fp.Trace.Add( "no line on the card could be carried out on the board; using the generic plan" );
			return false;
		}

		private static bool FootprintFits( BoardModel board, Sq anchor, Footprint footprint )
		{
			foreach ( var cell in Pathfinder.Cells( new MoveState( anchor, Facing.NorthSouth ), footprint ) )
				if ( !board.IsEnterable( cell ) ) return false;
			return true;
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
