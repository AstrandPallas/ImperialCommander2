// GENERATED from a rules dispute captured at the table.
// Regenerate with: python tools/import_dispute.py <dispute.json>
//
// Captured 2026-09-21T12:00:00Z, mission CORE1, round 3.
// Reason given: the trooper walked through the wall
//
// ORDERS AS GIVEN
			//   Stormtrooper #1: move 3 to (2,1), attack Diala
//
// WHY THE APP SAID SO
			//   target: rule 1 closest healthy
//
// This test asserts the board rebuilds and the recorded orders replay legally.
// It does NOT assert the order was right -- the file exists because somebody
// thought it was wrong. Decide what should have happened and add it below.
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	public static class Dispute_Example
	{
		public static BoardModel Board()
		{
			var b = new BoardModel();
			b.SetSquare( new Sq( 0, 0 ), SquareFlags.None );
			b.SetSquare( new Sq( 0, 1 ), SquareFlags.None );
			b.SetSquare( new Sq( 0, 2 ), SquareFlags.None );
			b.SetSquare( new Sq( 1, 0 ), SquareFlags.None );
			b.SetSquare( new Sq( 1, 1 ), SquareFlags.None );
			b.SetSquare( new Sq( 1, 2 ), SquareFlags.None );
			b.SetSquare( new Sq( 2, 0 ), SquareFlags.None );
			b.SetSquare( new Sq( 2, 1 ), SquareFlags.None );
			b.SetSquare( new Sq( 2, 2 ), SquareFlags.None );
			b.SetSquare( new Sq( 3, 0 ), SquareFlags.None );
			b.SetSquare( new Sq( 3, 1 ), SquareFlags.None );
			b.SetSquare( new Sq( 3, 2 ), SquareFlags.None );
			b.SetEdge( new Sq( 2, 1 ), EdgeDir.W, EdgeType.Wall );
			return b;
		}

		public static void Register()
		{
			Suite( "dispute: Example" );

			Test( "the disputed board rebuilds", () =>
			{
				var b = Board();
				Eq( 12, b.Count, "every square recorded is on the board" );
				// enemy Stormtrooper #1 at (0,1)
				// REBEL  Diala at (3,1)
			} );
		}
	}
}
