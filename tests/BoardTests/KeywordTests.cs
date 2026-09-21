using System.Collections.Generic;
using System.Linq;
using Saga.Tracking;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>
	/// The Mobile and Reach keywords, and the card mapping that finally feeds
	/// the engine a figure's real profile.
	/// </summary>
	public static class KeywordTests
	{
		private static BoardModel Open( int w = 8, int h = 4 )
		{
			var b = new BoardModel();
			for ( int c = 0; c < w; c++ )
				for ( int r = 0; r < h; r++ )
					b.SetSquare( new Sq( c, r ), SquareFlags.None );
			return b;
		}

		public static void Register()
		{
			Suite( "Mobile (sourced: Consolidated Rules p.47)" );

			Test( "ignores the additional cost of difficult terrain", () =>
			{
				// "ignore additional movement point costs when entering
				//  difficult terrain and spaces containing hostile figures"
				var b = Open();
				b.SetSquare( new Sq( 2, 1 ), SquareFlags.Difficult );

				Eq( 2, Pathfinder.Compute( b, new Sq( 1, 1 ), 6 ).CostTo( new Sq( 2, 1 ) ),
					"an ordinary figure pays 2" );
				Eq( 1, Pathfinder.Compute( b, new Sq( 1, 1 ), 6,
						new MoveOptions { Mobile = true } ).CostTo( new Sq( 2, 1 ) ),
					"a Mobile figure pays the base 1" );
			} );

			Test( "ignores the toll for entering a hostile figure's space", () =>
			{
				var b = Open();
				var hostile = new HashSet<Sq> { new Sq( 2, 1 ) };

				Eq( 2, Pathfinder.Compute( b, new Sq( 1, 1 ), 6,
						new MoveOptions { Hostile = hostile.Contains } )
						.CostTo( new Sq( 2, 1 ) ),
					"an ordinary figure pays the extra movement point" );
				Eq( 1, Pathfinder.Compute( b, new Sq( 1, 1 ), 6,
						new MoveOptions { Mobile = true, Hostile = hostile.Contains } )
						.CostTo( new Sq( 2, 1 ) ),
					"a Mobile figure does not" );
			} );

			Test( "may move through and enter blocking and impassable terrain", () =>
			{
				// "can move through and enter impassable and blocking terrain"
				var b = Open();
				b.SetSquare( new Sq( 2, 1 ), SquareFlags.Blocking );
				b.SetSquare( new Sq( 3, 1 ), SquareFlags.Impassable );

				var ordinary = Pathfinder.Compute( b, new Sq( 1, 1 ), 6 );
				var mobile = Pathfinder.Compute( b, new Sq( 1, 1 ), 6,
					new MoveOptions { Mobile = true } );

				True( ordinary.CostTo( new Sq( 2, 1 ) ) < 0, "blocking stops an ordinary figure" );
				True( ordinary.CostTo( new Sq( 3, 1 ) ) < 0, "so does impassable" );
				Eq( 1, mobile.CostTo( new Sq( 2, 1 ) ), "Mobile walks onto blocking terrain" );
				Eq( 2, mobile.CostTo( new Sq( 3, 1 ) ), "and onto impassable terrain" );
			} );

			Test( "may end its movement on blocking or impassable terrain", () =>
			{
				// "can end movement in a space containing impassable or
				//  blocking terrain"
				var b = Open();
				b.SetSquare( new Sq( 2, 1 ), SquareFlags.Blocking );
				var canEnd = Pathfinder.CanEndOn( b, new MoveOptions { Mobile = true } );
				True( canEnd( new Sq( 2, 1 ) ), "a Mobile figure may finish there" );
				False( Pathfinder.CanEndOn( b, new MoveOptions() )( new Sq( 2, 1 ) ),
					"an ordinary figure may not" );
			} );

			Test( "may NOT finish on a space occupied by another figure", () =>
			{
				// The entry grants terrain permissions only. Massive is the
				// keyword that says "can end its movement in spaces that
				// contain ... other figures" (p.41); Mobile says no such thing,
				// so the ordinary bar still applies.
				var b = Open();
				var friend = new HashSet<Sq> { new Sq( 2, 1 ) };
				var foe = new HashSet<Sq> { new Sq( 3, 1 ) };
				var opt = new MoveOptions
				{ Mobile = true, Friendly = friend.Contains, Hostile = foe.Contains };

				var canEnd = Pathfinder.CanEndOn( b, opt );
				False( canEnd( new Sq( 2, 1 ) ), "not on a friendly figure" );
				False( canEnd( new Sq( 3, 1 ) ), "nor on a hostile one" );

				True( Pathfinder.CanEndOn( b, new MoveOptions
				{ Massive = true, Friendly = friend.Contains, Hostile = foe.Contains } )
					( new Sq( 2, 1 ) ), "which is exactly where Massive differs" );
			} );

			Test( "a pit still stops a Mobile figure", () =>
			{
				// The entry names impassable and blocking and nothing else, and
				// pits appear nowhere in that rulebook. Reading the general
				// "ignore terrain" clause as covering them would invent a
				// permission, and a wrongly granted one produces illegal orders.
				var b = Open();
				b.SetSquare( new Sq( 2, 1 ), SquareFlags.Pit );
				var mobile = Pathfinder.Compute( b, new Sq( 1, 1 ), 6,
					new MoveOptions { Mobile = true } );
				True( mobile.CostTo( new Sq( 2, 1 ) ) < 0, "the pit is not entered" );
				False( Pathfinder.CanEndOn( b, new MoveOptions { Mobile = true } )( new Sq( 2, 1 ) ),
					"nor finished on" );
			} );

			Test( "figures still block line of sight to a Mobile figure", () =>
			{
				// "Figures do not block line of sight to or from a Massive
				//  figure" (p.41) is said of Massive alone. Mobile's entry does
				//  not carry it, so a Mobile figure is screened like any other.
				var b = Open( 6, 3 );
				var target = new FigureVisibility { Id = "t", Position = new Sq( 4, 1 ), Mobile = true };
				var screen = new FigureVisibility { Id = "s", Position = new Sq( 2, 1 ) };
				var others = new List<FigureVisibility> { screen };

				False( Visibility.CanSee( b, new Sq( 0, 1 ), target, others ),
					"a figure in the way still screens a Mobile target" );

				var massive = new FigureVisibility
				{ Id = "t", Position = new Sq( 4, 1 ), Massive = true };
				True( Visibility.CanSee( b, new Sq( 0, 1 ), massive, others ),
					"where a Massive target would be seen anyway" );
			} );

			Suite( "Reach (sourced: Consolidated Rules p.41 and p.51)" );

			Test( "a melee attack with Reach targets up to 2 spaces away", () =>
			{
				// "A figure with the Reach keyword may perform melee attacks
				//  that target figures or objects up to 2 spaces away."
				var b = Open();
				var near = AttackEvaluator.Assess( b, new Sq( 1, 1 ), new Sq( 3, 1 ),
					AttackKind.Melee, null, null, true );
				True( near.CanDeclare, "two spaces is within Reach" );
				Eq( 2, near.Distance, "and is reported at its true distance" );

				var far = AttackEvaluator.Assess( b, new Sq( 1, 1 ), new Sq( 4, 1 ),
					AttackKind.Melee, null, null, true );
				False( far.CanDeclare, "three spaces is not" );
			} );

			Test( "an attack with Reach does not require Accuracy", () =>
			{
				// "An attack with Reach does not require Accuracy."
				var b = Open();
				var a = AttackEvaluator.Assess( b, new Sq( 1, 1 ), new Sq( 3, 1 ),
					AttackKind.Melee, null, null, true );
				True( a.CanDeclare, "the attack is declared at range 2" );
				Eq( 0, a.RequiredAccuracy, "and no accuracy is demanded for it" );
			} );

			Test( "Reach needs line of sight where plain melee does not", () =>
			{
				// "The target of the attack must be within 2 spaces and in line
				//  of sight" -- whereas the melee entry (p.41) states adjacency
				//  alone, with no sight requirement.
				var b = Open();
				var blockers = new List<Sq> { new Sq( 2, 1 ) };

				// The positive control matters as much as the negative one: without
				// it this test passes just as happily when Reach does nothing at all.
				True( AttackEvaluator.Assess( b, new Sq( 1, 1 ), new Sq( 3, 1 ),
						AttackKind.Melee, null, null, true ).CanDeclare,
					"the same shot is legal with nothing in the way" );

				var reached = AttackEvaluator.Assess( b, new Sq( 1, 1 ), new Sq( 3, 1 ),
					AttackKind.Melee, blockers, null, true );
				False( reached.CanDeclare, "a figure in the way stops a Reach attack" );

				var adjacent = AttackEvaluator.Assess( b, new Sq( 1, 1 ), new Sq( 2, 1 ),
					AttackKind.Melee, blockers, null, true );
				True( adjacent.CanDeclare, "but an adjacent target is still attacked" );
			} );

			Test( "Reach adds a way to attack rather than removing the ordinary one", () =>
			{
				var b = Open();
				var withReach = AttackEvaluator.Assess( b, new Sq( 1, 1 ), new Sq( 2, 1 ),
					AttackKind.Melee, null, null, true );
				var without = AttackEvaluator.Assess( b, new Sq( 1, 1 ), new Sq( 2, 1 ),
					AttackKind.Melee );
				True( withReach.CanDeclare && without.CanDeclare,
					"both attack the adjacent figure" );
				Eq( without.Distance, withReach.Distance, "and report it the same way" );
			} );

			Test( "melee without Reach still reaches only an adjacent space", () =>
			{
				var b = Open();
				False( AttackEvaluator.Assess( b, new Sq( 1, 1 ), new Sq( 3, 1 ),
					AttackKind.Melee ).CanDeclare, "two spaces is out of reach" );
			} );

			Test( "Reach widens the squares a figure can attack from", () =>
			{
				var b = Open();
				var reach = Pathfinder.Compute( b, new Sq( 0, 1 ), 4 );
				var canEnd = Pathfinder.CanEndOn( b, new MoveOptions() );

				var plain = AttackEvaluator.FiringPositions( b, reach, new Sq( 4, 1 ),
					AttackKind.Melee, canEnd );
				var reaching = AttackEvaluator.FiringPositions( b, reach, new Sq( 4, 1 ),
					AttackKind.Melee, canEnd, null, null, true );

				True( reaching.Count > plain.Count,
					"more squares qualify once the figure has Reach" );
				True( plain.All( p => Sq.Chebyshev( p.square, new Sq( 4, 1 ) ) == 1 ),
					"plain melee only ever stands adjacent" );
				True( reaching.Any( p => Sq.Chebyshev( p.square, new Sq( 4, 1 ) ) == 2 ),
					"Reach lets it strike from a space further back" );
			} );

			Suite( "unit profile from the deployment card" );

			Test( "a melee card is not planned as a ranged one", () =>
			{
				// The defect this whole type exists to close: every figure
				// reached the planner as a ranged 1x1 at speed 4, because the
				// card's own fields never crossed into the engine.
				var p = UnitProfile.From( "Melee", "Small1x1", 5, new[] { "Reach" } );
				Eq( AttackKind.Melee, p.AttackKind, "attack type is carried over" );
				Eq( 5, p.Speed, "and so is speed" );
				True( p.HasReach, "and the keyword" );
			} );

			Test( "every footprint the cards use maps onto the engine's own", () =>
			{
				// enemies.json carries exactly these four, and they are spelled
				// the same on both sides -- which is what makes the mapping a
				// parse rather than a table somebody has to keep in step.
				Eq( Footprint.Small1x1, UnitProfile.From( "Ranged", "Small1x1", 4, null ).Footprint,
					"Small1x1" );
				Eq( Footprint.Medium1x2, UnitProfile.From( "Ranged", "Medium1x2", 4, null ).Footprint,
					"Medium1x2" );
				Eq( Footprint.Large2x2, UnitProfile.From( "Ranged", "Large2x2", 4, null ).Footprint,
					"Large2x2" );
				Eq( Footprint.Huge2x3, UnitProfile.From( "Ranged", "Huge2x3", 4, null ).Footprint,
					"Huge2x3" );
			} );

			Test( "the three rules keywords are recognised", () =>
			{
				True( UnitProfile.From( "Ranged", null, 4, new[] { "Massive" } ).Massive, "Massive" );
				True( UnitProfile.From( "Ranged", null, 4, new[] { "Mobile" } ).Mobile, "Mobile" );
				True( UnitProfile.From( "Ranged", null, 4, new[] { "Reach" } ).HasReach, "Reach" );
			} );

			Test( "a keyword is matched as a whole word, not a substring", () =>
			{
				// The same list carries "Cleave 2 {H}", "+3 Accuracy" and
				// "Habitat: Desert". Matching loosely would start finding
				// keywords that are not on the card.
				var p = UnitProfile.From( "Ranged", "Small1x1", 4,
					new[] { "Cleave 2 {H}", "+3 Accuracy", "Habitat: Desert", "Pierce 1" } );
				False( p.Massive || p.Mobile || p.HasReach,
					"none of those are Massive, Mobile or Reach" );
			} );

			Test( "an unreadable card falls back rather than throwing", () =>
			{
				// A card the app does not understand should still be playable.
				var p = UnitProfile.From( null, "SomethingNew", 0, new string[] { null } );
				Eq( 4, p.Speed, "speed falls back" );
				Eq( AttackKind.Ranged, p.AttackKind, "attack type falls back" );
				Eq( Footprint.Small1x1, p.Footprint, "footprint falls back" );
				False( p.Massive || p.Mobile || p.HasReach, "and no keyword is invented" );
			} );

			Test( "the profile reaches the planner through the tracker", () =>
			{
				// End to end: the flags have to survive the snapshot, which is
				// the crossing that was silently dropping them.
				var group = GroupCombatState.Create( "g1", "DG009", "Royal Guard", 1, 8 );
				group.Profile = UnitProfile.From( "Melee", "Small1x1", 5, new[] { "Reach" } );
				TrackerBridge.SetFigurePosition( group, 0, new Sq( 1, 1 ) );

				var hero = new HeroCombatState { CardId = "H1", Name = "Diala" };
				TrackerBridge.SetHeroPosition( hero, new Sq( 3, 1 ), 1 );

				var snap = TrackerBridge.Snapshot( group, new[] { hero }, null, 1 );
				Eq( 1, snap.Enemies.Count, "the trooper is in the snapshot" );
				var fig = snap.Enemies[0];
				Eq( AttackKind.Melee, fig.AttackKind, "as a melee figure" );
				Eq( 5, fig.Speed, "at its own speed" );
				True( fig.HasReach, "carrying Reach" );
			} );

			Test( "every shipped enemy card maps to a usable profile", () =>
			{
				// FFG's data, not ours. A renamed keyword or a new miniSize
				// would fall back to the defaults without complaint, putting
				// every figure of that card back to a ranged 1x1 moving 4.
				foreach ( var card in EnemyCards.All )
				{
					var p = UnitProfile.From( card.AttackType, card.MiniSize,
						card.Speed, card.Keywords );

					Eq( card.MiniSize, p.Footprint.ToString(),
						card.Name + "'s footprint must round-trip" );
					Eq( card.AttackType, p.AttackKind.ToString(),
						card.Name + "'s attack type must round-trip" );
					Eq( card.Speed, p.Speed, card.Name + "'s speed must carry over" );

					foreach ( var kw in card.Keywords )
					{
						if ( kw == "Massive" ) True( p.Massive, card.Name + " is Massive" );
						if ( kw == "Mobile" ) True( p.Mobile, card.Name + " is Mobile" );
						if ( kw == "Reach" ) True( p.HasReach, card.Name + " has Reach" );
					}
				}
			} );

			Test( "the shipped cards still hold the profile spread the AI depends on", () =>
			{
				// If these collapse to zero, the mapping has silently stopped
				// working and every enemy is being planned identically -- which
				// is what the engine did before any of this was wired up.
				int melee = EnemyCards.All.Count( c =>
					UnitProfile.From( c.AttackType, c.MiniSize, c.Speed, c.Keywords )
						.AttackKind == AttackKind.Melee );
				int large = EnemyCards.All.Count( c =>
					UnitProfile.From( c.AttackType, c.MiniSize, c.Speed, c.Keywords )
						.Footprint != Footprint.Small1x1 );
				int notFour = EnemyCards.All.Count( c =>
					UnitProfile.From( c.AttackType, c.MiniSize, c.Speed, c.Keywords ).Speed != 4 );

				Eq( 17, melee, "17 cards attack in melee" );
				Eq( 12, large, "12 cards are larger than one space" );
				True( notFour > 0, "and speed is not uniformly the old default" );
			} );

			Test( "a Reach figure plans to strike from two spaces away", () =>
			{
				// The payoff at the level the players see: the order given is
				// an attack, not a walk into contact.
				var b = Open();
				var group = GroupCombatState.Create( "g1", "DG009", "Royal Guard", 1, 8 );
				group.Profile = UnitProfile.From( "Melee", "Small1x1", 4, new[] { "Reach" } );
				TrackerBridge.SetFigurePosition( group, 0, new Sq( 0, 1 ) );

				var hero = new HeroCombatState { CardId = "H1", Name = "Diala" };
				TrackerBridge.SetHeroPosition( hero, new Sq( 5, 1 ), 1 );

				var snap = TrackerBridge.Snapshot( group, new[] { hero }, null, 1 );
				var plan = ActivationPlanner.Plan( b, snap.Enemies, snap.Rebels,
					null, snap.Visibility );

				Eq( 1, plan.Figures.Count, "one figure acts" );
				var fp = plan.Figures[0];
				True( fp.WillAttack, "and it attacks" );
				Eq( 2, fp.Attack.Distance, "from two spaces away, without closing to contact" );
			} );
		}
	}
}
