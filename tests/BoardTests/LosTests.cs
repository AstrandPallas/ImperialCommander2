using System.Collections.Generic;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>Line of sight golden suite.</summary>
	public static class LosTests
	{
		private static bool Los( Fixture f, char a, char b )
			=> LineOfSight.HasLos( f.Board, f.Marker( a ), f.Marker( b ) );

		public static void Register()
		{
			Suite( "line of sight" );

			Test( "open board has line of sight", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A . .|
+ + + +
|. . .|
+ + + +
|. . T|
+-+-+-+" );
				True( Los( f, 'A', 'T' ), "diagonal across open ground" );
			} );

			Test( "blocking terrain stops line of sight", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|. . .|
+ + + +
|A X T|
+ + + +
|. . .|
+-+-+-+" );
				False( Los( f, 'A', 'T' ), "blocking square directly between" );
			} );

			Test( "impassable terrain does NOT stop line of sight", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|. . .|
+ + + +
|A I T|
+ + + +
|. . .|
+-+-+-+" );
				True( Los( f, 'A', 'T' ), "impassable is transparent to LOS" );
			} );

			Test( "wall between adjacent spaces stops line of sight", () =>
			{
				var f = Fixture.Parse( @"
+-+-+
|A|T|
+-+-+" );
				False( Los( f, 'A', 'T' ), "wall on the shared edge" );
			} );

			Test( "closed door stops line of sight, open door does not", () =>
			{
				var closed = Fixture.Parse( @"
+-+-+
|ADT|
+-+-+" );
				False( Los( closed, 'A', 'T' ), "closed door" );

				var open = Fixture.Parse( @"
+-+-+
|AoT|
+-+-+" );
				True( Los( open, 'A', 'T' ), "open door" );
			} );

			Test( "impassable edge does NOT stop line of sight", () =>
			{
				var f = Fixture.Parse( @"
+-+-+
|A:T|
+-+-+" );
				True( Los( f, 'A', 'T' ), "impassable edge is transparent" );
			} );

			Test( "blocking edge stops line of sight", () =>
			{
				var f = Fixture.Parse( @"
+-+-+
|A#T|
+-+-+" );
				False( Los( f, 'A', 'T' ), "blocking edge" );
			} );

			Test( "a continuous wall blocks line of sight", () =>
			{
				// The wall spans the full width between the two spaces, so every
				// legal trace would have to pass through it.
				var f = Fixture.Parse( @"
+-+-+
|A .|
+-+-+
|. .|
+ + +
|T .|
+-+-+" );
				False( Los( f, 'A', 'T' ), "unbroken wall between the two spaces" );
			} );

			Test( "sight passes the open end of a short wall", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A . .|
+ +-+ +
|. . .|
+ + + +
|. . T|
+-+-+-+" );
				True( Los( f, 'A', 'T' ), "wall ends before the sight line" );
			} );

			Test( "glancing along a wall is legal", () =>
			{
				// The wall runs parallel to and along the sight line rather than
				// across it, so it never enters the traced region.
				var f = Fixture.Parse( @"
+-+-+-+
|A . T|
+-+-+-+
|. . .|
+-+-+-+" );
				True( Los( f, 'A', 'T' ), "wall alongside, not across" );
			} );

			Test( "figures block line of sight", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|. . .|
+ + + +
|A 1 T|
+ + + +
|. . .|
+-+-+-+" );
				var occupied = new HashSet<Sq> { f.Marker( '1' ) };
				True( LineOfSight.HasLos( f.Board, f.Marker( 'A' ), f.Marker( 'T' ) ),
					"clear when occupancy is not supplied" );
				False( LineOfSight.HasLos( f.Board, f.Marker( 'A' ), f.Marker( 'T' ), occupied.Contains ),
					"blocked by the intervening figure" );
			} );

			Test( "a figure on the target square does not block itself", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A . T|
+ + + +
|. . .|
+-+-+-+" );
				var occupied = new HashSet<Sq> { f.Marker( 'A' ), f.Marker( 'T' ) };
				True( LineOfSight.HasLos( f.Board, f.Marker( 'A' ), f.Marker( 'T' ), occupied.Contains ),
					"attacker and target are excluded from blocking" );
			} );

			Test( "line of sight is symmetric", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+
|A . X .|
+ +-+ + +
|. . . .|
+ + + + +
|. X . T|
+-+-+-+-+" );
				foreach ( var a in f.Board.Squares )
				{
					foreach ( var b in f.Board.Squares )
					{
						if ( a == b ) continue;
						Eq( LineOfSight.HasLos( f.Board, a, b ),
							LineOfSight.HasLos( f.Board, b, a ),
							$"symmetry between {a} and {b}" );
					}
				}
			} );

			Test( "adding a wall never creates line of sight", () =>
			{
				// Blocking dominance: strictly more obstruction can only remove
				// visibility, never add it.
				var open = Fixture.Parse( @"
+-+-+-+
|. . .|
+ + + +
|. . .|
+ + + +
|. . .|
+-+-+-+" );
				var walled = Fixture.Parse( @"
+-+-+-+
|. . .|
+ +-+ +
|. . .|
+ + + +
|. . .|
+-+-+-+" );
				foreach ( var a in open.Board.Squares )
				{
					foreach ( var b in open.Board.Squares )
					{
						if ( a == b ) continue;
						if ( !LineOfSight.HasLos( open.Board, a, b ) ) continue;
						// allowed to become false, never the reverse
						if ( LineOfSight.HasLos( walled.Board, a, b ) ) continue;
					}
				}
				foreach ( var a in walled.Board.Squares )
				{
					foreach ( var b in walled.Board.Squares )
					{
						if ( a == b ) continue;
						if ( LineOfSight.HasLos( walled.Board, a, b ) )
							True( LineOfSight.HasLos( open.Board, a, b ),
								$"wall created LOS between {a} and {b}" );
					}
				}
			} );
		}
	}
}
