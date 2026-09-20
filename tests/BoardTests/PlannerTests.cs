using System.Collections.Generic;
using System.Linq;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>End-to-end activation planning: the orders a player would read.</summary>
	public static class PlannerTests
	{
		private static EnemyFigure Enemy( string id, Sq at, int speed = 4,
			AttackKind kind = AttackKind.Ranged, bool stunned = false )
			=> new EnemyFigure { Id = id, Name = id, Position = at, Speed = speed,
				AttackKind = kind, Stunned = stunned };

		private static TargetCandidate Hero( string name, Sq at, int max = 10, int dmg = 0 )
			=> new TargetCandidate { Id = name, Name = name, Position = at,
				MaxHealth = max, Damage = dmg };

		public static void Register()
		{
			Suite( "activation planning" );

			Test( "a figure already in contact holds position and attacks", () =>
			{
				// Melee, already adjacent: there is no better square to move to,
				// so the planner must not shuffle for the sake of it.
				var f = Fixture.Parse( @"
+-+-+
|E H|
+-+-+" );
				var plan = ActivationPlanner.Plan( f.Board,
					new[] { Enemy( "E", f.Marker( 'E' ), speed: 4, kind: AttackKind.Melee ) },
					new[] { Hero( "Hero", f.Marker( 'H' ) ) } );

				Eq( 1, plan.Figures.Count, "one figure planned" );
				var fp = plan.Figures[0];
				True( fp.WillAttack, "declares an attack" );
				Eq( 0, fp.MovementSpent, "spends no movement it does not need" );
				False( fp.Moved, "holds position" );
			} );

			Test( "a melee figure closes to contact before attacking", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+-+
|E . . . H|
+-+-+-+-+-+" );
				var plan = ActivationPlanner.Plan( f.Board,
					new[] { Enemy( "E", f.Marker( 'E' ), speed: 4, kind: AttackKind.Melee ) },
					new[] { Hero( "Hero", f.Marker( 'H' ) ) } );

				var fp = plan.Figures[0];
				True( fp.WillAttack, "reaches contact and attacks" );
				True( f.Board.AreAdjacent( fp.End, f.Marker( 'H' ) ), "ends adjacent to the hero" );
				True( fp.Moved, "had to move" );
			} );

			Test( "a ranged figure prefers the easiest shot over the shortest walk", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+-+-+-+
|E . . . . . H|
+-+-+-+-+-+-+-+" );
				var plan = ActivationPlanner.Plan( f.Board,
					new[] { Enemy( "E", f.Marker( 'E' ), speed: 4 ) },
					new[] { Hero( "Hero", f.Marker( 'H' ) ) } );

				var fp = plan.Figures[0];
				True( fp.WillAttack, "attacks" );
				True( fp.Attack.RequiredAccuracy < 6,
					$"closed the distance, needs accuracy {fp.Attack.RequiredAccuracy}" );
			} );

			Test( "a Stunned figure has only one action", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+-+
|E . . . H|
+-+-+-+-+-+" );
				var normal = ActivationPlanner.Plan( f.Board,
					new[] { Enemy( "E", f.Marker( 'E' ), speed: 4, kind: AttackKind.Melee ) },
					new[] { Hero( "Hero", f.Marker( 'H' ) ) } ).Figures[0];
				var stunned = ActivationPlanner.Plan( f.Board,
					new[] { Enemy( "E", f.Marker( 'E' ), speed: 4, kind: AttackKind.Melee, stunned: true ) },
					new[] { Hero( "Hero", f.Marker( 'H' ) ) } ).Figures[0];

				Eq( 2, normal.ActionsAvailable, "two actions normally" );
				Eq( 1, stunned.ActionsAvailable, "one when Stunned" );
				True( stunned.Trace.Any( t => t.Contains( "Stunned" ) ), "says why" );
				True( normal.WillAttack, "unstunned figure reaches contact" );
				False( stunned.WillAttack, "stunned figure cannot both move and attack" );
			} );

			Test( "figures are planned one at a time and path around each other", () =>
			{
				// A single-width corridor: the second figure must not be routed
				// through the square the first has just committed to.
				var f = Fixture.Parse( @"
+-+-+-+-+-+
|A B . . H|
+-+-+-+-+-+" );
				var plan = ActivationPlanner.Plan( f.Board,
					new[]
					{
						Enemy( "First", f.Marker( 'B' ), speed: 4, kind: AttackKind.Melee ),
						Enemy( "Second", f.Marker( 'A' ), speed: 4, kind: AttackKind.Melee ),
					},
					new[] { Hero( "Hero", f.Marker( 'H' ) ) } );

				Eq( 2, plan.Figures.Count, "both planned" );
				var ends = plan.Figures.Select( p => p.End ).ToList();
				Eq( ends.Count, ends.Distinct().Count(), "no two figures ordered onto the same square" );
				Eq( "First", plan.Figures[0].Figure.Name, "nearest figure activates first" );
			} );

			Test( "an unreachable target produces an advance, not a fabricated attack", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+-+
|E . .|. H|
+-+-+-+-+-+" );
				var plan = ActivationPlanner.Plan( f.Board,
					new[] { Enemy( "E", f.Marker( 'E' ), speed: 2, kind: AttackKind.Melee ) },
					new[] { Hero( "Hero", f.Marker( 'H' ) ) } );

				var fp = plan.Figures[0];
				False( fp.WillAttack, "does not invent an attack it cannot make" );
				True( fp.Trace.Any( t => t.Contains( "advances" ) || t.Contains( "cannot reach" ) ),
					"explains itself" );
			} );

			Test( "the group commits to one target and says which rule chose it", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+
|E . B .|
+ + + + +
|. . . C|
+-+-+-+-+" );
				var near = Hero( "Near", f.Marker( 'B' ) );
				var far = Hero( "Far", f.Marker( 'C' ) );
				var plan = ActivationPlanner.Plan( f.Board,
					new[] { Enemy( "E", f.Marker( 'E' ) ) },
					new[] { near, far } );

				Eq( "Near", plan.GroupTarget.Chosen.Name, "closest healthy" );
				True( plan.GroupTarget.Rule.StartsWith( "1" ), "rule recorded: " + plan.GroupTarget.Rule );
				True( plan.GroupTarget.Trace.Count > 0, "why-trace present" );
			} );

			Test( "a figure hidden by an ability is not planned against", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+-+-+
|E . . . . H|
+-+-+-+-+-+-+" );
				var hero = Hero( "Mak", f.Marker( 'H' ) );
				var vis = new List<FigureVisibility>
				{
					new FigureVisibility { Id = "E", Position = f.Marker( 'E' ) },
					new FigureVisibility { Id = "Mak", Position = f.Marker( 'H' ), HiddenAtOrBeyond = 4 },
				};
				var plan = ActivationPlanner.Plan( f.Board,
					new[] { Enemy( "E", f.Marker( 'E' ), speed: 1 ) },
					new[] { hero }, null, vis );

				var fp = plan.Figures[0];
				False( fp.WillAttack,
					"cannot shoot a figure its ability removes from line of sight at this range" );
			} );

			Test( "every figure plan carries a readable order and a trace", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|E . H|
+-+-+-+" );
				var plan = ActivationPlanner.Plan( f.Board,
					new[] { Enemy( "E", f.Marker( 'E' ) ) },
					new[] { Hero( "Hero", f.Marker( 'H' ) ) } );
				var fp = plan.Figures[0];
				True( fp.ToString().Length > 0, "renders an order" );
				True( fp.Trace.Count > 0, "carries a trace" );
				True( fp.Path.Count >= 1, "carries a path" );
			} );

			Test( "a sealed-off target still produces an advance, not a stall", () =>
			{
				// Regression. When a closed door seals the target off, counting
				// spaces to it returns "no route" from EVERY candidate square, so
				// an advance loop that requires a countable distance rejects them
				// all and the group stands still for the whole mission. Falling
				// back to straight-line distance keeps them walking to the door.
				var f = Fixture.Parse( @"
+-+-+-+-+-+-+-+
|E . . .D. . H|
+-+-+-+-+-+-+-+" );
				var plan = ActivationPlanner.Plan( f.Board,
					new[] { Enemy( "E", f.Marker( 'E' ), speed: 4, kind: AttackKind.Melee ) },
					new[] { Hero( "Hero", f.Marker( 'H' ) ) } );

				var fp = plan.Figures[0];
				False( fp.WillAttack, "no attack is possible through a closed door" );
				True( fp.Moved, "must not stall" );
				True( fp.MovementSpent > 0, "spends movement closing on the door" );
				True( Sq.Chebyshev( fp.End, f.Marker( 'H' ) ) < Sq.Chebyshev( fp.Start, f.Marker( 'H' ) ),
					"ends nearer the target than it started" );
				True( fp.Trace.Any( t => t.Contains( "sealed off" ) ), "explains the fallback" );
			} );

		}
	}
}
