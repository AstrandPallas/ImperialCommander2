using System.Collections.Generic;
using System.Linq;
using Saga.Tracking;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>Choosing which deployment point to arrive at, and where to stand on arrival.</summary>
	public static class DeploymentTests
	{
		private static BoardModel Open( int w = 20, int h = 8 )
		{
			var b = new BoardModel();
			for ( int c = 0; c < w; c++ )
				for ( int r = 0; r < h; r++ )
					b.SetSquare( new Sq( c, r ), SquareFlags.None );
			return b;
		}

		private static HeroCombatState Hero( string id, Sq at )
		{
			var h = new HeroCombatState { CardId = id, Name = id };
			TrackerBridge.SetHeroPosition( h, at, 1 );
			return h;
		}

		public static void Register()
		{
			Suite( "deployment" );

			Test( "the point nearest the Rebels is suggested first", () =>
			{
				// A group that arrives across the map does nothing for several
				// rounds, which is the one outcome an Imperial player would
				// never pick. Taking the first point in the list did exactly
				// that whenever the list happened to be ordered badly.
				var b = Open();
				var points = new[]
				{
					(Name: "DP far", C: 1, R: 4),
					(Name: "DP near", C: 14, R: 4),
				};
				var heroes = new[] { Hero( "H1", new Sq( 17, 4 ) ) };

				var ranked = DeploymentPlanner.RankPoints( b, points, heroes );
				Eq( 2, ranked.Count, "both points are offered" );
				Eq( "DP near", ranked[0].Name, "the near one is suggested" );
				Eq( 3, ranked[0].ToNearestRebel, "three spaces from the hero" );
				True( ranked[0].Reason.Contains( "3 spaces" ), "and says so" );
			} );

			Test( "a point with no route to anybody sorts last but is still offered", () =>
			{
				// It may be the only point the mission's own text allows.
				var b = Open();
				for ( int r = 0; r < 8; r++ )
					b.SetSquare( new Sq( 10, r ), SquareFlags.Blocking );

				var points = new[]
				{
					(Name: "DP sealed", C: 2, R: 4),
					(Name: "DP open", C: 14, R: 4),
				};
				var heroes = new[] { Hero( "H1", new Sq( 17, 4 ) ) };

				var ranked = DeploymentPlanner.RankPoints( b, points, heroes );
				Eq( "DP open", ranked[0].Name, "the reachable point wins" );
				Eq( "DP sealed", ranked[1].Name, "but the other is not discarded" );
				Eq( -1, ranked[1].ToNearestRebel, "it simply has no counted route" );
				True( ranked[1].Reason.Contains( "no counted route" ), "and says why" );
			} );

			Test( "objectives break a tie between equally placed points", () =>
			{
				var b = Open();
				var points = new[]
				{
					(Name: "DP north", C: 8, R: 1),
					(Name: "DP south", C: 8, R: 7),
				};
				var heroes = new[] { Hero( "H1", new Sq( 8, 4 ) ) };

				var plain = DeploymentPlanner.RankPoints( b, points, heroes );
				Eq( plain[0].ToNearestRebel, plain[1].ToNearestRebel,
					"the two points are equally far from the hero" );

				var map = new ObjectiveMap();
				map.Add( new Sq( 8, 7 ) );
				var aware = DeploymentPlanner.RankPoints( b, points, heroes, map );
				Eq( "DP south", aware[0].Name, "the one on the objective is preferred" );
			} );

			Test( "ranking is deterministic", () =>
			{
				var b = Open();
				var points = new[]
				{
					(Name: "A", C: 3, R: 2), (Name: "B", C: 3, R: 6), (Name: "C", C: 5, R: 2),
				};
				var heroes = new[] { Hero( "H1", new Sq( 10, 4 ) ) };

				var first = DeploymentPlanner.RankPoints( b, points, heroes )
					.Select( p => p.Name ).ToList();
				for ( int i = 0; i < 10; i++ )
					Eq( string.Join( ",", first ),
						string.Join( ",", DeploymentPlanner.RankPoints( b, points, heroes )
							.Select( p => p.Name ) ),
						"the same board must always suggest the same point" );
			} );

			Test( "no deployment points means no suggestion, not a guess", () =>
			{
				var b = Open();
				Eq( 0, DeploymentPlanner.RankPoints( b, null, null ).Count,
					"nothing to offer" );
			} );

			Suite( "deployment placement" );

			Test( "a group arrives together and on legal squares", () =>
			{
				var b = Open();
				var placed = DeploymentPlanner.PlaceGroup( b, new Sq( 8, 4 ), 3 );

				Eq( 3, placed.Count, "one square per figure" );
				Eq( 3, placed.Distinct().Count(), "and no two on the same square" );
				True( placed.All( s => b.IsEnterable( s ) ), "all enterable" );
				True( placed.All( s => Sq.Chebyshev( s, new Sq( 8, 4 ) ) <= 2 ),
					"and the group lands together" );
			} );

			Test( "figures are not stacked on squares already held", () =>
			{
				var b = Open();
				var taken = new[] { new Sq( 8, 4 ), new Sq( 8, 5 ) };
				var placed = DeploymentPlanner.PlaceGroup( b, new Sq( 8, 4 ), 3, taken );

				Eq( 3, placed.Count, "still seats the group" );
				False( placed.Intersect( taken ).Any(), "but never on an occupied square" );
			} );

			Test( "deployment routes round a wall rather than through it", () =>
			{
				var b = Open( 10, 5 );
				for ( int r = 0; r < 5; r++ )
					b.SetEdge( new Sq( 3, r ), EdgeDir.W, EdgeType.Wall );

				var placed = DeploymentPlanner.PlaceGroup( b, new Sq( 1, 2 ), 4 );
				Eq( 4, placed.Count, "the group is seated" );
				True( placed.All( s => s.C < 3 ),
					"and nobody arrives through the wall" );
			} );

			Test( "a square with a line to a Rebel is preferred over a blind one", () =>
			{
				// A figure deployed into a corner it cannot see out of spends
				// its first activation walking back out again.
				//
				// The geometry is built so the BLIND square sorts first by
				// column: without the preference the figure lands there, which
				// is what makes this test able to fail. Verified by disabling
				// the preference and watching it go red.
				var b = new BoardModel();
				for ( int c = 0; c < 8; c++ )
					for ( int r = 0; r < 7; r++ )
						b.SetSquare( new Sq( c, r ), SquareFlags.Blocking );
				foreach ( var open in new[]
				{
					new Sq( 4, 5 ), new Sq( 5, 5 ), new Sq( 6, 5 ),
					new Sq( 6, 4 ), new Sq( 6, 3 ), new Sq( 6, 2 ), new Sq( 6, 1 ),
				} )
					b.SetSquare( open, SquareFlags.None );

				var hero = new Sq( 6, 1 );
				var heroes = new[] { Hero( "H1", hero ) };

				// The point itself is already held, so the choice is between
				// the two squares beside it.
				var occupied = new[] { new Sq( 5, 5 ) };

				False( LineOfSight.HasLos( b, new Sq( 4, 5 ), hero ),
					"the lower-numbered square is blind" );
				True( LineOfSight.HasLos( b, new Sq( 6, 5 ), hero ),
					"and the other one can see up the corridor" );

				var placed = DeploymentPlanner.PlaceGroup( b, new Sq( 5, 5 ), 1,
					occupied, heroes );

				Eq( 1, placed.Count, "the figure is seated" );
				False( placed[0] == new Sq( 4, 5 ),
					"not on the blind square, which is the one plain ordering picks" );
				True( LineOfSight.HasLos( b, placed[0], hero ),
					"but on one that can actually see the hero, got " + placed[0] );
			} );

			Test( "sight is only a tie-break, never a reason to scatter", () =>
			{
				// It separates squares at the same remove from the point. It
				// must not pull a figure further away to find a better angle.
				var b = Open( 16, 7 );
				var heroes = new[] { Hero( "H1", new Sq( 14, 3 ) ) };

				var placed = DeploymentPlanner.PlaceGroup( b, new Sq( 3, 3 ), 4, null, heroes );
				Eq( 4, placed.Count, "all four are seated" );
				True( placed.All( s => Sq.Chebyshev( s, new Sq( 3, 3 ) ) <= 2 ),
					"and all of them near the point" );
			} );

			Test( "a group larger than the space available is reported, not stacked", () =>
			{
				var b = new BoardModel();
				b.SetSquare( new Sq( 0, 0 ), SquareFlags.None );
				b.SetSquare( new Sq( 1, 0 ), SquareFlags.None );

				var placed = DeploymentPlanner.PlaceGroup( b, new Sq( 0, 0 ), 5 );
				Eq( 2, placed.Count, "only what fits" );
				Eq( 2, placed.Distinct().Count(), "with nobody doubled up" );
			} );
		}
	}
}
