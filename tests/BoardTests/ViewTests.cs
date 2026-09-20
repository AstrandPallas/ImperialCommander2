using System.Linq;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>Board squares mapped into the world space the tiles are drawn in.</summary>
	public static class ViewTests
	{
		public static void Register()
		{
			Suite( "board to world" );

			Test( "one unit per square, matching how tiles are placed", () =>
			{
				// TileRenderer.LoadTile: (entityPosition.X / 10, 0, -entityPosition.Y / 10),
				// and a board square is entityPosition / 10.
				var a = SagaBoardBridge.SquareToWorld( new Sq( 0, 0 ) );
				var b = SagaBoardBridge.SquareToWorld( new Sq( 1, 0 ) );
				var c = SagaBoardBridge.SquareToWorld( new Sq( 0, 1 ) );

				True( System.Math.Abs( (b.x - a.x) - 1f ) < 1e-5,
					"one square east should be one unit along +X" );
				True( System.Math.Abs( (c.z - a.z) + 1f ) < 1e-5,
					"one square south should be one unit along -Z" );
			} );

			Test( "a figure stands in the middle of its space", () =>
			{
				var w = SagaBoardBridge.SquareToWorld( new Sq( 3, 7 ) );
				True( System.Math.Abs( w.x - 3.5f ) < 1e-5, "x should be centred" );
				True( System.Math.Abs( w.z + 7.5f ) < 1e-5, "z should be centred" );
			} );

			Test( "world position round trips back to its square", () =>
			{
				foreach ( var sq in new[]
					{ new Sq( 0, 0 ), new Sq( 12, 5 ), new Sq( 103, 98 ), new Sq( 7, 0 ) } )
				{
					var w = SagaBoardBridge.SquareToWorld( sq );
					Eq( sq, SagaBoardBridge.WorldToSquare( w.x, w.z ),
						"round trip should return the same square" );
				}
			} );

			Test( "a planned path converts to a run of world points", () =>
			{
				// This is what the move animation follows, so it has to keep the
				// planner's order and length exactly.
				var b = new BoardModel();
				for ( int col = 0; col < 6; col++ )
					for ( int row = 0; row < 3; row++ )
						b.SetSquare( new Sq( col, row ), SquareFlags.Normal );

				var enemies = new[]
				{
					new EnemyFigure { Id = "E1", Name = "Trooper", Position = new Sq( 5, 1 ),
						Speed = 4, AttackKind = AttackKind.Ranged },
				};
				var rebels = new[]
				{
					new TargetCandidate { Id = "H1", Name = "Diala", Position = new Sq( 0, 1 ),
						MaxHealth = 10 },
				};

				var plan = ActivationPlanner.Plan( b, enemies, rebels );
				var fp = plan.Figures.Single();
				var points = SagaBoardBridge.PathToWorld( fp.Path, 0.5f );

				Eq( fp.Path.Count, points.Count, "one point per square on the path" );
				True( points.All( p => System.Math.Abs( p.y - 0.5f ) < 1e-5 ),
					"every point should sit at the requested height" );

				var start = SagaBoardBridge.SquareToWorld( fp.Start, 0.5f );
				Eq( start, points.First(), "the run should start where the figure stands" );
				var end = SagaBoardBridge.SquareToWorld( fp.End, 0.5f );
				Eq( end, points.Last(), "and finish where the order sends it" );
			} );

			Test( "an empty or missing path converts to no points", () =>
			{
				Eq( 0, SagaBoardBridge.PathToWorld( null ).Count, "null path" );
				Eq( 0, SagaBoardBridge.PathToWorld( new Sq[0] ).Count, "empty path" );
			} );
		}
	}
}
