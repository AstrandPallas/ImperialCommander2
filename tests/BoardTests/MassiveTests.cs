using System.Collections.Generic;
using System.Linq;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>The Massive keyword, tested against the printed wording.</summary>
	public static class MassiveTests
	{
		/// <summary>An open board with one difficult square and one blocking.</summary>
		private static BoardModel Board()
		{
			var b = new BoardModel();
			for ( int c = 0; c < 6; c++ )
				for ( int r = 0; r < 3; r++ )
					b.SetSquare( new Sq( c, r ), SquareFlags.None );
			b.SetSquare( new Sq( 2, 1 ), SquareFlags.Difficult );
			b.SetSquare( new Sq( 3, 1 ), SquareFlags.Blocking );
			return b;
		}

		public static void Register()
		{
			Suite( "Massive (sourced: Consolidated Rules p.41)" );

			Test( "enters difficult terrain at no additional movement cost", () =>
			{
				// "can enter spaces containing hostile figures and/or difficult
				//  terrain at no additional movement cost"
				var b = Board();
				var normal = Pathfinder.Compute( b, new Sq( 1, 1 ), 6 );
				var massive = Pathfinder.Compute( b, new Sq( 1, 1 ), 6,
					new MoveOptions { Massive = true } );

				Eq( 2, normal.CostTo( new Sq( 2, 1 ) ),
					"an ordinary figure pays 2 for difficult terrain" );
				Eq( 1, massive.CostTo( new Sq( 2, 1 ) ),
					"a Massive figure pays no additional cost for difficult terrain" );
			} );

			Test( "enters a hostile figure's space at no additional movement cost", () =>
			{
				// Same sentence as above: hostile figures cost nothing extra.
				var b = Board();
				var foe = new Sq( 2, 0 );
				var opt = new MoveOptions { Hostile = s => s == foe };
				var massiveOpt = new MoveOptions { Hostile = s => s == foe, Massive = true };

				var normal = Pathfinder.Compute( b, new Sq( 1, 0 ), 6, opt );
				var massive = Pathfinder.Compute( b, new Sq( 1, 0 ), 6, massiveOpt );

				Eq( 2, normal.CostTo( foe ), "an ordinary figure pays the hostile toll" );
				Eq( 1, massive.CostTo( foe ), "a Massive figure pays no hostile toll" );
			} );

			Test( "may end its movement on blocking terrain", () =>
			{
				// "A Massive figure can end its movement in spaces that contain
				//  blocking terrain and/or other figures."
				var b = Board();
				var opt = new MoveOptions { Massive = true };
				var reach = Pathfinder.Compute( b, new Sq( 1, 1 ), 6, opt );
				var ends = reach.EndSquares( Pathfinder.CanEndOn( b, opt ) ).ToList();

				True( ends.Contains( new Sq( 3, 1 ) ),
					"blocking terrain must be a legal end square for a Massive figure" );

				var plainOpt = new MoveOptions();
				var plain = Pathfinder.Compute( b, new Sq( 1, 1 ), 6, plainOpt );
				False( plain.EndSquares( Pathfinder.CanEndOn( b, plainOpt ) )
						.Contains( new Sq( 3, 1 ) ),
					"blocking terrain must NOT be a legal end square for anyone else" );
			} );

			Test( "cannot enter a space containing another Massive figure", () =>
			{
				// "Massive figures cannot enter spaces containing other Massive
				//  figures." This is the one restriction Massive adds rather
				//  than removes, so it cannot be folded into "ignores terrain".
				var b = Board();
				var otherMassive = new Sq( 3, 0 );
				var opt = new MoveOptions
				{
					Massive = true,
					OtherMassive = s => s == otherMassive,
				};
				var reach = Pathfinder.Compute( b, new Sq( 1, 0 ), 6, opt );

				Eq( -1, reach.CostTo( otherMassive ),
					"a Massive figure may not enter another Massive figure's space" );
				True( reach.CostTo( new Sq( 4, 0 ) ) > 0,
					"but the board beyond it is still reachable by going around" );
			} );

			Test( "figures do not block line of sight to or from a Massive figure", () =>
			{
				// "Figures do not block line of sight to or from a Massive
				//  figure." Note this is a property of the ENDPOINT, not of the
				//  blocker, which is why it cannot be expressed by clearing
				//  BlocksLineOfSight on the figure in the way.
				var b = Board();
				var observer = new Sq( 0, 0 );
				var blocker = new FigureVisibility { Id = "B", Position = new Sq( 1, 0 ) };

				var ordinary = new FigureVisibility { Id = "T", Position = new Sq( 2, 0 ) };
				False( Visibility.CanSee( b, observer, ordinary,
						new List<FigureVisibility> { blocker } ),
					"an ordinary target is screened by the figure between" );

				var massive = new FigureVisibility
				{ Id = "M", Position = new Sq( 2, 0 ), Massive = true };
				True( Visibility.CanSee( b, observer, massive,
						new List<FigureVisibility> { blocker } ),
					"a Massive target is not screened by figures" );
			} );

			Test( "a Massive figure on blocking terrain can still be seen and attacked", () =>
			{
				// "If a Massive figure occupies a space containing blocking
				//  terrain, line of sight can be traced to that figure, spaces
				//  can be counted to that figure, and adjacent figures can
				//  attack that figure."
				var b = Board();
				var onBlocking = new Sq( 3, 1 );
				var shooter = new Sq( 4, 1 );

				var target = new FigureVisibility
				{ Id = "M", Position = onBlocking, Massive = true };
				True( Visibility.CanSee( b, shooter, target, null ),
					"line of sight must reach a Massive figure standing on blocking terrain" );
				True( b.AreAdjacent( shooter, onBlocking ),
					"an adjacent figure must be able to attack it" );
				Eq( 1, Distance.Count( b, shooter, onBlocking, 60,
						targetOnBlockingIsCountable: true ),
					"spaces must be countable to a Massive figure on blocking terrain" );
				Eq( -1, Distance.Count( b, shooter, onBlocking ),
					"but blocking terrain with nobody on it stays uncountable" );

				// And the exemption must not open a route THROUGH the blocking
				// square to something on the far side of it.
				Eq( 2, Distance.Count( b, new Sq( 2, 1 ), new Sq( 4, 1 ) ),
					"counting still detours around blocking terrain" );
			} );
		}
	}
}
