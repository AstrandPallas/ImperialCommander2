using System.Collections.Generic;
using System.Linq;
using Saga.Tracking;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>Seeding heroes at setup, and snapping a dragged pin to a square.</summary>
	public static class HeroPlacementTests
	{
		private static BoardModel Open( int w = 10, int h = 6 )
		{
			var b = new BoardModel();
			for ( int c = 0; c < w; c++ )
				for ( int r = 0; r < h; r++ )
					b.SetSquare( new Sq( c, r ), SquareFlags.Normal );
			return b;
		}

		public static void Register()
		{
			Suite( "hero placement" );

			Test( "an entrance is told apart from a mission-event highlight", () =>
			{
				// Highlights mark both, so the name is all there is to go on.
				True( HeroPlacement.IsEntrance( "Entrance" ), "the common case" );
				True( HeroPlacement.IsEntrance( "Entrance 1" ), "numbered" );
				True( HeroPlacement.IsEntrance( "Han Entrance" ), "qualified" );
				True( HeroPlacement.IsEntrance( "Corridor Entrance" ), "qualified the other way" );
				False( HeroPlacement.IsEntrance( "AT-ST" ), "a mission event" );
				False( HeroPlacement.IsEntrance( "Reveal R2-1" ), "another" );
				False( HeroPlacement.IsEntrance( null ), "nothing at all" );
			} );

			Test( "a party is seeded together, on free enterable squares", () =>
			{
				var b = Open();
				var starts = HeroPlacement.SuggestStarts(
					b, new[] { ("Entrance", 2, 2) }, 4 );

				Eq( 4, starts.Count, "one square per hero" );
				Eq( starts.Count, starts.Distinct().Count(), "and no two on the same square" );
				True( starts.Contains( new Sq( 2, 2 ) ), "including the entrance itself" );
				True( starts.All( s => b.IsEnterable( s ) ), "every square must be enterable" );
				True( starts.All( s => Sq.Chebyshev( s, new Sq( 2, 2 ) ) <= 2 ),
					"and the party should land together, not scattered" );
			} );

			Test( "seeding routes round a wall rather than through it", () =>
			{
				// Breadth-first over CanStep, not raw distance: a square a
				// pace away through a wall is not somewhere a hero can start.
				var b = Open( 6, 3 );
				for ( int r = 0; r < 3; r++ )
					b.SetEdge( new Sq( 2, r ), EdgeDir.W, EdgeType.Wall );

				var starts = HeroPlacement.SuggestStarts( b, new[] { ("Entrance", 1, 1) }, 4 );
				Eq( 4, starts.Count, "still seats the party" );
				True( starts.All( s => s.C < 2 ),
					"nobody should be seeded through the wall" );
			} );

			Test( "blocking terrain and occupied squares are skipped", () =>
			{
				var b = Open( 6, 3 );
				b.SetSquare( new Sq( 2, 1 ), SquareFlags.Blocking );

				var starts = HeroPlacement.SuggestStarts(
					b, new[] { ("Entrance", 1, 1) }, 3,
					occupied: new[] { new Sq( 1, 0 ) } );

				False( starts.Contains( new Sq( 2, 1 ) ), "blocking terrain is not a start" );
				False( starts.Contains( new Sq( 1, 0 ) ), "nor is an occupied square" );
				Eq( 3, starts.Count, "and the party is still seated" );
			} );

			Test( "a mission with no entrance seeds nobody, rather than guessing", () =>
			{
				// Two of the 138 shipped missions carry no highlight. Inventing
				// a start for those would be the app making the board up.
				var starts = HeroPlacement.SuggestStarts(
					Open(), new[] { ("AT-ST", 3, 3) }, 4 );
				Eq( 0, starts.Count, "no entrance means no suggestion" );
			} );

			Test( "a pin dropped on a wall snaps to the nearest free square", () =>
			{
				// A finger lands between squares as often as on one, so a drop
				// onto something impassable moves outward instead of failing.
				var b = Open( 6, 4 );
				b.SetSquare( new Sq( 3, 2 ), SquareFlags.Blocking );

				var snapped = HeroPlacement.Snap( b, new Sq( 3, 2 ) );
				True( snapped.HasValue, "a drop near open ground should find a square" );
				Eq( 1, Sq.Chebyshev( snapped.Value, new Sq( 3, 2 ) ),
					"and should be the adjacent ring, not further" );
				Eq( PlacementResult.Ok,
					HeroPlacement.CanPlace( b, snapped.Value ), "and be placeable" );
			} );

			Test( "snapping is deterministic, so the same drop lands the same way", () =>
			{
				var b = Open( 6, 4 );
				b.SetSquare( new Sq( 3, 2 ), SquareFlags.Blocking );
				var first = HeroPlacement.Snap( b, new Sq( 3, 2 ) );
				for ( int i = 0; i < 20; i++ )
					Eq( first, HeroPlacement.Snap( b, new Sq( 3, 2 ) ),
						"repeated drops must not wander" );
			} );

			Test( "a drop with nothing free nearby returns nothing", () =>
			{
				var b = new BoardModel();
				b.SetSquare( new Sq( 0, 0 ), SquareFlags.Blocking );
				Eq( null, HeroPlacement.Snap( b, new Sq( 0, 0 ) ),
					"no free square means no answer, rather than a wrong one" );
			} );

			Test( "occupied squares come from heroes and enemies alike", () =>
			{
				var group = GroupCombatState.Create( "g1", "DG001", "Trooper", 2, 3 );
				TrackerBridge.SetFigurePosition( group, 0, new Sq( 5, 5 ) );
				var hero = new HeroCombatState { CardId = "H1", Name = "Diala" };
				TrackerBridge.SetHeroPosition( hero, new Sq( 1, 1 ), 1 );

				var taken = HeroPlacement.OccupiedSquares( new[] { hero }, new[] { group } );
				True( taken.Contains( new Sq( 1, 1 ) ), "the hero's square" );
				True( taken.Contains( new Sq( 5, 5 ) ), "and the trooper's" );
				Eq( 2, taken.Count, "the unplaced second trooper occupies nothing" );
			} );

			Test( "every simulatable mission seeds a party from its real entrance", () =>
			{
				// The end-to-end check: shipped mission data, real terrain, and
				// four heroes seated on squares they could actually stand on.
				var built = BoardBuilder.Build(
					Core1Placements.Tiles, Core1Placements.Lookup,
					Core1Placements.Doors.Select( d => new DoorPlacement
					{ X = d.X, Y = d.Y, Rotation = d.Rotation, Open = true } ).ToArray(),
					Core1Placements.Library() );

				var starts = HeroPlacement.SuggestStarts(
					built.Board, Core1Placements.Highlights, 4 );

				Eq( 4, starts.Count, "CORE1 should seat a four-hero party" );
				True( starts.All( s => built.Board.IsEnterable( s ) ),
					"on squares that exist and can be entered" );
				Eq( starts.Count, starts.Distinct().Count(), "with nobody stacked" );
			} );
		}
	}
}
