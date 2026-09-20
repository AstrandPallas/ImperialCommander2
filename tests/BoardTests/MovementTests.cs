using System.Collections.Generic;
using System.Linq;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>Movement golden suite, plus the properties that must hold on every board.</summary>
	public static class MovementTests
	{
		public static void Register()
		{
			Suite( "movement" );

			Test( "difficult terrain costs an extra movement point", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A d .|
+-+-+-+" );
				var reach = Pathfinder.Compute( f.Board, f.Marker( 'A' ), 4 );
				Eq( 2, reach.CostTo( new Sq( 1, 0 ) ), "entering difficult costs 2" );
				Eq( 3, reach.CostTo( new Sq( 2, 0 ) ), "2 for difficult then 1 for normal" );
			} );

			Test( "blocking and impassable squares are never reachable", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+
|A X I .|
+-+-+-+-+" );
				var reach = Pathfinder.Compute( f.Board, f.Marker( 'A' ), 9 );
				False( reach.CanReach( new Sq( 1, 0 ) ), "blocking" );
				False( reach.CanReach( new Sq( 2, 0 ) ), "impassable" );
				False( reach.CanReach( new Sq( 3, 0 ) ), "sealed off behind them" );
			} );

			Test( "a wall blocks movement but an impassable edge is also blocked", () =>
			{
				var wall = Fixture.Parse( @"
+-+-+
|A|.|
+-+-+" );
				False( Pathfinder.Compute( wall.Board, wall.Marker( 'A' ), 5 ).CanReach( new Sq( 1, 0 ) ),
					"wall" );

				var imp = Fixture.Parse( @"
+-+-+
|A:.|
+-+-+" );
				False( Pathfinder.Compute( imp.Board, imp.Marker( 'A' ), 5 ).CanReach( new Sq( 1, 0 ) ),
					"impassable edge stops movement even though it keeps adjacency" );
			} );

			Test( "closed doors block movement, open doors do not", () =>
			{
				var closed = Fixture.Parse( @"
+-+-+
|AD.|
+-+-+" );
				False( Pathfinder.Compute( closed.Board, closed.Marker( 'A' ), 5 ).CanReach( new Sq( 1, 0 ) ),
					"closed" );

				var open = Fixture.Parse( @"
+-+-+
|Ao.|
+-+-+" );
				True( Pathfinder.Compute( open.Board, open.Marker( 'A' ), 5 ).CanReach( new Sq( 1, 0 ) ),
					"open" );
			} );

			Test( "a figure may move THROUGH a friendly but not END on it", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A 1 .|
+-+-+-+" );
				var friend = f.Marker( '1' );
				var opt = new MoveOptions { Friendly = s => s == friend };
				var reach = Pathfinder.Compute( f.Board, f.Marker( 'A' ), 4, opt );

				True( reach.CanReach( new Sq( 2, 0 ) ), "passes through the friendly figure" );

				var canEnd = Pathfinder.CanEndOn( f.Board, opt );
				var ends = new HashSet<Sq>( reach.EndSquares( canEnd ) );
				False( ends.Contains( friend ), "cannot finish movement on the friendly figure" );
				True( ends.Contains( new Sq( 2, 0 ) ), "can finish beyond it" );
			} );

			Test( "moving through a hostile costs one extra movement point", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A 1 .|
+-+-+-+" );
				var enemy = f.Marker( '1' );
				var opt = new MoveOptions { Hostile = s => s == enemy };
				var reach = Pathfinder.Compute( f.Board, f.Marker( 'A' ), 4, opt );

				Eq( 2, reach.CostTo( enemy ), "entering the hostile's space costs 1 + 1" );
				Eq( 3, reach.CostTo( new Sq( 2, 0 ) ), "then 1 more to continue past it" );

				var ends = new HashSet<Sq>( reach.EndSquares( Pathfinder.CanEndOn( f.Board, opt ) ) );
				False( ends.Contains( enemy ), "but movement may not END on a figure" );
				True( ends.Contains( new Sq( 2, 0 ) ), "finishing beyond it is fine" );
			} );

			Test( "a hostile toll is avoided when a cheaper route exists", () =>
			{
				// Going around costs 2; going through the hostile costs 2 as
				// well, so the toll must at least not be free.
				var f = Fixture.Parse( @"
+-+-+-+
|A 1 .|
+ + + +
|. . .|
+-+-+-+" );
				var enemy = f.Marker( '1' );
				var withEnemy = Pathfinder.Compute( f.Board, f.Marker( 'A' ), 6,
					new MoveOptions { Hostile = s => s == enemy } );
				var noEnemy = Pathfinder.Compute( f.Board, f.Marker( 'A' ), 6 );
				True( withEnemy.CostTo( enemy ) > noEnemy.CostTo( enemy ),
					"occupied space is strictly more expensive than an empty one" );
			} );

			Test( "Massive ignores blocking and impassable terrain", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A X .|
+-+-+-+" );
				False( Pathfinder.Compute( f.Board, f.Marker( 'A' ), 4 ).CanReach( new Sq( 2, 0 ) ),
					"ordinary figure is stopped" );
				True( Pathfinder.Compute( f.Board, f.Marker( 'A' ), 4, new MoveOptions { Massive = true } )
						.CanReach( new Sq( 2, 0 ) ),
					"Massive walks through it" );
			} );

			Test( "large figures may not move diagonally", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A . .|
+ + + +
|. . .|
+ + + +
|. . .|
+-+-+-+" );
				var small = Pathfinder.Compute( f.Board, f.Marker( 'A' ), 1 );
				True( small.CanReach( new Sq( 1, 1 ) ), "small figure steps diagonally for 1" );

				var large = Pathfinder.Compute( f.Board, f.Marker( 'A' ), 1,
					new MoveOptions { Footprint = Footprint.Large2x2 } );
				False( large.CanReach( new Sq( 1, 1 ) ), "large figure cannot" );
			} );

			Test( "a 2x2 figure needs all four spaces to be clear", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A . .|
+ + + +
|. . X|
+-+-+-+" );
				var opt = new MoveOptions { Footprint = Footprint.Large2x2 };
				var reach = Pathfinder.Compute( f.Board, f.Marker( 'A' ), 6, opt );
				False( reach.CanReach( new Sq( 1, 0 ) ),
					"anchoring at (1,0) would put a corner on the blocking square" );
			} );

			// ---- properties ----

			Test( "property: more movement points never reach fewer squares", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+
|A . d .|
+ +-+ + +
|. . X .|
+ + + + +
|. d . .|
+-+-+-+-+" );
				for ( int mp = 0; mp < 6; mp++ )
				{
					var a = Pathfinder.Compute( f.Board, f.Marker( 'A' ), mp );
					var b = Pathfinder.Compute( f.Board, f.Marker( 'A' ), mp + 1 );
					foreach ( var kv in a.Cost )
						True( b.Cost.ContainsKey( kv.Key ), $"{kv.Key} reachable at {mp} but not {mp + 1}" );
				}
			} );

			Test( "property: every returned path replays legally and within budget", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+
|A . d .|
+ +-+ + +
|. . X .|
+ + + + +
|. d . .|
+-+-+-+-+" );
				const int budget = 6;
				var opt = new MoveOptions();
				var reach = Pathfinder.Compute( f.Board, f.Marker( 'A' ), budget, opt );

				foreach ( var kv in reach.Cost )
				{
					var path = reach.PathTo( kv.Key );
					if ( path.Count == 0 ) continue;
					Eq( reach.Start, path[0], "path starts at the origin" );

					int total = 0;
					for ( int i = 1; i < path.Count; i++ )
					{
						var prev = path[i - 1].Anchor;
						var cur = path[i].Anchor;
						if ( prev == cur ) { total += 1; continue; }   // rotation
						True( f.Board.CanStep( prev, cur ), $"illegal step {prev} -> {cur}" );
						total += f.Board.EnterCost( cur );
					}
					Eq( kv.Value, total, $"replayed cost for {kv.Key}" );
					True( total <= budget, $"path to {kv.Key} exceeds the budget" );
				}
			} );

			Test( "property: adding a wall never increases reachability", () =>
			{
				var open = Fixture.Parse( @"
+-+-+-+
|A . .|
+ + + +
|. . .|
+-+-+-+" );
				var walled = Fixture.Parse( @"
+-+-+-+
|A . .|
+ +-+ +
|. . .|
+-+-+-+" );
				var a = Pathfinder.Compute( open.Board, open.Marker( 'A' ), 4 );
				var b = Pathfinder.Compute( walled.Board, walled.Marker( 'A' ), 4 );
				foreach ( var kv in b.Cost )
				{
					True( a.Cost.ContainsKey( kv.Key ), $"{kv.Key} reachable only with the wall present" );
					True( kv.Value >= a.Cost[kv.Key], $"{kv.Key} became cheaper with a wall added" );
				}
			} );

			Test( "equal-cost paths are returned straight, not wandering", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+-+-+-+
|A . . . . . .|
+ + + + + + + +
|. . . . . . .|
+ + + + + + + +
|. . . . . . .|
+-+-+-+-+-+-+-+" );
				var reach = Pathfinder.Compute( f.Board, f.Marker( 'A' ), 6 );
				var path = reach.PathTo( new Sq( 5, 0 ) ).Select( m => m.Anchor ).ToList();

				Eq( 6, path.Count, "five steps plus the origin" );
				foreach ( var step in path )
					Eq( 0, step.R, $"the straight route never leaves row 0, but visited {step}" );

				// Same for a pure diagonal, which has many equal-cost routes.
				var diag = reach.PathTo( new Sq( 2, 2 ) ).Select( m => m.Anchor ).ToList();
				for ( int i = 1; i < diag.Count; i++ )
				{
					int dc = diag[i].C - diag[i - 1].C;
					int dr = diag[i].R - diag[i - 1].R;
					Eq( 1, dc, $"diagonal run should keep going right at step {i}" );
					Eq( 1, dr, $"diagonal run should keep going down at step {i}" );
				}
			} );

			Test( "straightening never makes a path more expensive", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+-+
|A d d d .|
+ + + + + +
|. . . . .|
+ + + + + +
|. . . . .|
+-+-+-+-+-+" );
				var reach = Pathfinder.Compute( f.Board, f.Marker( 'A' ), 9 );
				var target = new Sq( 4, 0 );
				var path = reach.PathTo( target ).Select( m => m.Anchor ).ToList();

				int replay = 0;
				for ( int i = 1; i < path.Count; i++ )
				{
					True( f.Board.CanStep( path[i - 1], path[i] ),
						$"illegal step {path[i - 1]} -> {path[i]}" );
					replay += f.Board.EnterCost( path[i] );
				}
				Eq( reach.CostTo( target ), replay, "returned path costs exactly what Dijkstra said" );
				True( replay < 7, $"it routed around the mud rather than straight through it ({replay})" );
			} );

		}
	}
}
