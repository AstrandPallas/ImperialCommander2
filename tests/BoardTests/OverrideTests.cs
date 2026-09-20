using System.Collections.Generic;
using System.Linq;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>Overruling the AI mid-activation.</summary>
	public static class OverrideTests
	{
		private static (BoardModel, List<EnemyFigure>, List<TargetCandidate>) Scene()
		{
			var b = new BoardModel();
			for ( int c = 0; c < 10; c++ )
				for ( int r = 0; r < 4; r++ )
					b.SetSquare( new Sq( c, r ), SquareFlags.Normal );

			var enemies = new List<EnemyFigure>
			{
				new EnemyFigure { Id = "E1", Name = "Trooper 1", Position = new Sq( 9, 1 ),
					Speed = 4, AttackKind = AttackKind.Ranged },
				new EnemyFigure { Id = "E2", Name = "Trooper 2", Position = new Sq( 9, 2 ),
					Speed = 4, AttackKind = AttackKind.Ranged },
			};
			var rebels = new List<TargetCandidate>
			{
				new TargetCandidate { Id = "H1", Name = "Diala", Position = new Sq( 1, 1 ),
					MaxHealth = 10 },
				new TargetCandidate { Id = "H2", Name = "Gaarkhan", Position = new Sq( 1, 3 ),
					MaxHealth = 12 },
			};
			return (b, enemies, rebels);
		}

		public static void Register()
		{
			Suite( "overruling the AI" );

			Test( "a placed figure stays where the players put it", () =>
			{
				var (b, enemies, rebels) = Scene();
				var ov = new PlanOverride { Reason = "I had already moved him" };
				ov.PlacedAt["E1"] = new Sq( 4, 0 );

				var plan = ActivationPlanner.Plan( b, enemies, rebels, null, null, ov );
				var e1 = plan.Figures.First( f => f.Figure.Id == "E1" );

				Eq( new Sq( 4, 0 ), e1.End, "the figure should be where the players put it" );
				False( e1.Moved, "and should not be given a move of its own" );
				Eq( 0, e1.MovementSpent, "nor charged movement for it" );
				True( e1.Trace.Any( t => t.Contains( "placed by the players" ) ),
					"the why-trace should say the players placed it" );
				True( e1.Trace.Any( t => t.Contains( "I had already moved him" ) ),
					"and should carry their reason" );
			} );

			Test( "the rest of the group re-plans around the new position", () =>
			{
				// The point of the whole feature: an override must not leave
				// the other figures routed around a square nobody is on.
				var (b, enemies, rebels) = Scene();
				var ov = new PlanOverride();
				ov.PlacedAt["E1"] = new Sq( 4, 0 );

				var plan = ActivationPlanner.Plan( b, enemies, rebels, null, null, ov );
				var e2 = plan.Figures.First( f => f.Figure.Id == "E2" );

				Eq( new Sq( 4, 0 ), enemies.First( e => e.Id == "E1" ).Position,
					"the override should be applied to the figure itself" );
				True( e2.End != new Sq( 4, 0 ),
					"the other figure must not end on the placed figure's square" );
				True( e2.Trace.Count > 0, "and should still explain itself" );
			} );

			Test( "a placed figure is still told whether it can attack", () =>
			{
				// Having moved it themselves, the players want the one answer
				// the app is actually good at: can it shoot from here?
				var (b, enemies, rebels) = Scene();
				var ov = new PlanOverride();
				ov.PlacedAt["E1"] = new Sq( 2, 1 );   // right beside Diala

				var plan = ActivationPlanner.Plan( b, enemies, rebels, null, null, ov );
				var e1 = plan.Figures.First( f => f.Figure.Id == "E1" );

				True( e1.WillAttack, "from an adjacent square it should report an attack" );
				Eq( "H1", e1.Target.Id, "against the target it can actually reach" );
			} );

			Test( "forcing a target overrides the priority chain", () =>
			{
				var (b, enemies, rebels) = Scene();

				var natural = ActivationPlanner.Plan( b, enemies, rebels );
				Eq( "H1", natural.GroupTarget.Chosen.Id,
					"unforced, the chain should pick the closer Rebel" );

				var ov = new PlanOverride
				{ TargetId = "H2", Reason = "mission rule: they must go for Gaarkhan" };
				var forced = ActivationPlanner.Plan( b, enemies, rebels, null, null, ov );

				Eq( "H2", forced.GroupTarget.Chosen.Id, "the forced target should win" );
				False( forced.NeedsPlayerDecision,
					"a forced choice is settled, so it must not also prompt as a tie" );
				True( forced.GroupTarget.Trace.Any( t => t.Contains( "OVERRIDDEN" ) ),
					"the why-trace should record that it was overridden" );
				True( forced.GroupTarget.Trace.Any( t => t.Contains( "mission rule" ) ),
					"and should carry the reason" );
			} );

			Test( "an override naming a figure or target that is gone is ignored", () =>
			{
				// Overrides are entered by hand under time pressure, so a stale
				// or mistyped id has to be harmless.
				var (b, enemies, rebels) = Scene();
				var ov = new PlanOverride { TargetId = "NOBODY" };
				ov.PlacedAt["GHOST"] = new Sq( 5, 5 );

				var plan = ActivationPlanner.Plan( b, enemies, rebels, null, null, ov );
				True( plan.GroupTarget.Chosen != null,
					"planning should carry on with the chain's own answer" );
				Eq( enemies.Count, plan.Figures.Count, "and should still plan every figure" );
				True( plan.GroupTarget.Trace.Any( t => t.Contains( "not in play" ) ),
					"the trace should say the override was ignored, not stay silent" );
			} );

			Test( "planning without an override is unchanged", () =>
			{
				// The override path must not perturb the ordinary one.
				var (b, e1, r1) = Scene();
				var a = ActivationPlanner.Plan( b, e1, r1 );
				var (b2, e2, r2) = Scene();
				var c = ActivationPlanner.Plan( b2, e2, r2, null, null, new PlanOverride() );

				Eq( a.GroupTarget.Chosen.Id, c.GroupTarget.Chosen.Id, "same target" );
				Eq( a.Figures.Count, c.Figures.Count, "same figure count" );
				for ( int i = 0; i < a.Figures.Count; i++ )
					Eq( a.Figures[i].End, c.Figures[i].End, "same orders" );
			} );
		}
	}
}
