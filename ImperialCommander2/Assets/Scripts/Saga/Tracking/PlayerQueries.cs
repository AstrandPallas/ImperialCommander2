using System.Collections.Generic;
using System.Linq;
using Saga.Board;

namespace Saga.Tracking
{
	/// <summary>One enemy figure that could attack a given space this round.</summary>
	public sealed class Threat
	{
		public string GroupInstanceId;
		public string FigureId;
		public string CardName;

		/// <summary>Where the figure stands now.</summary>
		public Sq From;

		/// <summary>The cheapest space it could attack from.</summary>
		public Sq AttacksFrom;

		/// <summary>Movement points it would spend to get there.</summary>
		public int MoveCost;

		/// <summary>True when it can attack without moving at all.</summary>
		public bool AlreadyInPosition => MoveCost == 0;

		public AttackKind Kind;

		/// <summary>Accuracy the attack would have to make.</summary>
		public int RequiredAccuracy;

		public override string ToString()
			=> AlreadyInPosition
				? $"{CardName} can attack from where it stands ({From})"
				: $"{CardName} can attack after moving {MoveCost} to {AttacksFrom}";
	}

	/// <summary>
	/// Questions the PLAYERS ask the board, as opposed to the orders the app
	/// gives the Imperial side.
	/// </summary>
	/// <remarks>
	/// These are the arguments that actually stop play at a table -- "can I see
	/// it from here", "how far can I get", "if I stand there, who shoots me" --
	/// and the app already holds an exact answer to all three. It is the same
	/// engine the AI uses, so a query and an order can never disagree.
	///
	/// Everything here is advisory and read-only. Nothing in this file changes
	/// tracked state.
	/// </remarks>
	public static class PlayerQueries
	{
		/// <summary>Squares occupied by figures, which block line of sight.</summary>
		public static List<FigureVisibility> FiguresOnBoard(
			IEnumerable<HeroCombatState> heroes, IEnumerable<GroupCombatState> groups )
		{
			var all = new List<FigureVisibility>();

			foreach ( var hero in heroes ?? Enumerable.Empty<HeroCombatState>() )
			{
				if ( hero == null || !hero.InPlay || hero.PosC == null || hero.PosR == null )
					continue;
				all.Add( new FigureVisibility
				{
					Id = hero.CardId,
					Position = new Sq( hero.PosC.Value, hero.PosR.Value ),
				} );
			}

			foreach ( var group in groups ?? Enumerable.Empty<GroupCombatState>() )
			{
				if ( group == null ) continue;
				var profile = group.Profile ?? UnitProfile.Default;
				foreach ( var slot in group.Figures )
				{
					if ( !slot.Alive || !slot.HasPosition ) continue;
					all.Add( new FigureVisibility
					{
						Id = TrackerBridge.FigureId( group.InstanceId, slot.Index ),
						Position = new Sq( slot.PosC.Value, slot.PosR.Value ),
						Massive = profile.Massive,
						Mobile = profile.Mobile,
					} );
				}
			}

			return all;
		}

		/// <summary>
		/// Can a figure standing on one square see another square?
		/// </summary>
		/// <remarks>
		/// Both endpoints are excluded from the blocker set, so neither the
		/// asker nor the thing being looked at screens the line.
		/// </remarks>
		public static bool CanSee( BoardModel board, Sq from, Sq to,
			IEnumerable<FigureVisibility> figures = null )
		{
			if ( board == null ) return false;
			var blockers = Visibility.BlockersFor( board, from, figures, to );
			return LineOfSight.HasLos( board, from, to, blockers.Contains );
		}

		/// <summary>Spaces counted between two squares, or -1 when they cannot be counted.</summary>
		public static int SpacesBetween( BoardModel board, Sq from, Sq to )
			=> board == null ? -1 : Distance.Count( board, from, to );

		/// <summary>
		/// Every square a hero could reach, with what it would cost.
		/// </summary>
		/// <remarks>
		/// Other figures are pathed through but not finished on, which is the
		/// ordinary rule, so the answer is where the hero may actually END UP
		/// rather than merely where it could walk.
		/// </remarks>
		public static List<(Sq square, int cost)> ReachableBy(
			BoardModel board, HeroCombatState hero, int moveActions,
			IEnumerable<HeroCombatState> otherHeroes = null,
			IEnumerable<GroupCombatState> groups = null )
		{
			var result = new List<(Sq, int)>();
			if ( board == null || hero == null || hero.PosC == null || hero.PosR == null )
				return result;

			var start = new Sq( hero.PosC.Value, hero.PosR.Value );
			var opt = OptionsAround( hero, otherHeroes, groups );
			var reach = Pathfinder.Compute( board, start, System.Math.Max( 0, moveActions ) * hero.Speed, opt );
			var canEnd = Pathfinder.CanEndOn( board, opt );

			var seen = new HashSet<Sq>();
			foreach ( var kv in reach.Cost )
			{
				var s = kv.Key.Anchor;
				if ( !seen.Add( s ) ) continue;
				if ( s != start && !canEnd( s ) ) continue;
				result.Add( (s, reach.CostTo( s )) );
			}

			result.Sort( ( a, b ) =>
			{
				int c = a.Item2.CompareTo( b.Item2 );
				if ( c != 0 ) return c;
				c = a.Item1.C.CompareTo( b.Item1.C );
				return c != 0 ? c : a.Item1.R.CompareTo( b.Item1.R );
			} );
			return result;
		}

		private static MoveOptions OptionsAround( HeroCombatState hero,
			IEnumerable<HeroCombatState> otherHeroes, IEnumerable<GroupCombatState> groups )
		{
			var friendly = new HashSet<Sq>();
			foreach ( var h in otherHeroes ?? Enumerable.Empty<HeroCombatState>() )
			{
				if ( h == null || h == hero || !h.InPlay ) continue;
				if ( h.PosC == null || h.PosR == null ) continue;
				friendly.Add( new Sq( h.PosC.Value, h.PosR.Value ) );
			}

			var hostile = new HashSet<Sq>();
			foreach ( var g in groups ?? Enumerable.Empty<GroupCombatState>() )
			{
				if ( g == null ) continue;
				foreach ( var slot in g.Figures )
				{
					if ( !slot.Alive || !slot.HasPosition ) continue;
					hostile.Add( new Sq( slot.PosC.Value, slot.PosR.Value ) );
				}
			}

			return new MoveOptions { Friendly = friendly.Contains, Hostile = hostile.Contains };
		}

		/// <summary>
		/// Which enemy figures could attack a given square this round, and what
		/// each would have to spend to do it.
		/// </summary>
		/// <remarks>
		/// This is the question worth answering before a hero commits to a
		/// square, and the one players are worst at eyeballing, because it
		/// compounds every enemy's speed with terrain and line of sight at
		/// once. An enemy is given one move action and one attack, matching
		/// what the planner assumes, so the answer is "this round", not "ever".
		///
		/// Enemy figures are NOT treated as blocking each other's line of
		/// sight to the square, because they activate one group at a time and
		/// may move out of each other's way first. Erring toward reporting a
		/// threat is the safe direction: the cost of a missed threat is a dead
		/// hero, the cost of a spurious one is a more cautious move.
		/// </remarks>
		public static List<Threat> ThreatsTo( BoardModel board, Sq square,
			IEnumerable<GroupCombatState> groups,
			IEnumerable<HeroCombatState> heroes = null )
		{
			var threats = new List<Threat>();
			if ( board == null || !board.Exists( square ) ) return threats;

			// Only the heroes screen the shot; see the remarks above.
			var screens = new List<FigureVisibility>();
			foreach ( var h in heroes ?? Enumerable.Empty<HeroCombatState>() )
			{
				if ( h == null || !h.InPlay || h.PosC == null || h.PosR == null ) continue;
				var pos = new Sq( h.PosC.Value, h.PosR.Value );
				if ( pos == square ) continue;
				screens.Add( new FigureVisibility { Id = h.CardId, Position = pos } );
			}
			var blockers = Visibility.BlockersFor( board, square, screens, square );

			foreach ( var group in groups ?? Enumerable.Empty<GroupCombatState>() )
			{
				if ( group == null || group.IsDefeated ) continue;
				var profile = group.Profile ?? UnitProfile.Default;

				// A Stunned group spends one of its two actions clearing the
				// condition, so it has nothing left to move with.
				int actions = group.Has( Condition.Stunned ) ? 1 : 2;
				int budget = System.Math.Max( 0, actions - 1 ) * profile.Speed;

				foreach ( var slot in group.Figures )
				{
					if ( !slot.Alive || !slot.HasPosition ) continue;
					var from = new Sq( slot.PosC.Value, slot.PosR.Value );

					var opt = new MoveOptions
					{
						Footprint = profile.Footprint,
						Massive = profile.Massive,
						Mobile = profile.Mobile,
					};
					var reach = Pathfinder.Compute( board, from, budget, opt );
					var canEnd = Pathfinder.CanEndOn( board, opt );

					var spots = AttackEvaluator.FiringPositions( board, reach, square,
						profile.AttackKind, canEnd, blockers, null, profile.HasReach );
					if ( spots.Count == 0 ) continue;

					var best = spots[0];
					threats.Add( new Threat
					{
						GroupInstanceId = group.InstanceId,
						FigureId = TrackerBridge.FigureId( group.InstanceId, slot.Index ),
						CardName = group.CardName,
						From = from,
						AttacksFrom = best.square,
						MoveCost = best.moveCost,
						Kind = profile.AttackKind,
						RequiredAccuracy = best.attack.RequiredAccuracy,
					} );
				}
			}

			threats.Sort( ( a, b ) => a.MoveCost.CompareTo( b.MoveCost ) );
			return threats;
		}

		/// <summary>
		/// The same question asked of every square a hero could move to, so the
		/// safest destination can be picked out.
		/// </summary>
		public static List<(Sq square, int cost, int threatCount)> ThreatMapFor(
			BoardModel board, HeroCombatState hero, int moveActions,
			IEnumerable<GroupCombatState> groups,
			IEnumerable<HeroCombatState> otherHeroes = null )
		{
			var map = new List<(Sq, int, int)>();
			var groupList = groups?.ToList();
			foreach ( var (square, cost) in ReachableBy( board, hero, moveActions, otherHeroes, groupList ) )
				map.Add( (square, cost, ThreatsTo( board, square, groupList, otherHeroes ).Count) );
			return map;
		}
	}
}
