using System.Collections.Generic;
using System.Linq;
using Saga.Tracking;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>Tracked state in, planner inputs out, and orders back again.</summary>
	public static class TrackerBridgeTests
	{
		private static BoardModel Board( int w = 12, int h = 6 )
		{
			var b = new BoardModel();
			for ( int c = 0; c < w; c++ )
				for ( int r = 0; r < h; r++ )
					b.SetSquare( new Sq( c, r ), SquareFlags.Normal );
			return b;
		}

		private static GroupCombatState Group( string id, params Sq[] at )
		{
			var g = GroupCombatState.Create( id, "DG001", "Stormtrooper", at.Length, 3 );
			for ( int i = 0; i < at.Length; i++ )
				TrackerBridge.SetFigurePosition( g, i, at[i] );
			return g;
		}

		private static HeroCombatState Hero( string id, string name, Sq at, int round = 1 )
		{
			var h = new HeroCombatState { CardId = id, Name = name, MaxHealth = 10 };
			TrackerBridge.SetHeroPosition( h, at, round );
			return h;
		}

		public static void Register()
		{
			Suite( "tracker to planner" );

			Test( "a tracked group and heroes become a plannable snapshot", () =>
			{
				var group = Group( "g1", new Sq( 9, 2 ), new Sq( 9, 3 ) );
				var heroes = new[] { Hero( "H1", "Diala", new Sq( 1, 2 ) ) };

				var snap = TrackerBridge.Snapshot( group, heroes, null, 1 );
				True( snap.CanPlan, "snapshot should be plannable" );
				Eq( 2, snap.Enemies.Count, "both living figures should be present" );
				Eq( 1, snap.Rebels.Count, "the hero should be a target" );
				Eq( 0, snap.Gaps.Count, "nothing should be missing" );

				var plan = ActivationPlanner.Plan( Board(), snap.Enemies, snap.Rebels );
				Eq( 2, plan.Figures.Count, "the planner should order both figures" );
				Eq( "H1", plan.GroupTarget.Chosen.Id, "and target the only hero" );
			} );

			Test( "a figure with no position is reported, not guessed at", () =>
			{
				// The app reads the board; it must not invent a position for a
				// figure the players have not placed.
				var group = Group( "g1", new Sq( 9, 2 ) );
				group.Figures.Add( new FigureSlot { Index = 1, Alive = true } );
				group.FiguresAlive = 2;

				var snap = TrackerBridge.Snapshot( group, new[] { Hero( "H1", "D", new Sq( 1, 2 ) ) } );
				Eq( 1, snap.Enemies.Count, "only the placed figure should be planned" );
				Eq( 1, snap.Gaps.Count, "the unplaced one should be reported" );
				True( snap.Gaps[0].Reason.Contains( "no position" ), "with a usable reason" );
			} );

			Test( "a stale hero position is used, and flagged", () =>
			{
				// Never refuse to answer -- but say the answer rests on old
				// information, so the plan can carry a warning.
				var group = Group( "g1", new Sq( 9, 2 ) );
				var hero = Hero( "H1", "Diala", new Sq( 1, 2 ), round: 1 );

				var fresh = TrackerBridge.Snapshot( group, new[] { hero }, null, 2 );
				Eq( 0, fresh.Gaps.Count, "one round old is not stale" );

				var stale = TrackerBridge.Snapshot( group, new[] { hero }, null, 5 );
				Eq( 1, stale.Rebels.Count, "the hero is still planned against" );
				Eq( 1, stale.Gaps.Count, "but the staleness is reported" );
				True( stale.Gaps[0].Reason.Contains( "rounds old" ), "with the age" );
			} );

			Test( "withdrawn and dead figures drop out", () =>
			{
				var group = Group( "g1", new Sq( 9, 2 ), new Sq( 9, 3 ) );
				group.Figures[1].Alive = false;

				var withdrawn = Hero( "H2", "Gaarkhan", new Sq( 2, 2 ) );
				withdrawn.IsWithdrawn = true;

				var snap = TrackerBridge.Snapshot( group,
					new[] { Hero( "H1", "Diala", new Sq( 1, 2 ) ), withdrawn } );
				Eq( 1, snap.Enemies.Count, "the dead figure should not be ordered" );
				Eq( 1, snap.Rebels.Count, "the withdrawn hero should not be targeted" );
			} );

			Test( "other groups on the board block sight without being ordered", () =>
			{
				var activating = Group( "g1", new Sq( 9, 2 ) );
				var bystander = Group( "g2", new Sq( 5, 2 ), new Sq( 5, 3 ) );

				var snap = TrackerBridge.Snapshot( activating,
					new[] { Hero( "H1", "Diala", new Sq( 1, 2 ) ) },
					new[] { bystander } );

				Eq( 1, snap.Enemies.Count, "only the activating group is ordered" );
				True( snap.Visibility.Any( v => v.Position == new Sq( 5, 2 ) ),
					"but the bystanders still occupy squares for line of sight" );
				True( snap.Visibility.Any( v => v.Position == new Sq( 9, 2 ) ),
					"and so does the activating figure" );
			} );

			Test( "a Stunned group is planned with one action", () =>
			{
				var group = Group( "g1", new Sq( 9, 2 ) );
				group.AddCondition( Condition.Stunned );

				var snap = TrackerBridge.Snapshot( group,
					new[] { Hero( "H1", "Diala", new Sq( 1, 2 ) ) } );
				True( snap.Enemies[0].Stunned, "the condition should reach the planner" );

				var plan = ActivationPlanner.Plan( Board(), snap.Enemies, snap.Rebels );
				Eq( 1, plan.Figures[0].ActionsAvailable,
					"Stunned costs an action, so only one remains" );
			} );

			Test( "committing a plan moves the tracked figures", () =>
			{
				// The loop that makes the whole thing work: tracker -> plan ->
				// players move the minis -> tracker.
				var group = Group( "g1", new Sq( 9, 2 ), new Sq( 9, 3 ) );
				var heroes = new[] { Hero( "H1", "Diala", new Sq( 1, 2 ) ) };
				var snap = TrackerBridge.Snapshot( group, heroes );
				var plan = ActivationPlanner.Plan( Board(), snap.Enemies, snap.Rebels );

				Eq( 2, TrackerBridge.Commit( plan, group ), "both figures should be committed" );

				foreach ( var fp in plan.Figures )
				{
					TrackerBridge.TryParseFigureId( fp.Figure.Id, out _, out int index );
					var slot = group.Figures.First( f => f.Index == index );
					Eq( fp.End.C, slot.PosC.Value, "tracked column should match the order" );
					Eq( fp.End.R, slot.PosR.Value, "tracked row should match the order" );
				}

				// And the next snapshot starts from where they now stand.
				var next = TrackerBridge.Snapshot( group, heroes );
				True( next.Enemies.All( e => plan.Figures.Any( f => f.End == e.Position ) ),
					"the following activation should begin from the committed squares" );
			} );

			Test( "figure ids round trip", () =>
			{
				// The id is the only thing tying a tracker slot, a planned
				// order and a token on screen together.
				var id = TrackerBridge.FigureId( "abc-123", 2 );
				True( TrackerBridge.TryParseFigureId( id, out var instance, out int index ),
					"a generated id should parse" );
				Eq( "abc-123", instance, "instance should survive" );
				Eq( 2, index, "index should survive" );
				False( TrackerBridge.TryParseFigureId( "nonsense", out _, out _ ),
					"a malformed id should be rejected rather than half-parsed" );
			} );
		}
	}
}
