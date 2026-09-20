using System.Collections.Generic;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>Mak Eshka'rey's Covert ability, tested against the printed wording.</summary>
	public static class CovertTests
	{
		private static BoardModel Line( int width )
		{
			var b = new BoardModel();
			for ( int c = 0; c < width; c++ )
				b.SetSquare( new Sq( c, 0 ), SquareFlags.None );
			return b;
		}

		public static void Register()
		{
			Suite( "Covert / Mak Eshka'rey (sourced: hero sheet)" );

			Test( "at exactly 4 spaces he cannot be seen, at 3 he can", () =>
			{
				var b = Line( 10 );
				var mak = new FigureVisibility
				{ Id = "MAK", Position = new Sq( 4, 0 ), HiddenAtOrBeyond = 4 };

				// "4 or more spaces away" -- inclusive, so 4 is already hidden.
				Eq( 4, Distance.Count( b, new Sq( 0, 0 ), mak.Position ),
					"fixture should place the observer exactly 4 spaces away" );
				True( mak.IsHiddenFrom( b, new Sq( 0, 0 ) ),
					"a hostile at exactly 4 spaces must NOT have line of sight" );

				Eq( 3, Distance.Count( b, new Sq( 1, 0 ), mak.Position ),
					"fixture should place the observer exactly 3 spaces away" );
				False( mak.IsHiddenFrom( b, new Sq( 1, 0 ) ),
					"a hostile at 3 spaces still sees him" );
			} );

			Test( "beyond 4 he stays hidden", () =>
			{
				var b = Line( 10 );
				var mak = new FigureVisibility
				{ Id = "MAK", Position = new Sq( 9, 0 ), HiddenAtOrBeyond = 4 };
				True( mak.IsHiddenFrom( b, new Sq( 0, 0 ) ), "9 spaces is more than 4" );
			} );

			Test( "he stops blocking line of sight for figures that cannot see him", () =>
			{
				// "You do not block line of sight for those figures." The
				// consequence is that the blocker set is PER OBSERVER: Mak
				// screens a near shooter and not a far one, from the very same
				// square.
				var b = Line( 12 );
				var mak = new FigureVisibility
				{ Id = "MAK", Position = new Sq( 5, 0 ), HiddenAtOrBeyond = 4 };
				var figures = new List<FigureVisibility> { mak };

				var near = new Sq( 3, 0 );    // 2 spaces from Mak: sees him, is blocked by him
				var far = new Sq( 0, 0 );     // 5 spaces from Mak: cannot see him, is not blocked

				True( Visibility.BlockersFor( b, near, figures ).Contains( mak.Position ),
					"a shooter close enough to see Mak is still screened by him" );
				False( Visibility.BlockersFor( b, far, figures ).Contains( mak.Position ),
					"a shooter that cannot see Mak is not screened by him either" );
			} );

			Test( "an ordinary figure blocks for everyone regardless of distance", () =>
			{
				// Control case. Without this, the test above would pass just as
				// well if the blocker set were empty for every observer.
				var b = Line( 12 );
				var plain = new FigureVisibility { Id = "P", Position = new Sq( 5, 0 ) };
				var figures = new List<FigureVisibility> { plain };

				True( Visibility.BlockersFor( b, new Sq( 3, 0 ), figures )
						.Contains( plain.Position ),
					"an ordinary figure screens a near shooter" );
				True( Visibility.BlockersFor( b, new Sq( 0, 0 ), figures )
						.Contains( plain.Position ),
					"and screens a far one too" );
			} );
		}
	}
}
