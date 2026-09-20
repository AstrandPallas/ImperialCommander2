using System.Collections.Generic;
using System.Linq;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>The places where the rules are silent and the engine had to choose.</summary>
	public static class InterpretationTests
	{
		public static void Register()
		{
			Suite( "documented interpretations are real settings" );

			Test( "StrictBarrierCorners changes line of sight, and only loosens it", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+-+
|A . . . .|
+-+-+-+-+-+
| | | | | |
+-+-+-+-+-+
|. . . . B|
+-+-+-+-+-+
" );
				var b = f.Board;
				var from = f.Marker( 'A' );
				var to = f.Marker( 'B' );

				b.StrictBarrierCorners = true;
				bool strict = LineOfSight.HasLos( b, from, to );
				b.StrictBarrierCorners = false;
				bool loose = LineOfSight.HasLos( b, from, to );

				True( loose || !strict,
					"relaxing StrictBarrierCorners must never remove line of sight" );
			} );

			Test( "relaxing StrictBarrierCorners never removes sight, over many boards", () =>
			{
				// The directional claim, checked as a property rather than on a
				// single hand-picked board. A setting documented as "strictly
				// more permissive" that occasionally takes sight away would be a
				// genuinely nasty bug, because it would only show on the boards
				// nobody tried.
				uint seed = 12345;
				uint Next() { seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5; return seed; }

				int differed = 0;
				for ( int iter = 0; iter < 200; iter++ )
				{
					var b = new BoardModel();
					for ( int c = 0; c < 6; c++ )
						for ( int r = 0; r < 6; r++ )
							b.SetSquare( new Sq( c, r ),
								Next() % 10 == 0 ? SquareFlags.Blocking : SquareFlags.None );
					for ( int c = 0; c < 6; c++ )
						for ( int r = 0; r < 6; r++ )
						{
							if ( Next() % 8 == 0 ) b.SetEdge( new Sq( c, r ), EdgeDir.N, EdgeType.Wall );
							if ( Next() % 8 == 0 ) b.SetEdge( new Sq( c, r ), EdgeDir.W, EdgeType.Wall );
						}

					var squares = b.Squares.ToList();
					foreach ( var a in squares )
					{
						foreach ( var z in squares )
						{
							if ( a == z ) continue;
							b.StrictBarrierCorners = true;
							bool strict = LineOfSight.HasLos( b, a, z );
							b.StrictBarrierCorners = false;
							bool loose = LineOfSight.HasLos( b, a, z );
							True( loose || !strict,
								$"relaxing the rule removed sight {a} -> {z}" );
							if ( strict != loose ) differed++;
						}
					}
				}

				// If the two modes never disagreed the property above would be
				// vacuously true and the setting would be doing nothing.
				True( differed > 0,
					"the two interpretations never disagreed, so the setting is inert" );
			} );

			Test( "LargeFigurePaysWorstSpace changes what a 2x2 pays for mud", () =>
			{
				var b = new BoardModel();
				for ( int c = 0; c < 6; c++ )
					for ( int r = 0; r < 4; r++ )
						b.SetSquare( new Sq( c, r ), SquareFlags.None );
				// One foot of the 2x2's destination lands in difficult terrain.
				b.SetSquare( new Sq( 2, 1 ), SquareFlags.Difficult );

				var opt = new MoveOptions { Footprint = Footprint.Large2x2 };
				var start = new Sq( 0, 0 );

				b.LargeFigurePaysWorstSpace = true;
				int worst = Pathfinder.Compute( b, start, 8, opt ).CostTo( new Sq( 2, 0 ) );
				b.LargeFigurePaysWorstSpace = false;
				int best = Pathfinder.Compute( b, start, 8, opt ).CostTo( new Sq( 2, 0 ) );

				True( worst > 0 && best > 0, "both readings must reach the square" );
				True( worst >= best,
					"charging the worst space can never be cheaper than charging the best" );
				True( worst > best,
					"with one foot in mud the two readings must actually differ, "
					+ $"got worst={worst} best={best}" );
			} );
		}
	}
}
