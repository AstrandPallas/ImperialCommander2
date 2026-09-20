using System;
using System.Collections.Generic;
using System.Linq;

namespace Saga.Board.Tests
{
	/// <summary>A realistic scenario rendered as an adjudicable record.</summary>
	public static class Scenario
	{
		public static void RunAll()
		{
			Corridor();
			RoomWithDoor();
			WoundedPriority();
			RealMission();
		}

		/// <summary>The AI on a real, fully authored shipped map.</summary>
		private static void RealMission()
		{
			Header( "CORE1, real map with authored terrain",
				"does the AI route sensibly around real difficult and blocking terrain?" );

			var built = BoardBuilder.Build(
				Core1Placements.Tiles, Core1Placements.Lookup,
				Core1Placements.Doors.Select( d => new DoorPlacement
				{ X = d.X, Y = d.Y, Rotation = d.Rotation, Open = true } ).ToArray(),
				Core1Placements.Library() );
			var board = built.Board;

			int difficult = board.Squares.Count(
				s => (board.Flags( s ) & SquareFlags.Difficult) != 0 );
			int blocking = board.Squares.Count(
				s => (board.Flags( s ) & SquareFlags.Blocking) != 0 );
			Console.WriteLine( $"board: {board.Count} squares, {difficult} difficult, "
				+ $"{blocking} blocking, {built.Overlaps.Count} overlaps" );

			// Put the enemy beside difficult terrain and the hero beyond it, so
			// the routing decision is forced rather than incidental.
			var diff = board.Squares
				.Where( s => (board.Flags( s ) & SquareFlags.Difficult) != 0 )
				.OrderBy( s => s.C ).ThenBy( s => s.R ).ToList();
			if ( diff.Count == 0 ) { Console.WriteLine( "(no difficult terrain found)" ); return; }

			var focus = diff[diff.Count / 2];
			var open = board.Squares.Where( board.IsEnterable ).ToList();
			var start = open.OrderBy( s => Sq.Chebyshev( s, focus ) )
				.ThenBy( s => s.C ).First( s => Sq.Chebyshev( s, focus ) >= 3 );
			var heroAt = open.OrderBy( s => Sq.Chebyshev( s, focus ) )
				.ThenByDescending( s => s.C )
				.First( s => Sq.Chebyshev( s, focus ) >= 2 && s != start );

			var enemies = new[]
			{
				new EnemyFigure { Id = "T1", Name = "Stormtrooper", Position = start,
					Speed = 4, AttackKind = AttackKind.Ranged },
			};
			var heroes = new[]
			{
				new TargetCandidate { Id = "H1", Name = "Diala", Position = heroAt,
					MaxHealth = 10 },
			};

			Console.WriteLine( $"trooper at {start}, hero at {heroAt}, "
				+ $"difficult terrain near {focus}" );

			var plan = ActivationPlanner.Plan( board, enemies, heroes );
			var fp = plan.Figures[0];
			Console.WriteLine();
			Console.WriteLine( fp );
			foreach ( var t in plan.GroupTarget.Trace ) Console.WriteLine( "   . " + t );
			foreach ( var t in fp.Trace ) Console.WriteLine( "   . " + t );
			Console.WriteLine( "   path: " + string.Join( " -> ", fp.Path ) );

			// Cost the path by hand so the movement claim is checkable, and show
			// which steps crossed difficult ground.
			int cost = 0;
			for ( int i = 1; i < fp.Path.Count; i++ )
			{
				int step = board.EnterCost( fp.Path[i] );
				cost += step;
				if ( step > 1 )
					Console.WriteLine( $"   ! {fp.Path[i]} is difficult terrain, cost {step}" );
			}
			Console.WriteLine( $"   replayed cost: {cost} (plan says {fp.MovementSpent})" );

			var figures = new Dictionary<Sq, char> { { heroAt, 'H' } };
			Console.WriteLine();
			Console.WriteLine( BoardRenderer.Render( board, figures, fp.Path, heroAt ) );
		}

		private static void Header( string name, string question )
		{
			Console.WriteLine();
			Console.WriteLine( new string( '=', 68 ) );
			Console.WriteLine( "SCENARIO: " + name );
			Console.WriteLine( "ADJUDICATE: " + question );
			Console.WriteLine( new string( '=', 68 ) );
		}

		private static void Corridor()
		{
			Header( "corridor with difficult terrain",
				"should the troopers wade through the mud or go around it?" );

			var f = Fixture.Parse( @"
+-+-+-+-+-+-+-+-+
|1 . d d . . . H|
+ + + + + + + + +
|2 . d d . . . .|
+ + + + + + + + +
|. . . . . . . .|
+-+-+-+-+-+-+-+-+" );

			var enemies = new[]
			{
				new EnemyFigure { Id = "T1", Name = "Stormtrooper A", Position = f.Marker( '1' ),
					Speed = 4, AttackKind = AttackKind.Ranged },
				new EnemyFigure { Id = "T2", Name = "Stormtrooper B", Position = f.Marker( '2' ),
					Speed = 4, AttackKind = AttackKind.Ranged },
			};
			var heroes = new[]
			{
				new TargetCandidate { Id = "H1", Name = "Diala", Position = f.Marker( 'H' ),
					MaxHealth = 10, Damage = 0 },
			};
			Report( f, enemies, heroes );
		}

		private static void RoomWithDoor()
		{
			Header( "closed door between the groups",
				"the door is shut: no line of sight and no path. do they advance sensibly?" );

			// The door and the wall sit on EDGES (even columns), sealing the
			// eastern half off entirely: no path and no line of sight.
			var f = Fixture.Parse( @"
+-+-+-+-+-+-+-+
|1 . . .D. . H|
+ + + + + + + +
|2 . . .|. . G|
+-+-+-+-+-+-+-+" );

			var enemies = new[]
			{
				new EnemyFigure { Id = "T1", Name = "Trooper A", Position = f.Marker( '1' ),
					Speed = 4, AttackKind = AttackKind.Ranged },
				new EnemyFigure { Id = "T2", Name = "Trooper B", Position = f.Marker( '2' ),
					Speed = 4, AttackKind = AttackKind.Melee },
			};
			var heroes = new[]
			{
				new TargetCandidate { Id = "H1", Name = "Diala", Position = f.Marker( 'H' ),
					MaxHealth = 10 },
				new TargetCandidate { Id = "H2", Name = "Gaarkhan", Position = f.Marker( 'G' ),
					MaxHealth = 12 },
			};
			Report( f, enemies, heroes );
		}

		private static void WoundedPriority()
		{
			Header( "two heroes equidistant, one badly hurt",
				"rule 2 says finish off the one with least Health remaining" );

			var f = Fixture.Parse( @"
+-+-+-+-+-+
|H . . . G|
+ + + + + +
|. . 1 . .|
+-+-+-+-+-+" );

			var enemies = new[]
			{
				new EnemyFigure { Id = "T1", Name = "Elite Trooper", Position = f.Marker( '1' ),
					Speed = 4, AttackKind = AttackKind.Ranged },
			};
			var heroes = new[]
			{
				new TargetCandidate { Id = "H1", Name = "Diala", Position = f.Marker( 'H' ),
					MaxHealth = 10, Damage = 8 },        // 2 remaining
				new TargetCandidate { Id = "H2", Name = "Gaarkhan", Position = f.Marker( 'G' ),
					MaxHealth = 12, Damage = 1 },        // 11 remaining
			};
			Report( f, enemies, heroes );
		}

		private static void Report( Fixture f, EnemyFigure[] enemies, TargetCandidate[] heroes )
		{
			var figures = new Dictionary<Sq, char>();
			for ( int i = 0; i < enemies.Length; i++ ) figures[enemies[i].Position] = (char)('1' + i);
			for ( int i = 0; i < heroes.Length; i++ ) figures[heroes[i].Position] = (char)('a' + i);

			Console.WriteLine( "BEFORE:" );
			Console.WriteLine( BoardRenderer.Render( f.Board, figures ) );
			foreach ( var h in heroes )
				Console.WriteLine( $"   {h.Name,-10} at {h.Position}  health {h.RemainingHealth}/{h.MaxHealth}"
					+ (h.IsHealthy ? "" : "  WOUNDED") );
			foreach ( var e in enemies )
				Console.WriteLine( $"   {e.Name,-14} at {e.Position}  speed {e.Speed}  {e.AttackKind}" );

			var vis = enemies.Select( e => new FigureVisibility { Id = e.Id, Position = e.Position } )
				.Concat( heroes.Select( h => new FigureVisibility { Id = h.Id, Position = h.Position } ) )
				.ToList();

			var plan = ActivationPlanner.Plan( f.Board, enemies, heroes, null, vis );
			Console.WriteLine();
			Console.WriteLine( BoardRenderer.RenderPlan( f.Board, plan, figures ) );
		}
	}
}
