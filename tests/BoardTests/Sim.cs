using System;
using System.Collections.Generic;
using System.Linq;

namespace Saga.Board.Tests
{
	/// <summary>Plays a shipped mission out headlessly, round after round, and prints every AI decision in a form that can be judged against the rulebook.</summary>
	public static class Sim
	{
		/// <summary>How a simulated hero decides where to go.</summary>
		public enum Policy
		{
			/// <summary>Push toward the objective, ignoring danger.</summary>
			Advance,
			/// <summary>Close on the nearest enemy figure.</summary>
			Engage,
			/// <summary>Stand still and shoot.</summary>
			Hold,
			/// <summary>Close while healthy, back off once hurt.</summary>
			Skirmish,
		}

		public sealed class SimHero
		{
			public TargetCandidate Card;
			public Policy Policy;
			public int Speed = 4;
		}

		private sealed class Rng
		{
			private uint _s;
			public Rng( uint seed ) { _s = seed == 0 ? 1u : seed; }
			public uint Next() { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return _s; }
			public int Range( int lo, int hi ) => lo + (int)(Next() % (uint)Math.Max( 1, hi - lo ));
		}

		/// <summary>Set while a run is being used as a regression gate rather than read by a person.</summary>
		private static bool _quiet;

		private static void Say( string line )
		{
			if ( !_quiet ) Console.WriteLine( line );
		}

		private static void Say()
		{
			if ( !_quiet ) Console.WriteLine();
		}

		/// <summary>The shipped maps this harness can play.</summary>
		public static readonly string[] Missions =
		{
			"CORE1", "CORE2", "CORE8",
			"TWIN1", "TWIN3",
			"BESPIN1",
			"HOTH1", "HOTH8",
			"LOTHAL2",
			"EMPIRE1",
			"JABBA1",
		};

		public static int Run( uint seed = 1, int rounds = 6, bool quiet = false,
			string mission = "CORE1" )
		{
			_quiet = quiet;
			var built = Build( mission );
			var board = built.Board;
			var rng = new Rng( seed );

			var open = board.Squares.Where( board.IsEnterable ).ToList();
			if ( open.Count < 16 )
			{
				Say( "board too small to simulate" );
				return 0;
			}

			var west = open.OrderBy( s => s.C ).ThenBy( s => s.R ).ToList();
			var east = open.OrderByDescending( s => s.C ).ThenBy( s => s.R ).ToList();
			var objective = east[0];

			var heroes = new List<SimHero>
			{
				Hero( "H1", "Diala", west[0], 10, Policy.Engage, 4 ),
				Hero( "H2", "Gaarkhan", west[3], 12, Policy.Advance, 4 ),
				Hero( "H3", "Fenn", west[6], 10, Policy.Skirmish, 4 ),
				Hero( "H4", "Mak", west[9], 8, Policy.Hold, 5 ),
			};
			var enemies = new List<EnemyFigure>
			{
				new EnemyFigure { Id = "E1", Name = "Stormtrooper 1", Position = east[1],
					Speed = 4, AttackKind = AttackKind.Ranged },
				new EnemyFigure { Id = "E2", Name = "Stormtrooper 2", Position = east[2],
					Speed = 4, AttackKind = AttackKind.Ranged },
				new EnemyFigure { Id = "E3", Name = "Trandoshan", Position = east[4],
					Speed = 5, AttackKind = AttackKind.Melee },
			};

			int difficult = board.Squares.Count(
				s => (board.Flags( s ) & SquareFlags.Difficult) != 0 );
			int blocking = board.Squares.Count(
				s => (board.Flags( s ) & SquareFlags.Blocking) != 0 );

			Say( new string( '=', 74 ) );
			Say( "SIMULATED PLAY  mission " + mission + "  seed " + seed
				+ "  " + rounds + " rounds" );
			Say( "board: " + board.Count + " squares, " + difficult
				+ " difficult, " + blocking + " blocking" );
			Say( "objective: " + objective );
			Say( new string( '=', 74 ) );

			int violations = 0;
			for ( int round = 1; round <= rounds; round++ )
			{
				Say();
				Say( "----- ROUND " + round + " " + new string( '-', 54 ) );

				MoveHeroes( board, heroes, enemies, objective );

				var live = heroes.Where( h => h.Card.InPlay ).Select( h => h.Card ).ToList();
				if ( live.Count == 0 )
				{
					Say( "no Rebels left in play" );
					break;
				}

				var plan = ActivationPlanner.Plan( board, enemies, live, null, null );
				violations += Report( board, plan, enemies, live, heroes, round, seed );
				Apply( plan, enemies, heroes, rng );
			}

			Say();
			Say( new string( '=', 74 ) );
			Say( violations == 0
				? "LEGALITY: no violations across the run"
				: "LEGALITY: " + violations + " violation(s) -- see above" );
			Say( "SENSE: the orders above still need judging by hand." );
			return violations;
		}

		private static BoardBuilder.BuildResult Build( string mission )
		{
			switch ( mission )
			{
				case "CORE2": return From( Core2Placements.Tiles, Core2Placements.Lookup,
					Core2Placements.Doors, Core2Placements.Library() );
				case "CORE8": return From( Core8Placements.Tiles, Core8Placements.Lookup,
					Core8Placements.Doors, Core8Placements.Library() );
				case "TWIN1": return From( Twin1Placements.Tiles, Twin1Placements.Lookup,
					Twin1Placements.Doors, Twin1Placements.Library() );
				case "TWIN3": return From( Twin3Placements.Tiles, Twin3Placements.Lookup,
					Twin3Placements.Doors, Twin3Placements.Library() );
				case "BESPIN1": return From( Bespin1Placements.Tiles, Bespin1Placements.Lookup,
					Bespin1Placements.Doors, Bespin1Placements.Library() );
				case "HOTH1": return From( Hoth1Placements.Tiles, Hoth1Placements.Lookup,
					Hoth1Placements.Doors, Hoth1Placements.Library() );
				case "HOTH8": return From( Hoth8Placements.Tiles, Hoth8Placements.Lookup,
					Hoth8Placements.Doors, Hoth8Placements.Library() );
				case "LOTHAL2": return From( Lothal2Placements.Tiles, Lothal2Placements.Lookup,
					Lothal2Placements.Doors, Lothal2Placements.Library() );
				case "EMPIRE1": return From( Empire1Placements.Tiles, Empire1Placements.Lookup,
					Empire1Placements.Doors, Empire1Placements.Library() );
				case "JABBA1": return From( Jabba1Placements.Tiles, Jabba1Placements.Lookup,
					Jabba1Placements.Doors, Jabba1Placements.Library() );
				default: return From( Core1Placements.Tiles, Core1Placements.Lookup,
					Core1Placements.Doors, Core1Placements.Library() );
			}
		}

		/// <summary>Doors are opened for simulation.</summary>
		private static BoardBuilder.BuildResult From( TilePlacement[] tiles,
			System.Func<string, string, (int, int)?> lookup, DoorPlacement[] doors,
			TerrainLibrary lib )
			=> BoardBuilder.Build( tiles, lookup,
				doors.Select( d => new DoorPlacement
				{ X = d.X, Y = d.Y, Rotation = d.Rotation, Open = true } ).ToArray(), lib );

		private static SimHero Hero( string id, string name, Sq at, int hp, Policy p, int speed )
		{
			return new SimHero
			{
				Policy = p,
				Speed = speed,
				Card = new TargetCandidate { Id = id, Name = name, Position = at, MaxHealth = hp },
			};
		}

		/// <summary>Move each hero by its policy.</summary>
		private static void MoveHeroes( BoardModel board, List<SimHero> heroes,
			List<EnemyFigure> enemies, Sq objective )
		{
			foreach ( var h in heroes.Where( x => x.Card.InPlay ) )
			{
				if ( h.Policy == Policy.Hold ) continue;

				var others = new HashSet<Sq>( heroes.Where( x => x != h && x.Card.InPlay )
					.Select( x => x.Card.Position ) );
				var foes = new HashSet<Sq>( enemies.Select( e => e.Position ) );
				var opt = new MoveOptions
				{
					Friendly = others.Contains,
					Hostile = foes.Contains,
				};
				var reach = Pathfinder.Compute( board, h.Card.Position, h.Speed, opt );
				var canEnd = Pathfinder.CanEndOn( board, opt );
				var ends = reach.EndSquares( s => canEnd( s ) && !others.Contains( s )
					&& !foes.Contains( s ) ).ToList();
				if ( ends.Count == 0 ) continue;

				bool hurt = h.Card.Damage * 2 >= h.Card.MaxHealth;
				Sq want;
				if ( h.Policy == Policy.Advance )
				{
					want = ends.OrderBy( s => Sq.Chebyshev( s, objective ) )
						.ThenBy( s => s.C ).ThenBy( s => s.R ).First();
				}
				else if ( h.Policy == Policy.Engage || !hurt )
				{
					want = ends.OrderBy( s => enemies.Min( e => Sq.Chebyshev( s, e.Position ) ) )
						.ThenBy( s => s.C ).ThenBy( s => s.R ).First();
				}
				else
				{
					want = ends.OrderByDescending(
							s => enemies.Min( e => Sq.Chebyshev( s, e.Position ) ) )
						.ThenBy( s => s.C ).ThenBy( s => s.R ).First();
				}
				h.Card.Position = want;
			}
		}

		private static int Report( BoardModel board, ActivationPlan plan,
			List<EnemyFigure> enemies, List<TargetCandidate> live, List<SimHero> heroes,
			int round, uint seed )
		{
			if ( plan.GroupTarget != null )
			{
				string chosen = plan.GroupTarget.Chosen == null
					? "(none)" : plan.GroupTarget.Chosen.Name;
				Say( "target: " + chosen + " by rule [" + plan.GroupTarget.Rule + "]"
					+ (plan.GroupTarget.NeedsPlayerDecision ? "  TIE - players decide" : "") );
				foreach ( var t in plan.GroupTarget.Trace ) Say( "   . " + t );
			}

			foreach ( var fp in plan.Figures )
			{
				Say();
				Say( "  " + fp );
				foreach ( var t in fp.Trace ) Say( "     . " + t );
				if ( fp.Path.Count > 1 )
				{
					// Replay the cost independently of the planner, so the
					// movement claim is checked rather than believed.
					int cost = 0;
					var notes = new List<string>();
					for ( int i = 1; i < fp.Path.Count; i++ )
					{
						int step = board.EnterCost( fp.Path[i] );
						cost += step;
						if ( step > 1 ) notes.Add( fp.Path[i] + " costs " + step );
					}
					Say( "     path: " + string.Join( " -> ", fp.Path ) );
					Say( "     replayed cost " + cost + ", plan claims "
						+ fp.MovementSpent + (cost == fp.MovementSpent ? "" : "   <-- MISMATCH") );
					foreach ( var n in notes ) Say( "     ! " + n );
				}
			}

			var figures = new Dictionary<Sq, char>();
			foreach ( var h in heroes.Where( x => x.Card.InPlay ) )
				figures[h.Card.Position] = h.Card.Name[0];
			foreach ( var e in enemies ) figures[e.Position] = 'x';
			Say();
			Say( BoardRenderer.Render( board, figures, null, default( Sq ) ) );

			var problems = PlanLegality.Violations( board, plan, enemies, live,
				"seed " + seed + " round " + round );
			foreach ( var p in problems ) Say( "  *** ILLEGAL: " + p );
			return problems.Count;
		}

		/// <summary>Commit the plan, then resolve its attacks.</summary>
		private static void Apply( ActivationPlan plan, List<EnemyFigure> enemies,
			List<SimHero> heroes, Rng rng )
		{
			foreach ( var fp in plan.Figures )
			{
				fp.Figure.Position = fp.End;
				if ( !fp.WillAttack || fp.Target == null ) continue;

				var hero = heroes.FirstOrDefault( h => h.Card.Id == fp.Target.Id );
				if ( hero == null ) continue;

				int damage = rng.Range( 1, 4 );
				hero.Card.Damage += damage;
				Say( "  -> " + fp.Figure.Name + " hits " + hero.Card.Name
					+ " for " + damage + " (" + hero.Card.Damage + "/"
					+ hero.Card.MaxHealth + ")" );

				if ( hero.Card.Damage < hero.Card.MaxHealth ) continue;

				if ( hero.Card.IsWounded )
				{
					hero.Card.InPlay = false;
					Say( "  -> " + hero.Card.Name + " is WITHDRAWN" );
				}
				else
				{
					hero.Card.IsWounded = true;
					hero.Card.Damage = 0;
					Say( "  -> " + hero.Card.Name + " is WOUNDED" );
				}
			}
		}
	}
}
