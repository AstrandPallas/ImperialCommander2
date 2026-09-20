using System.Collections.Generic;
using System.Linq;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>Attacks and space counting.</summary>
	public static class AttackTests
	{
		public static void Register()
		{
			Suite( "counting distance" );

			Test( "difficult terrain costs movement but not distance", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A d T|
+-+-+-+" );
				var a = f.Marker( 'A' );
				var t = f.Marker( 'T' );
				Eq( 2, Distance.Count( f.Board, a, t ), "two spaces away regardless of terrain" );
				Eq( 3, Pathfinder.Compute( f.Board, a, 9 ).CostTo( t ),
					"but three movement points to walk it" );
			} );

			Test( "diagonals count as one space", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A . .|
+ + + +
|. . .|
+ + + +
|. . T|
+-+-+-+" );
				Eq( 2, Distance.Count( f.Board, f.Marker( 'A' ), f.Marker( 'T' ) ),
					"two diagonal steps" );
			} );

			Test( "spaces cannot be counted through blocking terrain", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|X X X|
+-+-+-+
|A X T|
+-+-+-+
|X X X|
+-+-+-+" );
				Eq( -1, Distance.Count( f.Board, f.Marker( 'A' ), f.Marker( 'T' ) ),
					"walled in by blocking terrain" );
			} );

			Test( "figures do not interrupt counting", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A 1 T|
+-+-+-+" );
				Eq( 2, Distance.Count( f.Board, f.Marker( 'A' ), f.Marker( 'T' ) ),
					"counting passes straight through a figure" );
			} );

			Suite( "attacks" );

			Test( "melee can only target an adjacent figure", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A B C|
+-+-+-+" );
				True( AttackEvaluator.Assess( f.Board, f.Marker( 'A' ), f.Marker( 'B' ),
					AttackKind.Melee ).CanDeclare, "adjacent" );
				False( AttackEvaluator.Assess( f.Board, f.Marker( 'A' ), f.Marker( 'C' ),
					AttackKind.Melee ).CanDeclare, "two spaces away" );
			} );

			Test( "a wall denies melee even between touching spaces", () =>
			{
				var f = Fixture.Parse( @"
+-+-+
|A|T|
+-+-+" );
				False( AttackEvaluator.Assess( f.Board, f.Marker( 'A' ), f.Marker( 'T' ),
					AttackKind.Melee ).CanDeclare, "wall severs adjacency" );
			} );

			Test( "impassable terrain leaves spaces adjacent, so melee still works", () =>
			{
				// The distinction that is easiest to get backwards: impassable
				// stops movement but not adjacency and not line of sight.
				var f = Fixture.Parse( @"
+-+-+
|A:T|
+-+-+" );
				True( AttackEvaluator.Assess( f.Board, f.Marker( 'A' ), f.Marker( 'T' ),
					AttackKind.Melee ).CanDeclare, "still adjacent across an impassable edge" );
			} );

			Test( "ranged attacks have NO maximum range", () =>
			{
				// Rules Reference gates ranged attacks on line of sight, not on a
				// range value. Distance only raises the accuracy required.
				var f = Fixture.Parse( @"
+-+-+-+-+-+-+-+-+
|A . . . . . . T|
+-+-+-+-+-+-+-+-+" );
				var a = AttackEvaluator.Assess( f.Board, f.Marker( 'A' ), f.Marker( 'T' ),
					AttackKind.Ranged );
				True( a.CanDeclare, "a distant target in line of sight is legal" );
				Eq( 7, a.Distance, "seven spaces" );
				Eq( 7, a.RequiredAccuracy, "accuracy must meet the distance" );
			} );

			Test( "ranged attacks require line of sight", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A X T|
+-+-+-+" );
				False( AttackEvaluator.Assess( f.Board, f.Marker( 'A' ), f.Marker( 'T' ),
					AttackKind.Ranged ).CanDeclare, "blocking terrain between" );
			} );

			Test( "an intervening figure denies a ranged attack", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|. . .|
+ + + +
|A 1 T|
+ + + +
|. . .|
+-+-+-+" );
				var blockers = new[] { f.Marker( '1' ) };
				True( AttackEvaluator.Assess( f.Board, f.Marker( 'A' ), f.Marker( 'T' ),
					AttackKind.Ranged ).CanDeclare, "clear when nobody is standing there" );
				False( AttackEvaluator.Assess( f.Board, f.Marker( 'A' ), f.Marker( 'T' ),
					AttackKind.Ranged, blockers ).CanDeclare, "figure blocks the shot" );
			} );

			Test( "firing positions come back cheapest first", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+-+
|A . . . T|
+ + + + + +
|. . . . .|
+-+-+-+-+-+" );
				var opt = new MoveOptions();
				var reach = Pathfinder.Compute( f.Board, f.Marker( 'A' ), 3, opt );
				var spots = AttackEvaluator.FiringPositions( f.Board, reach, f.Marker( 'T' ),
					AttackKind.Ranged, Pathfinder.CanEndOn( f.Board, opt ) );

				True( spots.Count > 0, "some firing position exists" );
				for ( int i = 1; i < spots.Count; i++ )
					True( spots[i].moveCost >= spots[i - 1].moveCost, "ordered by movement cost" );
				Eq( 0, spots[0].moveCost, "standing still is already a firing position" );
			} );

			Test( "melee firing positions are exactly the adjacent reachable spaces", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A . .|
+ + + +
|. . T|
+-+-+-+" );
				var opt = new MoveOptions();
				var reach = Pathfinder.Compute( f.Board, f.Marker( 'A' ), 4, opt );
				var spots = AttackEvaluator.FiringPositions( f.Board, reach, f.Marker( 'T' ),
					AttackKind.Melee, Pathfinder.CanEndOn( f.Board, opt ) );
				foreach ( var s in spots )
					True( f.Board.AreAdjacent( s.square, f.Marker( 'T' ) ),
						$"{s.square} must be adjacent for melee" );
				True( spots.Any( s => s.square == new Sq( 1, 1 ) ), "(1,1) is a valid melee spot" );
			} );

			Suite( "visibility abilities" );

			Test( "a figure hidden at range is not a legal ranged target", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+-+-+
|A . . . . T|
+-+-+-+-+-+-+" );
				var mak = new FigureVisibility { Id = "Mak", Position = f.Marker( 'T' ), HiddenAtOrBeyond = 4 };
				False( Visibility.CanSee( f.Board, f.Marker( 'A' ), mak ),
					"five spaces away, beyond the threshold" );
			} );

			Test( "the same figure is visible from inside the threshold", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A . T|
+-+-+-+" );
				var mak = new FigureVisibility { Id = "Mak", Position = f.Marker( 'T' ), HiddenAtOrBeyond = 4 };
				True( Visibility.CanSee( f.Board, f.Marker( 'A' ), mak ), "two spaces away" );
			} );

			Test( "a figure hidden from an observer also stops blocking for it", () =>
			{
				// The half of the ability that is easy to miss: blocking is per
				// observer, not a property of the board.
				var f = Fixture.Parse( @"
+-+-+-+-+-+-+-+
|A . . 1 . . T|
+-+-+-+-+-+-+-+" );
				var hidden = new FigureVisibility { Id = "Mak", Position = f.Marker( '1' ), HiddenAtOrBeyond = 3 };
				var ordinary = new FigureVisibility { Id = "Trooper", Position = f.Marker( '1' ) };
				var target = new FigureVisibility { Id = "Target", Position = f.Marker( 'T' ) };

				False( Visibility.CanSee( f.Board, f.Marker( 'A' ), target, new[] { ordinary } ),
					"an ordinary figure in the way blocks the shot" );
				True( Visibility.CanSee( f.Board, f.Marker( 'A' ), target, new[] { hidden } ),
					"a figure hidden from this observer does not block it" );
			} );

			Test( "blockers are recomputed per observer", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+-+-+-+
|A . . 1 . . B|
+-+-+-+-+-+-+-+" );
				var hidden = new FigureVisibility { Id = "Mak", Position = f.Marker( '1' ), HiddenAtOrBeyond = 3 };
				var far = Visibility.BlockersFor( f.Board, f.Marker( 'A' ), new[] { hidden } );
				var near = Visibility.BlockersFor( f.Board, f.Marker( '1' ).West, new[] { hidden } );
				Eq( 0, far.Count, "distant observer is not blocked by the hidden figure" );
				Eq( 1, near.Count, "adjacent observer still is" );
			} );

		}
	}
}