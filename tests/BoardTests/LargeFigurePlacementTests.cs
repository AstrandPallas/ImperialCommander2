using System.Collections.Generic;
using System.Linq;
using Saga.Tracking;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>
	/// Seating a figure whose base is larger than one square, on the board it
	/// actually failed on.
	/// </summary>
	public static class LargeFigurePlacementTests
	{
		private static BoardModel Core1()
			=> BoardBuilder.Build(
				Core1Placements.Tiles, Core1Placements.Lookup,
				Core1Placements.Doors.Select( d => new DoorPlacement
				{ X = d.X, Y = d.Y, Rotation = d.Rotation, Open = false } ).ToArray(),
				Core1Placements.Library() ).Board;

		public static void Register()
		{
			Suite( "large figure placement" );

			Test( "the Nexu is seated where its whole 2x2 base fits", () =>
			{
				// The real failure: DP Nexu in CORE1 is (97,108), and (98,108)
				// -- one of the four squares a 2x2 anchored there would cover --
				// does not exist. Seated as a 1x1 the Nexu looked fine and
				// could never move, because no plan could fit its base anywhere.
				var b = Core1();
				var dp = new Sq( 97, 108 );
				True( b.IsEnterable( dp ), "the deployment square itself is fine" );
				False( HeroPlacement.Fits( b, dp, Footprint.Large2x2 ),
					"but a 2x2 base anchored on it runs off the tile" );

				var placed = DeploymentPlanner.PlaceGroup( b, dp, 1, null, null, Footprint.Large2x2 );
				Eq( 1, placed.Count, "the Nexu is still seated" );
				True( HeroPlacement.Fits( b, placed[0], Footprint.Large2x2 ),
					"on an anchor where all four squares of its base exist: " + placed[0] );
				True( Sq.Chebyshev( placed[0], dp ) <= 2, "and close to the point it was meant for" );
			} );

			Test( "and can then actually move", () =>
			{
				var b = Core1();
				var anchor = DeploymentPlanner.PlaceGroup( b, new Sq( 97, 108 ), 1,
					null, null, Footprint.Large2x2 )[0];

				var nexu = new List<EnemyFigure>
				{
					new EnemyFigure { Id = "n", Name = "Nexu", Position = anchor, Speed = 6,
						AttackKind = AttackKind.Melee, Footprint = Footprint.Large2x2, Mobile = true },
				};
				var diala = new List<TargetCandidate>
				{
					new TargetCandidate { Id = "H1", Name = "Diala", Position = new Sq( 98, 100 ), MaxHealth = 12 },
				};
				var plan = ActivationPlanner.Plan( b, nexu, diala );
				True( plan.Figures[0].Moved,
					"it advances rather than holding: " + string.Join( " | ", plan.Figures[0].Trace ) );
				False( plan.Figures[0].Trace.Any( t => t.Contains( "does not fit" ) ),
					"and its base fits at the start" );
			} );

			Test( "a large figure's whole base counts as occupied", () =>
			{
				var group = GroupCombatState.Create( "g", "DG018", "Nexu", 1, 9, true );
				group.Profile = UnitProfile.From( "Melee", "Large2x2", 6, new[] { "Mobile" } );
				TrackerBridge.SetFigurePosition( group, 0, new Sq( 5, 5 ) );

				var taken = HeroPlacement.OccupiedSquares( null, new[] { group } );
				Eq( 4, taken.Count, "four squares, not one" );
				True( taken.Contains( new Sq( 6, 6 ) ), "including the far corner of the base" );
			} );

			Test( "dragging a large figure snaps its whole base onto the board", () =>
			{
				var b = Core1();
				var snapped = HeroPlacement.Snap( b, new Sq( 97, 108 ), null, 3, Footprint.Large2x2 );
				True( snapped.HasValue, "somewhere nearby takes a 2x2" );
				True( HeroPlacement.Fits( b, snapped.Value, Footprint.Large2x2 ), "all four squares" );
			} );

			Test( "a 2x2 token is drawn on the corner its four squares share", () =>
			{
				var (x, _, z) = SagaBoardBridge.FootprintCenter( new Sq( 4, 4 ), Footprint.Large2x2 );
				Eq( 5f, x, "centre column" );
				Eq( -5f, z, "centre row, negative z" );
				var (sx, _, sz) = SagaBoardBridge.FootprintCenter( new Sq( 4, 4 ), Footprint.Small1x1 );
				Eq( 4.5f, sx, "a 1x1 is still the square's centre" );
				Eq( -4.5f, sz, "" );
			} );
		}
	}
}
