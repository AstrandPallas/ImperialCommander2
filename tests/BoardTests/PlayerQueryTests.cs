using System.Collections.Generic;
using System.Linq;
using Saga.Tracking;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>The questions the players ask the board, rather than the orders it gives.</summary>
	public static class PlayerQueryTests
	{
		private static BoardModel Open( int w = 12, int h = 6 )
		{
			var b = new BoardModel();
			for ( int c = 0; c < w; c++ )
				for ( int r = 0; r < h; r++ )
					b.SetSquare( new Sq( c, r ), SquareFlags.None );
			return b;
		}

		private static HeroCombatState Hero( string id, Sq at, int speed = 4 )
		{
			var h = new HeroCombatState { CardId = id, Name = id, Speed = speed };
			TrackerBridge.SetHeroPosition( h, at, 1 );
			return h;
		}

		private static GroupCombatState Trooper( Sq at, string attack = "Ranged",
			int speed = 4, int figures = 1 )
		{
			var g = GroupCombatState.Create( "g" + at, "DG001", "Stormtrooper", figures, 3 );
			g.Profile = UnitProfile.From( attack, "Small1x1", speed, null );
			// UnitProfile treats a speed of 0 as "the card did not say" and
			// falls back to 4, which is right for card data and wrong for a
			// test that wants a figure which genuinely cannot move.
			g.Profile.Speed = speed;
			TrackerBridge.SetFigurePosition( g, 0, at );
			return g;
		}

		public static void Register()
		{
			Suite( "player queries" );

			Test( "line of sight is blocked by terrain the players can see", () =>
			{
				var b = Open();
				b.SetSquare( new Sq( 4, 2 ), SquareFlags.Blocking );
				b.SetSquare( new Sq( 4, 3 ), SquareFlags.Blocking );

				True( PlayerQueries.CanSee( b, new Sq( 1, 2 ), new Sq( 3, 2 ) ),
					"nothing in the way" );
				False( PlayerQueries.CanSee( b, new Sq( 4, 2 ), new Sq( 4, 2 ) ) == false,
					"a square can see itself" );
				False( PlayerQueries.CanSee( b, new Sq( 1, 3 ), new Sq( 7, 3 ) ),
					"blocking terrain stops the line" );
			} );

			Test( "a query and the AI's own answer cannot disagree", () =>
			{
				// The reason this reuses the engine rather than reimplementing
				// it: if a player asks whether a trooper can see them and gets
				// a different answer from the one the planner used, the app has
				// argued with itself in front of the table.
				var b = Open();
				b.SetSquare( new Sq( 5, 2 ), SquareFlags.Blocking );

				var from = new Sq( 2, 2 );
				var to = new Sq( 8, 2 );
				var figures = new List<FigureVisibility>();

				bool query = PlayerQueries.CanSee( b, from, to, figures );
				var assessed = AttackEvaluator.Assess( b, from, to, AttackKind.Ranged );
				Eq( query, assessed.CanDeclare,
					"the player query and the attack evaluator must agree" );
			} );

			Test( "neither endpoint screens its own line", () =>
			{
				var b = Open();
				var hero = Hero( "H1", new Sq( 2, 2 ) );
				var trooper = Trooper( new Sq( 6, 2 ) );
				var figures = PlayerQueries.FiguresOnBoard( new[] { hero }, new[] { trooper } );

				Eq( 2, figures.Count, "both figures are on the board" );
				True( PlayerQueries.CanSee( b, new Sq( 2, 2 ), new Sq( 6, 2 ), figures ),
					"a figure does not block the line to itself" );
			} );

			Test( "a figure in between does block", () =>
			{
				var b = Open();
				var hero = Hero( "H1", new Sq( 2, 2 ) );
				var screen = Hero( "H2", new Sq( 4, 2 ) );
				var trooper = Trooper( new Sq( 6, 2 ) );
				var figures = PlayerQueries.FiguresOnBoard(
					new[] { hero, screen }, new[] { trooper } );

				False( PlayerQueries.CanSee( b, new Sq( 2, 2 ), new Sq( 6, 2 ), figures ),
					"the hero standing in the middle screens the shot" );
			} );

			Test( "reachable squares cost what the pathfinder says they cost", () =>
			{
				var b = Open();
				b.SetSquare( new Sq( 3, 2 ), SquareFlags.Difficult );
				var hero = Hero( "H1", new Sq( 1, 2 ), speed: 4 );

				var reach = PlayerQueries.ReachableBy( b, hero, 1 );
				var lookup = reach.ToDictionary( r => r.square, r => r.cost );

				Eq( 0, lookup[new Sq( 1, 2 )], "standing still is free" );
				Eq( 1, lookup[new Sq( 2, 2 )], "one ordinary step" );
				Eq( 3, lookup[new Sq( 3, 2 )], "difficult ground costs the extra point" );
				True( reach.All( r => r.cost <= 4 ), "and nothing exceeds the budget" );
			} );

			Test( "a hero cannot finish on another figure", () =>
			{
				var b = Open();
				var hero = Hero( "H1", new Sq( 1, 2 ) );
				var friend = Hero( "H2", new Sq( 2, 2 ) );
				var trooper = Trooper( new Sq( 3, 2 ) );

				var reach = PlayerQueries.ReachableBy( b, hero, 1, new[] { friend },
					new[] { trooper } );
				var squares = reach.Select( r => r.square ).ToHashSet();

				False( squares.Contains( new Sq( 2, 2 ) ), "not on the friendly figure" );
				False( squares.Contains( new Sq( 3, 2 ) ), "nor on the trooper" );
				True( squares.Contains( new Sq( 4, 2 ) ),
					"but it may still move PAST the friendly one" );
			} );

			Suite( "threat map" );

			Test( "an enemy already in position is reported at zero cost", () =>
			{
				var b = Open();
				var trooper = Trooper( new Sq( 5, 2 ) );
				var threats = PlayerQueries.ThreatsTo( b, new Sq( 2, 2 ), new[] { trooper } );

				Eq( 1, threats.Count, "one figure threatens the square" );
				True( threats[0].AlreadyInPosition, "it need not move to shoot" );
				Eq( 0, threats[0].MoveCost, "so it spends nothing" );
			} );

			Test( "an enemy that must close first reports what it would spend", () =>
			{
				// A melee trooper across the room is still a threat, just a
				// more expensive one -- and that difference is exactly what a
				// player is deciding on.
				var b = Open();
				var brute = Trooper( new Sq( 8, 2 ), attack: "Melee", speed: 4 );
				var threats = PlayerQueries.ThreatsTo( b, new Sq( 5, 2 ), new[] { brute } );

				Eq( 1, threats.Count, "it can reach" );
				False( threats[0].AlreadyInPosition, "but not without moving" );
				Eq( 2, threats[0].MoveCost, "closing to an adjacent space costs 2" );
			} );

			Test( "an enemy too far away this round is not a threat this round", () =>
			{
				var b = Open( 24, 6 );
				var brute = Trooper( new Sq( 22, 2 ), attack: "Melee", speed: 3 );
				Eq( 0, PlayerQueries.ThreatsTo( b, new Sq( 1, 2 ), new[] { brute } ).Count,
					"out of reach with one move action" );
			} );

			Test( "terrain the enemy cannot see through removes the threat", () =>
			{
				var b = Open();
				for ( int r = 0; r < 6; r++ )
					b.SetSquare( new Sq( 6, r ), SquareFlags.Blocking );

				// Speed 1 so it cannot walk around the wall to find an angle.
				var trooper = Trooper( new Sq( 9, 2 ), speed: 1 );
				Eq( 0, PlayerQueries.ThreatsTo( b, new Sq( 2, 2 ), new[] { trooper } ).Count,
					"a wall across the room blocks the shot" );
			} );

			Test( "a Stunned group loses the move it would need", () =>
			{
				// "A Stunned figure must spend one action to remove the
				// condition", leaving one action, which the attack consumes.
				var b = Open();
				var brute = Trooper( new Sq( 8, 2 ), attack: "Melee", speed: 4 );
				Eq( 1, PlayerQueries.ThreatsTo( b, new Sq( 5, 2 ), new[] { brute } ).Count,
					"it threatens the square while ready" );

				brute.Conditions.Add( Condition.Stunned );
				Eq( 0, PlayerQueries.ThreatsTo( b, new Sq( 5, 2 ), new[] { brute } ).Count,
					"and cannot once Stunned, because it has nothing left to move with" );
			} );

			Test( "a defeated group threatens nothing", () =>
			{
				var b = Open();
				var trooper = Trooper( new Sq( 5, 2 ) );
				while ( !trooper.IsDefeated ) trooper.ApplyDamage( 3 );
				Eq( 0, PlayerQueries.ThreatsTo( b, new Sq( 2, 2 ), new[] { trooper } ).Count,
					"nothing left to do the shooting" );
			} );

			Test( "the threat map separates the safe squares from the exposed ones", () =>
			{
				// The question the map exists to answer: of everywhere I can
				// go, which squares are behind cover?
				var b = Open( 14, 6 );
				for ( int r = 0; r < 6; r++ )
					if ( r != 5 ) b.SetSquare( new Sq( 7, r ), SquareFlags.Blocking );

				var hero = Hero( "H1", new Sq( 3, 2 ), speed: 4 );
				var trooper = Trooper( new Sq( 11, 2 ), speed: 0 );

				var map = PlayerQueries.ThreatMapFor( b, hero, 1, new[] { trooper } );
				True( map.Count > 0, "the hero has somewhere to go" );
				True( map.Any( m => m.threatCount == 0 ),
					"some reachable squares are out of the trooper's sight" );
				True( map.Any( m => m.threatCount > 0 ),
					"and some are not, or the map would be telling us nothing" );

				// Every exposed square must genuinely have a line to the
				// shooter, which is the claim the map is making.
				foreach ( var m in map.Where( x => x.threatCount > 0 ) )
					True( PlayerQueries.CanSee( b, m.square, new Sq( 11, 2 ) ),
						"square " + m.square + " is reported exposed, so it must be visible" );
			} );

			Test( "reach lets an enemy threaten a square it is not adjacent to", () =>
			{
				var b = Open();
				var guard = GroupCombatState.Create( "rg", "DG009", "Royal Guard", 1, 8 );
				guard.Profile = UnitProfile.From( "Melee", "Small1x1", 4, new[] { "Reach" } );
				guard.Profile.Speed = 0;
				TrackerBridge.SetFigurePosition( guard, 0, new Sq( 5, 2 ) );

				// Speed 0, so anything it threatens it threatens standing still.
				var threats = PlayerQueries.ThreatsTo( b, new Sq( 7, 2 ), new[] { guard } );
				Eq( 1, threats.Count, "two spaces away is still within Reach" );
				True( threats[0].AlreadyInPosition, "without taking a step" );

				Eq( 0, PlayerQueries.ThreatsTo( b, new Sq( 8, 2 ), new[] { guard } ).Count,
					"three spaces away is not" );
			} );
		}
	}
}
