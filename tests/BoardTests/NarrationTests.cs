using System.Collections.Generic;
using System.Linq;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>
	/// Saying where a figure goes, switching the board AI off, and turning an
	/// argument at the table into a test.
	/// </summary>
	public static class NarrationTests
	{
		private static BoardModel Open( int w = 14, int h = 8 )
		{
			var b = new BoardModel();
			for ( int c = 0; c < w; c++ )
				for ( int r = 0; r < h; r++ )
					b.SetSquare( new Sq( c, r ), SquareFlags.None );
			return b;
		}

		private static List<EnemyFigure> One( Sq at, AttackKind kind = AttackKind.Ranged )
			=> new List<EnemyFigure>
			{
				new EnemyFigure { Id = "e1", Name = "Stormtrooper #1", Position = at,
					Speed = 4, AttackKind = kind },
			};

		private static List<TargetCandidate> Hero( Sq at )
			=> new List<TargetCandidate>
			{
				new TargetCandidate { Id = "H1", Name = "Diala", Position = at, MaxHealth = 12 },
			};

		public static void Register()
		{
			Suite( "narration" );

			Test( "row grows SOUTH and column grows east", () =>
			{
				// Stated and pinned rather than inferred. The mission data has
				// entity Y growing downward on screen, mapping to -Z in the
				// world, so a larger row is further south. Getting this
				// backwards would send every figure the wrong way while
				// staying perfectly self-consistent.
				var origin = new Sq( 5, 5 );
				Eq( "south", PlanNarrator.Direction( origin, new Sq( 5, 7 ) ), "larger row" );
				Eq( "north", PlanNarrator.Direction( origin, new Sq( 5, 3 ) ), "smaller row" );
				Eq( "east", PlanNarrator.Direction( origin, new Sq( 8, 5 ) ), "larger column" );
				Eq( "west", PlanNarrator.Direction( origin, new Sq( 2, 5 ) ), "smaller column" );
				Eq( "south-east", PlanNarrator.Direction( origin, new Sq( 7, 7 ) ), "both" );
				Eq( "north-west", PlanNarrator.Direction( origin, new Sq( 3, 3 ) ), "both again" );
			} );

			Test( "a square is described against the nearest thing on the table", () =>
			{
				var marks = new[]
				{
					new Landmark( "crate", new Sq( 6, 4 ) ),
					new Landmark( "terminal", new Sq( 12, 4 ) ),
				};

				Eq( "on the crate", PlanNarrator.Where( new Sq( 6, 4 ), marks ), "standing on it" );
				Eq( "1 square east of the crate",
					PlanNarrator.Where( new Sq( 7, 4 ), marks ), "beside it" );
				Eq( "2 squares north of the crate",
					PlanNarrator.Where( new Sq( 6, 2 ), marks ), "and further" );
				Eq( "1 square west of the terminal",
					PlanNarrator.Where( new Sq( 11, 4 ), marks ), "the nearer landmark wins" );
			} );

			Test( "a landmark too far away is not used", () =>
			{
				// "Nine squares north-west of the terminal" is harder to follow
				// than the tile the figure is standing on.
				var marks = new[] { new Landmark( "crate", new Sq( 0, 0 ) ) };
				var where = PlanNarrator.Where( new Sq( 11, 7 ), marks );
				False( where.Contains( "crate" ), "the distant crate is not named: " + where );
			} );

			Test( "with no landmark it names the tile, and only then the coordinates", () =>
			{
				var tiles = new Dictionary<Sq, BoardBuilder.TileRef>
				{
					[new Sq( 4, 4 )] = new BoardBuilder.TileRef
					{ Expansion = "Core", TileId = "12", Side = "B" },
				};

				Eq( "on tile Core 12B", PlanNarrator.Where( new Sq( 4, 4 ), null, tiles ),
					"the tile is something the players can see" );

				var raw = PlanNarrator.Where( new Sq( 9, 2 ), null, tiles );
				True( raw.Contains( "9,2" ), "and coordinates are the last resort: " + raw );
			} );

			Test( "an order reads as an instruction, not a coordinate list", () =>
			{
				// Close enough that a melee figure with one move action can
				// actually reach: 4 movement points, so the hero has to be
				// within reach of an adjacent square.
				var b = Open();
				var marks = new[] { new Landmark( "crate", new Sq( 5, 5 ) ) };
				var plan = ActivationPlanner.Plan( b, One( new Sq( 2, 4 ), AttackKind.Melee ),
					Hero( new Sq( 6, 4 ) ) );
				True( plan.Figures[0].WillAttack, "the setup must produce an attack to test one" );

				var line = PlanNarrator.Narrate( plan.Figures[0], marks );
				True( line.StartsWith( "Stormtrooper #1:" ), "it names the figure: " + line );
				True( line.Contains( "crate" ), "and anchors to the crate: " + line );
				True( line.Contains( "attack Diala" ), "and says what it does: " + line );
				False( line.Contains( "(" ) && line.Contains( "," ) && line.Contains( "square 9" ),
					"without falling back to coordinates: " + line );
			} );

			Test( "a figure that cannot attack says so plainly", () =>
			{
				var b = Open( 24, 8 );
				var plan = ActivationPlanner.Plan( b,
					One( new Sq( 1, 4 ), AttackKind.Melee ), Hero( new Sq( 22, 4 ) ) );
				var line = PlanNarrator.Narrate( plan.Figures[0] );
				True( line.Contains( "no attack" ), "it is not left ambiguous: " + line );
			} );

			Test( "every figure in a plan gets a line", () =>
			{
				var b = Open();
				var group = new List<EnemyFigure>
				{
					new EnemyFigure { Id = "e1", Name = "A", Position = new Sq( 2, 3 ) },
					new EnemyFigure { Id = "e2", Name = "B", Position = new Sq( 2, 5 ) },
				};
				var plan = ActivationPlanner.Plan( b, group, Hero( new Sq( 9, 4 ) ) );
				Eq( 2, PlanNarrator.Narrate( plan ).Count, "one line per figure" );
			} );

			Suite( "board AI toggle" );

			Test( "Classic mode is a way back, and says why it was taken", () =>
			{
				BoardAiSettings.UseStrictRules();
				True( BoardAiSettings.UseBoardAi, "board orders by default" );

				BoardAiSettings.FallBackToClassic( "terrain for Core 19B looks wrong" );
				False( BoardAiSettings.UseBoardAi, "and off when asked" );
				True( BoardAiSettings.ClassicReason.Contains( "Core 19B" ),
					"carrying the reason, because a silent downgrade is "
					+ "indistinguishable from the feature never working" );

				BoardAiSettings.UseStrictRules();
				Eq( "", BoardAiSettings.ClassicReason, "and clears when turned back on" );
			} );

			Suite( "rules disputes" );

			Test( "a captured dispute rebuilds the board it was captured from", () =>
			{
				// Without this round trip a dispute file is a bug report that
				// ages. With it, the argument becomes a test.
				var b = Open( 8, 5 );
				b.SetSquare( new Sq( 3, 2 ), SquareFlags.Difficult );
				b.SetSquare( new Sq( 4, 2 ), SquareFlags.Blocking );
				b.SetEdge( new Sq( 5, 2 ), EdgeDir.W, EdgeType.Wall );

				var enemies = One( new Sq( 1, 2 ) );
				var rebels = Hero( new Sq( 6, 2 ) );
				var plan = ActivationPlanner.Plan( b, enemies, rebels );

				var snap = DisputeSnapshot.Capture( b, plan, enemies, rebels,
					"CORE1", 3, "the trooper walked through the wall" );

				var rebuilt = snap.ToBoard();
				Eq( b.Count, rebuilt.Count, "every square comes back" );
				Eq( SquareFlags.Difficult, rebuilt.Flags( new Sq( 3, 2 ) ), "difficult survives" );
				Eq( SquareFlags.Blocking, rebuilt.Flags( new Sq( 4, 2 ) ), "so does blocking" );
				Eq( EdgeType.Wall, rebuilt.Edge( new Sq( 5, 2 ), EdgeDir.W ), "and the wall" );
			} );

			Test( "the snapshot carries the argument, not just the geometry", () =>
			{
				var b = Open( 8, 5 );
				var enemies = One( new Sq( 1, 2 ) );
				var rebels = Hero( new Sq( 6, 2 ) );
				var plan = ActivationPlanner.Plan( b, enemies, rebels );
				var snap = DisputeSnapshot.Capture( b, plan, enemies, rebels,
					"CORE1", 3, "that shot had no line of sight" );

				Eq( "CORE1", snap.missionId, "which mission" );
				Eq( 3, snap.round, "which round" );
				True( snap.reason.Contains( "line of sight" ), "what the players said" );
				Eq( 1, snap.orders.Count, "the order that was given" );
				True( snap.trace.Count > 0, "and the reasoning behind it" );
				Eq( 2, snap.figures.Count, "with both figures recorded" );
				True( snap.figures.Any( f => f.hostile ), "the Rebel marked as one" );
			} );

			Test( "a dispute can be captured with no plan at all", () =>
			{
				// The players may stop the game before anything was ordered,
				// and a capture that throws loses the report entirely.
				var b = Open( 5, 5 );
				var snap = DisputeSnapshot.Capture( b, null, null, null, "CORE1", 1,
					"this door should be open" );
				Eq( 25, snap.squares.Count, "the board is still recorded" );
				Eq( 0, snap.orders.Count, "with no orders to show" );
				True( snap.Describe().Contains( "door should be open" ),
					"and the reason survives" );
			} );

			Test( "the whole board is captured, not a window around the argument", () =>
			{
				// Line of sight and pathing depend on geometry well outside the
				// part anybody is looking at, so a cropped board reproduces a
				// different question from the one that was asked.
				var b = Open( 14, 8 );
				var snap = DisputeSnapshot.Capture( b, null, null, null );
				Eq( 14 * 8, snap.squares.Count, "every square" );
			} );
		}
	}
}
