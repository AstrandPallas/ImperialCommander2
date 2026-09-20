using System;
using System.Collections.Generic;
using System.Linq;
using Saga.Tracking;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>Randomised adversarial testing of the planner.</summary>
	public static class PlannerFuzzTests
	{
		private sealed class Rng
		{
			private uint _s;
			public Rng( uint seed ) { _s = seed == 0 ? 1u : seed; }
			public uint Next()
			{
				// xorshift32: deterministic across platforms, unlike Random.
				_s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5;
				return _s;
			}
			public int Range( int lo, int hi ) => lo + (int)(Next() % (uint)Math.Max( 1, hi - lo ));
			public bool Chance( int percent ) => Next() % 100 < (uint)percent;
			public T Pick<T>( IList<T> xs ) => xs[Range( 0, xs.Count )];
		}

		private static BoardModel RandomBoard( Rng r, int w, int h )
		{
			var b = new BoardModel();
			for ( int c = 0; c < w; c++ )
			{
				for ( int row = 0; row < h; row++ )
				{
					var flags = SquareFlags.Normal;
					int roll = r.Range( 0, 100 );
					if ( roll < 12 ) flags = SquareFlags.Difficult;
					else if ( roll < 18 ) flags = SquareFlags.Blocking;
					else if ( roll < 22 ) flags = SquareFlags.Impassable;
					b.SetSquare( new Sq( c, row ), flags );
				}
			}

			// Interior edges, including the awkward ones.
			for ( int c = 0; c < w; c++ )
			{
				for ( int row = 0; row < h; row++ )
				{
					if ( row > 0 && r.Chance( 14 ) )
						b.SetEdge( new Sq( c, row ), EdgeDir.N, RandomEdge( r ) );
					if ( c > 0 && r.Chance( 14 ) )
						b.SetEdge( new Sq( c, row ), EdgeDir.W, RandomEdge( r ) );
				}
			}
			return b;
		}

		private static EdgeType RandomEdge( Rng r )
		{
			switch ( r.Range( 0, 5 ) )
			{
				case 0: return EdgeType.Wall;
				case 1: return EdgeType.Blocking;
				case 2: return EdgeType.Impassable;
				case 3: return EdgeType.DoorClosed;
				default: return EdgeType.DoorOpen;
			}
		}

		/// <summary>Re-derive what the plan claims, from the board alone.</summary>
		private static void AssertPlanIsLegal( BoardModel board, ActivationPlan plan,
			List<EnemyFigure> group, List<TargetCandidate> rebels, uint seed )
		{
			var problems = PlanLegality.Violations( board, plan, group, rebels, $"seed {seed}" );
			True( problems.Count == 0, string.Join( "; ", problems ) );
		}

		private static (BoardModel, List<EnemyFigure>, List<TargetCandidate>) Scenario( uint seed )
		{
			var r = new Rng( seed );
			int w = r.Range( 4, 11 );
			int h = r.Range( 4, 11 );
			var board = RandomBoard( r, w, h );

			var free = board.Squares.Where( board.IsEnterable )
				.OrderBy( s => s.C ).ThenBy( s => s.R ).ToList();
			var enemies = new List<EnemyFigure>();
			var rebels = new List<TargetCandidate>();
			if ( free.Count < 4 ) return (board, enemies, rebels);

			var taken = new HashSet<Sq>();

			bool Fits( Sq s, Footprint f )
				=> Pathfinder.Cells( new MoveState( s, Facing.NorthSouth ), f )
					.All( c => board.Exists( c ) && board.IsEnterable( c ) && !taken.Contains( c ) );

			// Same geometry check, ignoring occupancy -- used to confirm a
			// footprint still fits at a square already reserved for this figure.
			bool Fits2( Sq s, Footprint f )
				=> Pathfinder.Cells( new MoveState( s, Facing.NorthSouth ), f )
					.All( c => board.Exists( c ) && board.IsEnterable( c ) );

			Sq? Take( Footprint f = Footprint.Small1x1 )
			{
				var candidates = free.Where( s => Fits( s, f ) ).ToList();
				if ( candidates.Count == 0 ) return null;
				var chosen = r.Pick( candidates );
				foreach ( var c in Pathfinder.Cells( new MoveState( chosen, Facing.NorthSouth ), f ) )
					taken.Add( c );
				return chosen;
			}

			int nEnemies = r.Range( 1, 4 );
			for ( int i = 0; i < nEnemies && taken.Count < free.Count; i++ )
			{
				var foot = r.Chance( 30 ) ? (Footprint)r.Range( 1, 4 ) : Footprint.Small1x1;
				var at = Take( foot ) ?? Take( Footprint.Small1x1 );
				if ( at == null ) continue;
				if ( !Fits2( at.Value, foot ) ) foot = Footprint.Small1x1;
				enemies.Add( new EnemyFigure
				{
					Id = "E" + i,
					Name = "Enemy" + i,
					Position = at.Value,
					Speed = r.Range( 2, 6 ),
					AttackKind = r.Chance( 50 ) ? AttackKind.Melee : AttackKind.Ranged,
					Stunned = r.Chance( 20 ),
					Footprint = foot,
					Massive = r.Chance( 8 ),
				} );
			}

			int nRebels = r.Range( 1, 5 );
			for ( int i = 0; i < nRebels && taken.Count < free.Count; i++ )
			{
				var spot = Take();
				if ( spot == null ) break;
				int max = r.Range( 6, 14 );
				rebels.Add( new TargetCandidate
				{
					Id = "H" + i,
					Name = "Hero" + i,
					Position = spot.Value,
					MaxHealth = max,
					Damage = r.Range( 0, max ),
					IsWounded = r.Chance( 25 ),
					InPlay = !r.Chance( 10 ),
				} );
			}
			return (board, enemies, rebels);
		}

		public static void Register()
		{
			Suite( "planner fuzzing" );

			Test( "every order the AI produces is legal, across 400 random boards", () =>
			{
				int planned = 0, attacked = 0, moved = 0;
				int large = 0, stunned = 0, massive = 0, melee = 0, tied = 0;
				for ( uint seed = 1; seed <= 400; seed++ )
				{
					var (board, enemies, rebels) = Scenario( seed );
					if ( enemies.Count == 0 || !rebels.Any( x => x.InPlay ) ) continue;

					var plan = ActivationPlanner.Plan( board, enemies, rebels );
					AssertPlanIsLegal( board, plan, enemies, rebels, seed );

					planned += plan.Figures.Count;
					attacked += plan.Figures.Count( f => f.WillAttack );
					moved += plan.Figures.Count( f => f.Moved );
					large += enemies.Count( e => e.Footprint != Footprint.Small1x1 );
					stunned += enemies.Count( e => e.Stunned );
					massive += enemies.Count( e => e.Massive );
					melee += enemies.Count( e => e.AttackKind == AttackKind.Melee );
					if ( plan.NeedsPlayerDecision ) tied++;
				}

				True( planned > 500, $"exercised enough figures ({planned})" );
				True( attacked > 50, $"enough found attacks ({attacked})" );
				True( moved > 50, $"enough actually moved ({moved})" );
				True( large > 40, $"enough large figures generated ({large})" );
				True( stunned > 40, $"enough Stunned figures ({stunned})" );
				True( massive > 5, $"some Massive figures ({massive})" );
				True( melee > 100, $"a healthy mix of melee ({melee})" );
				True( tied > 0, $"at least one unresolvable target tie ({tied})" );

				Console.WriteLine( $"        coverage: {planned} figures, {attacked} attacks, "
					+ $"{moved} moves, {large} large, {stunned} stunned, {massive} massive, "
					+ $"{melee} melee, {tied} ties" );
			} );

			Test( "planning is deterministic for a given board", () =>
			{
				// Without this, nothing found by fuzzing can be reproduced, and
				// the replay log cannot diff AI decisions across engine changes.
				for ( uint seed = 1; seed <= 60; seed++ )
				{
					var (board, enemies, rebels) = Scenario( seed );
					if ( enemies.Count == 0 || !rebels.Any( x => x.InPlay ) ) continue;

					var a = ActivationPlanner.Plan( board, enemies, rebels );
					var (board2, enemies2, rebels2) = Scenario( seed );
					var b = ActivationPlanner.Plan( board2, enemies2, rebels2 );

					Eq( a.Figures.Count, b.Figures.Count, $"seed {seed}: figure count" );
					for ( int i = 0; i < a.Figures.Count; i++ )
					{
						Eq( a.Figures[i].End, b.Figures[i].End, $"seed {seed}: end square" );
						Eq( a.Figures[i].WillAttack, b.Figures[i].WillAttack, $"seed {seed}: attack" );
						Eq( a.Figures[i].MovementSpent, b.Figures[i].MovementSpent,
							$"seed {seed}: movement" );
					}
				}
			} );

			Test( "the AI never targets a withdrawn or defeated hero", () =>
			{
				for ( uint seed = 500; seed <= 640; seed++ )
				{
					var (board, enemies, rebels) = Scenario( seed );
					if ( enemies.Count == 0 ) continue;
					// Force most rebels out of play.
					for ( int i = 1; i < rebels.Count; i++ ) rebels[i].InPlay = false;
					if ( !rebels.Any( x => x.InPlay ) ) continue;

					var plan = ActivationPlanner.Plan( board, enemies, rebels );
					if ( plan.GroupTarget?.Chosen != null )
						True( plan.GroupTarget.Chosen.InPlay, $"seed {seed}: target must be in play" );
					foreach ( var fp in plan.Figures.Where( f => f.Target != null ) )
						True( fp.Target.InPlay, $"seed {seed}: per-figure target must be in play" );
				}
			} );

			Test( "tracking survives a randomised mission without going inconsistent", () =>
			{
				// Damage, conditions and deaths applied in random order, checking
				// the group state can never describe something impossible.
				var r = new Rng( 99 );
				for ( int trial = 0; trial < 300; trial++ )
				{
					int figures = r.Range( 1, 5 );
					int hp = r.Range( 1, 7 );
					var g = GroupCombatState.Create( "g", "DG", "Group", figures, hp );

					for ( int step = 0; step < 30 && !g.IsDefeated; step++ )
					{
						switch ( r.Range( 0, 5 ) )
						{
							case 0: g.ApplyDamage( r.Range( 1, 9 ) ); break;
							case 1: g.Heal( r.Range( 1, 4 ) ); break;
							case 2: g.AddCondition( (Condition)r.Range( 0, 5 ) ); break;
							case 3: g.ResolveAfterAction(); break;
							default: g.SetEngaged( r.Range( 0, figures ) ); break;
						}

						True( g.FiguresAlive >= 0 && g.FiguresAlive <= g.MaxFigures,
							$"trial {trial}: alive count out of range ({g.FiguresAlive})" );
						True( g.CurrentFigureDamage >= 0, $"trial {trial}: negative damage" );
						True( g.EngagedRemaining >= 0 && g.EngagedRemaining <= g.PerFigureHealth,
							$"trial {trial}: engaged health out of range" );
						Eq( g.FiguresAlive, g.Figures.Count( f => f.Alive ),
							$"trial {trial}: alive count disagrees with the figure slots" );
						if ( !g.IsDefeated )
							True( g.Figures.Any( f => f.Alive && f.Index == g.EngagedFigureIndex ),
								$"trial {trial}: engaged figure is not alive" );
					}
				}
			} );
		}
	}
}
