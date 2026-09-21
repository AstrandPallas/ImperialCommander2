using System;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	public static class Program
	{
		public static int Main( string[] args )
		{
			if ( args.Length > 0 && args[0] == "--demo" )
			{
				Scenario.RunAll();
				return 0;
			}

			if ( args.Length > 0 && args[0] == "--sweep" )
			{
				int seeds = args.Length > 1 ? int.Parse( args[1] ) : 100;
				int rnds = args.Length > 2 ? int.Parse( args[2] ) : 8;
				int total = 0;
				foreach ( var m in Sim.Missions )
				{
					int bad = 0;
					for ( uint sd = 1; sd <= seeds; sd++ )
						bad += Sim.Run( sd, rnds, quiet: true, mission: m );
					total += bad;
					Console.WriteLine( $"{m,-9} {seeds} seeds x {rnds} rounds -> "
						+ (bad == 0 ? "clean" : bad + " ILLEGAL") );
				}
				Console.WriteLine();
				Console.WriteLine( total == 0
					? $"all {Sim.Missions.Length} missions clean"
					: $"{total} violations across {Sim.Missions.Length} missions" );
				return total == 0 ? 0 : 1;
			}

			// Headless simulated play on a real map. Prints an adjudicable
			// transcript and returns non-zero if any order was illegal.
			if ( args.Length > 0 && args[0] == "--sim" )
			{
				uint seed = args.Length > 1 ? uint.Parse( args[1] ) : 1u;
				int rounds = args.Length > 2 ? int.Parse( args[2] ) : 6;
				string mission = args.Length > 3 ? args[3] : "CORE1";
				return Sim.Run( seed, rounds, false, mission );
			}

			string filter = args.Length > 0 ? args[0] : null;

			RegisterFixtureTests();
			RegisterAdjacencyTests();
			LosTests.Register();
			MovementTests.Register();
			AiTests.Register();
			AttackTests.Register();
			PlannerTests.Register();
			MissionTests.Register();
			TrackingTests.Register();
			PlannerFuzzTests.Register();
			MassiveTests.Register();
			CovertTests.Register();
			InterpretationTests.Register();
			CorrectionTests.Register();
			OverrideTests.Register();
			ViewTests.Register();
			TrackerBridgeTests.Register();
			HeroPlacementTests.Register();
			KeywordTests.Register();
			HeroStatsTests.Register();
			PlayerQueryTests.Register();
			UndoTests.Register();
			ObjectiveTests.Register();
			DeploymentTests.Register();
			SimTests.Register();

			return Run( filter );
		}

		private static void RegisterFixtureTests()
		{
			Suite( "fixture parser" );

			Test( "parses squares, edges and markers", () =>
			{
				var f = Fixture.Parse( @"
+-+-+
|A d|
+ +-+
|X|T|
+-+-+" );
				Eq( 2, f.Width, "width" );
				Eq( 2, f.Height, "height" );
				Eq( new Sq( 0, 0 ), f.Marker( 'A' ), "marker A" );
				Eq( new Sq( 1, 1 ), f.Marker( 'T' ), "marker T" );
				Eq( SquareFlags.Difficult, f.Board.Flags( new Sq( 1, 0 ) ), "difficult square" );
				Eq( SquareFlags.Blocking, f.Board.Flags( new Sq( 0, 1 ) ), "blocking square" );
				Eq( EdgeType.Wall, f.Board.Edge( new Sq( 1, 1 ), EdgeDir.N ), "wall north of (1,1)" );
				Eq( EdgeType.Wall, f.Board.Edge( new Sq( 1, 1 ), EdgeDir.W ), "wall west of (1,1)" );
				Eq( EdgeType.Open, f.Board.Edge( new Sq( 1, 0 ), EdgeDir.W ), "open between (0,0) and (1,0)" );
			} );

			Test( "an edge is stored once regardless of which side asks", () =>
			{
				var f = Fixture.Parse( @"
+-+-+
|A|T|
+-+-+" );
				var a = new Sq( 0, 0 );
				var t = new Sq( 1, 0 );
				Eq( EdgeType.Wall, f.Board.Edge( a, t ), "a -> t" );
				Eq( EdgeType.Wall, f.Board.Edge( t, a ), "t -> a" );
			} );
		}

		private static void RegisterAdjacencyTests()
		{
			Suite( "adjacency and movement" );

			Test( "difficult terrain costs 2 to enter", () =>
			{
				var f = Fixture.Parse( @"
+-+-+
|. d|
+-+-+" );
				Eq( 1, f.Board.EnterCost( new Sq( 0, 0 ) ), "normal square" );
				Eq( 2, f.Board.EnterCost( new Sq( 1, 0 ) ), "difficult square" );
			} );

			Test( "walls break adjacency, impassable does not", () =>
			{
				var wall = Fixture.Parse( @"
+-+-+
|A|T|
+-+-+" );
				False( wall.Board.AreAdjacent( wall.Marker( 'A' ), wall.Marker( 'T' ) ),
					"wall severs adjacency" );

				var imp = Fixture.Parse( @"
+-+-+
|A:T|
+-+-+" );
				True( imp.Board.AreAdjacent( imp.Marker( 'A' ), imp.Marker( 'T' ) ),
					"impassable leaves spaces adjacent" );
				False( imp.Board.CanStep( imp.Marker( 'A' ), imp.Marker( 'T' ) ),
					"but movement across it is blocked" );
			} );

			Test( "closed door breaks adjacency, open door does not", () =>
			{
				var closed = Fixture.Parse( @"
+-+-+
|ADT|
+-+-+" );
				False( closed.Board.AreAdjacent( closed.Marker( 'A' ), closed.Marker( 'T' ) ), "closed" );
				False( closed.Board.CanStep( closed.Marker( 'A' ), closed.Marker( 'T' ) ), "closed movement" );

				var open = Fixture.Parse( @"
+-+-+
|AoT|
+-+-+" );
				True( open.Board.AreAdjacent( open.Marker( 'A' ), open.Marker( 'T' ) ), "open" );
				True( open.Board.CanStep( open.Marker( 'A' ), open.Marker( 'T' ) ), "open movement" );
			} );

			Test( "blocking and impassable squares cannot be entered", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+
|. X I P|
+-+-+-+-+" );
				True( f.Board.IsEnterable( new Sq( 0, 0 ) ), "normal" );
				False( f.Board.IsEnterable( new Sq( 1, 0 ) ), "blocking" );
				False( f.Board.IsEnterable( new Sq( 2, 0 ) ), "impassable" );
				False( f.Board.IsEnterable( new Sq( 3, 0 ) ), "pit" );
			} );

			Test( "diagonal movement is blocked when both routes are sealed", () =>
			{
				// (0,0) to (1,1): both corner squares are walled off.
				var f = Fixture.Parse( @"
+-+-+
|A|.|
+-+-+
|. T|
+-+-+" );
				False( f.Board.CanStep( f.Marker( 'A' ), f.Marker( 'T' ) ),
					"no clear orthogonal route around the corner" );
			} );

			Test( "diagonal movement is allowed when one route is open", () =>
			{
				var f = Fixture.Parse( @"
+-+-+
|A .|
+-+ +
|. T|
+-+-+" );
				True( f.Board.CanStep( f.Marker( 'A' ), f.Marker( 'T' ) ),
					"route via (1,0) is clear" );
			} );

			Test( "inactive squares are not pathable", () =>
			{
				var f = Fixture.Parse( @"
+-+-+
|A .|
+-+-+" );
				var t = new Sq( 1, 0 );
				True( f.Board.CanStep( f.Marker( 'A' ), t ), "active" );
				f.Board.SetActive( t, false );
				False( f.Board.CanStep( f.Marker( 'A' ), t ), "hidden map section is not pathable" );
			} );
		}
	}
}
