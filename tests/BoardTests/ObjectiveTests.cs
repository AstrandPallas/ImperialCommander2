using System.Collections.Generic;
using System.Linq;
using Saga.Tracking;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>
	/// Objectives as a positioning preference: they decide between squares the
	/// planner already considers equally good, and nothing else.
	/// </summary>
	public static class ObjectiveTests
	{
		private static BoardModel Open( int w = 14, int h = 7 )
		{
			var b = new BoardModel();
			for ( int c = 0; c < w; c++ )
				for ( int r = 0; r < h; r++ )
					b.SetSquare( new Sq( c, r ), SquareFlags.None );
			return b;
		}

		private static List<EnemyFigure> One( Sq at, AttackKind kind = AttackKind.Ranged,
			int speed = 4 )
			=> new List<EnemyFigure>
			{
				new EnemyFigure { Id = "e1", Name = "Trooper", Position = at,
					Speed = speed, AttackKind = kind },
			};

		private static List<TargetCandidate> Hero( Sq at, int health = 10 )
			=> new List<TargetCandidate>
			{
				new TargetCandidate { Id = "H1", Name = "Diala", Position = at,
					MaxHealth = health },
			};

		public static void Register()
		{
			Suite( "objectives" );

			Test( "contesting is scored on the token, then beside it, then not at all", () =>
			{
				var map = new ObjectiveMap();
				map.Add( new Sq( 5, 5 ) );
				Eq( 2, map.Contest( new Sq( 5, 5 ) ), "standing on it" );
				Eq( 1, map.Contest( new Sq( 5, 6 ) ), "beside it" );
				Eq( 1, map.Contest( new Sq( 6, 6 ) ), "diagonally beside it" );
				Eq( 0, map.Contest( new Sq( 5, 8 ) ), "and nowhere near it" );
			} );

			Test( "an empty map scores nothing anywhere", () =>
			{
				Eq( 0, ObjectiveMap.Empty.Contest( new Sq( 1, 1 ) ), "nothing to contest" );
				Eq( -1, ObjectiveMap.Empty.DistanceToNearest( new Sq( 1, 1 ) ),
					"and no distance to report" );
			} );

			Test( "resolved tokens stop being objectives", () =>
			{
				// Once a crate is opened, standing on it achieves nothing.
				var tokens = new List<MissionTokenState>
				{
					new MissionTokenState { Name = "crate A", Kind = TokenKind.Crate,
						PosC = 3, PosR = 3 },
					new MissionTokenState { Name = "crate B", Kind = TokenKind.Crate,
						PosC = 6, PosR = 3, State = "opened" },
					new MissionTokenState { Name = "terminal", Kind = TokenKind.Terminal,
						PosC = 8, PosR = 3 },
					new MissionTokenState { Name = "a door", Kind = TokenKind.Door,
						PosC = 9, PosR = 3 },
					new MissionTokenState { Name = "unplaced", Kind = TokenKind.Objective },
				};

				var map = TrackerBridge.ObjectivesFrom( tokens );
				True( map.IsObjective( new Sq( 3, 3 ) ), "the unopened crate counts" );
				False( map.IsObjective( new Sq( 6, 3 ) ), "the opened one does not" );
				True( map.IsObjective( new Sq( 8, 3 ) ), "the terminal counts" );
				False( map.IsObjective( new Sq( 9, 3 ) ), "a door is not an objective" );
				Eq( 2, map.Count, "and a token with no position cannot be stood on" );
			} );

			Test( "an objective breaks a tie that was previously settled by column", () =>
			{
				// A melee attacker needs no accuracy, so every square adjacent
				// to the hero is an equally good shot and the choice comes
				// down to movement. From (6,2) the squares (5,5), (6,5) and
				// (7,5) all cost 3, which used to be settled by taking the
				// lowest column -- that is, by nothing.
				var b = Open();
				var plain = ActivationPlanner.Plan( b,
					One( new Sq( 6, 2 ), AttackKind.Melee ), Hero( new Sq( 6, 6 ) ) );

				Eq( new Sq( 5, 5 ), plain.Figures[0].End, "the lowest column wins by default" );
				Eq( 3, plain.Figures[0].MovementSpent, "at a cost of 3" );

				var map = new ObjectiveMap();
				map.Add( new Sq( 7, 5 ) );            // one of the other tied squares

				var aware = ActivationPlanner.Plan( b,
					One( new Sq( 6, 2 ), AttackKind.Melee ), Hero( new Sq( 6, 6 ) ),
					null, null, null, map );

				True( aware.Figures[0].WillAttack,
					"the attack is still made -- an objective never costs one" );
				Eq( new Sq( 7, 5 ), aware.Figures[0].End,
					"and it stands on the objective instead" );
				Eq( 3, aware.Figures[0].MovementSpent, "for exactly the same movement" );
			} );

			Test( "an objective never costs a better shot", () =>
			{
				// The ordering is accuracy, then movement, then the objective.
				// A square that contests an objective but cannot shoot must
				// lose to one that can.
				var b = Open();
				for ( int r = 0; r < 7; r++ )
					if ( r != 3 ) b.SetSquare( new Sq( 8, r ), SquareFlags.Blocking );

				var map = new ObjectiveMap();
				map.Add( new Sq( 2, 6 ) );            // far from any firing line

				var plan = ActivationPlanner.Plan( b, One( new Sq( 4, 3 ) ),
					Hero( new Sq( 11, 3 ) ), null, null, null, map );

				True( plan.Figures[0].WillAttack, "it still finds the shot" );
				Eq( 3, plan.Figures[0].End.R, "through the only gap, not off toward the crate" );
			} );

			Test( "an objective never changes which Rebel is targeted", () =>
			{
				// The documented priority chain is untouched. Only the square
				// the figure stands on can change.
				var b = Open();
				var rebels = new List<TargetCandidate>
				{
					new TargetCandidate { Id = "H1", Name = "Diala",
						Position = new Sq( 9, 3 ), MaxHealth = 12 },
					new TargetCandidate { Id = "H2", Name = "Gaarkhan",
						Position = new Sq( 9, 5 ), MaxHealth = 14 },
				};

				var without = ActivationPlanner.Plan( b, One( new Sq( 3, 4 ) ), rebels );

				var map = new ObjectiveMap();
				map.Add( new Sq( 9, 6 ) );            // right next to Gaarkhan
				var with = ActivationPlanner.Plan( b, One( new Sq( 3, 4 ) ), rebels,
					null, null, null, map );

				Eq( without.GroupTarget.Chosen.Id, with.GroupTarget.Chosen.Id,
					"the chain picks the same Rebel either way" );
			} );

			Test( "a figure with nothing to attack drifts toward the objective", () =>
			{
				// Among squares equally close to an unreachable target, the one
				// that contests the mission is the better place to stand.
				// Melee, because a ranged figure has no range limit in this
				// game -- distance only raises the accuracy it must make, so
				// it can always declare a shot and never takes this branch.
				var b = Open( 20, 9 );
				var plain = ActivationPlanner.Plan( b,
					One( new Sq( 2, 4 ), AttackKind.Melee, speed: 2 ),
					Hero( new Sq( 18, 4 ) ) );
				var plainEnd = plain.Figures[0].End;
				False( plain.Figures[0].WillAttack, "far out of reach of a melee attack" );

				var map = new ObjectiveMap();
				map.Add( new Sq( plainEnd.C, plainEnd.R + 1 ) );

				var aware = ActivationPlanner.Plan( b,
					One( new Sq( 2, 4 ), AttackKind.Melee, speed: 2 ),
					Hero( new Sq( 18, 4 ) ), null, null, null, map );

				Eq( Distance.Count( b, plainEnd, new Sq( 18, 4 ) ),
					Distance.Count( b, aware.Figures[0].End, new Sq( 18, 4 ) ),
					"it closes exactly as far as it did before" );
				True( map.Contest( aware.Figures[0].End ) > 0,
					"but ends where it contests the objective" );
			} );

			Test( "the trace says when a square was taken for an objective", () =>
			{
				// A tactical preference has to be visible, or it reads as the
				// app moving figures for no reason.
				var b = Open();
				var map = new ObjectiveMap();
				map.Add( new Sq( 6, 4 ) );

				var plan = ActivationPlanner.Plan( b, One( new Sq( 6, 4 ) ),
					Hero( new Sq( 6, 6 ) ), null, null, null, map );

				True( plan.Figures[0].Trace.Any( t => t.Contains( "objective" ) ),
					"the reason is stated: " + string.Join( " | ", plan.Figures[0].Trace ) );
			} );

			Test( "planning without objectives behaves exactly as before", () =>
			{
				// Everything above is opt-in. A mission with no tokens, or a
				// caller that passes nothing, must be unaffected.
				var b = Open();
				var a = ActivationPlanner.Plan( b, One( new Sq( 3, 3 ) ), Hero( new Sq( 8, 3 ) ) );
				var c = ActivationPlanner.Plan( b, One( new Sq( 3, 3 ) ), Hero( new Sq( 8, 3 ) ),
					null, null, null, ObjectiveMap.Empty );
				Eq( a.Figures[0].End, c.Figures[0].End, "the same square either way" );
				Eq( a.Figures[0].MovementSpent, c.Figures[0].MovementSpent, "for the same cost" );
			} );
		}
	}
}
