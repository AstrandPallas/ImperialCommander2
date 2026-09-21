using System.Linq;
using Saga.Tracking;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>
	/// Luke as a campaign hero: the homebrew data, and the two things that
	/// break if nobody checks.
	/// </summary>
	public static class LukeTests
	{
		private static System.Collections.Generic.IEnumerable<ClassCard> Deck()
			=> ClassDecks.All.Where( c => c.Owner == LukeAsHero.HeroId );

		private static int CostOf( string name )
			=> Deck().Where( c => c.Name == name ).Select( c => c.Cost ).FirstOrDefault( -1 );

		public static void Register()
		{
			Suite( "Luke as a campaign hero (homebrew)" );

			Test( "EVERY deck spends exactly two cards at each of 1, 2, 3 and 4 XP", () =>
			{
				// This is the convention that matters, and it holds across all
				// 22 decks including the one this fork authors. A deck that
				// breaks it desynchronises its hero's XP economy from every
				// other hero at the table.
				foreach ( var owner in ClassDecks.All.Select( c => c.Owner ).Distinct() )
				{
					var costs = ClassDecks.All.Where( c => c.Owner == owner )
						.Select( c => c.Cost ).ToList();
					foreach ( int tier in new[] { 1, 2, 3, 4 } )
						Eq( 2, costs.Count( c => c == tier ),
							owner + " must have two cards at " + tier + " XP" );
					True( costs.Count( c => c == 0 ) >= 1,
						owner + " must have a starting card" );
				}
			} );

			Test( "one shipped hero starts with two weapons, and it is deliberate", () =>
			{
				// Verena Talos is the single exception to "nine cards": she is
				// a melee and ranged hybrid and starts with both weapons, which
				// the data marks with the id suffix 00a rather than a new tier.
				// Recorded here so it is not "corrected" by somebody later.
				var verena = ClassDecks.All.Where( c => c.Owner == "H11" ).ToList();
				Eq( 10, verena.Count, "ten cards, not nine" );
				Eq( 2, verena.Count( c => c.Cost == 0 ), "because two of them cost nothing" );
				True( verena.Any( c => c.Id.EndsWith( "00a" ) ),
					"and the extra one is marked as such" );

				// Everyone else, this fork's hero included, has exactly nine.
				foreach ( var owner in ClassDecks.All.Select( c => c.Owner )
					.Distinct().Where( o => o != "H11" ) )
					Eq( 9, ClassDecks.All.Count( c => c.Owner == owner ),
						owner + " has the usual nine" );
			} );

			Test( "the two abilities that make the ally overpowered cost the most", () =>
			{
				// Deflect and Heroic are the reason ally Luke cannot simply be
				// dropped in at level 0, so they are bought back rather than
				// given away.
				Eq( 2, CostOf( "Deflect" ), "Deflect at 2 XP" );
				Eq( 4, CostOf( "Heroic" ), "Heroic at 4 XP" );
			} );

			Test( "the 0-XP card is his weapon, so the item deck still upgrades him", () =>
			{
				// Heroes get weapons from the item deck. An innate attack would
				// either double-dip or lock him out of item progression, which
				// is why Diala's Plasteel Staff works the same way.
				Eq( 0, CostOf( "Jedi Knight's Lightsaber" ),
					"his lightsaber is the starting card" );
			} );

			Test( "he does not clone the existing Force hero's deck", () =>
			{
				// Diala already owns these. A second hero with the same cards
				// would make one of them pointless to play.
				var his = Deck().Select( c => c.Name ).ToList();
				foreach ( var taken in ClassDecks.All.Where( c => c.Owner == "H1" )
					.Select( c => c.Name ) )
					False( his.Contains( taken ),
						taken + " belongs to Diala and must not be reused" );
			} );

			Test( "his card ids do not collide with anybody else's", () =>
			{
				Eq( ClassDecks.All.Length, ClassDecks.All.Select( c => c.Id ).Distinct().Count(),
					"every class card id in the game is unique" );
			} );

			Suite( "Luke setup conflicts" );

			Test( "a campaign without him is untouched", () =>
			{
				var conflicts = LukeAsHero.Check(
					new[] { "H1", "H3" },
					availableAllyIds: new[] { "A011" },
					plannedMissionIds: new[] { "OTHER2" } );
				Eq( 0, conflicts.Count, "an ordinary campaign sees none of this" );
				Eq( 0, LukeAsHero.AlliesToExclude( new[] { "H1" } ).Count(),
					"and nothing is excluded from it" );
			} );

			Test( "he cannot be a hero and stay in the ally pool", () =>
			{
				var conflicts = LukeAsHero.Check(
					new[] { "H1", "H22" }, availableAllyIds: new[] { "A001", "A011" } );

				Eq( 1, conflicts.Count, "one conflict" );
				True( conflicts[0].What.Contains( "ally pool" ), "which names the problem" );
				True( conflicts[0].Fix.Contains( "A011" ), "and the card to exclude" );
				Eq( "A011", LukeAsHero.AlliesToExclude( new[] { "H22" } ).Single(),
					"and that is what is offered for exclusion" );
			} );

			Test( "already excluding him clears the conflict", () =>
			{
				Eq( 0, LukeAsHero.Check( new[] { "H22" },
						availableAllyIds: new[] { "A011" },
						ignoredIds: new[] { "A011" } ).Count,
					"nothing left to warn about" );
			} );

			Test( "his own side mission's reward is reported as void", () =>
			{
				// OTHER2 ships in this repo and rewards the party with Luke as
				// an ally, which means nothing once he is standing on the board.
				var conflicts = LukeAsHero.Check(
					new[] { "H22" }, ignoredIds: new[] { "A011" },
					plannedMissionIds: new[] { "CORE1", "OTHER2" } );

				Eq( 1, conflicts.Count, "the mission is flagged" );
				True( conflicts[0].What.Contains( "OTHER2" ), "by name" );
				True( conflicts[0].Fix.Contains( "personal mission" ),
					"with the re-skin suggested rather than a deletion" );
			} );

			Test( "conflicts are reported, never applied", () =>
			{
				// Quietly removing a card the players chose, or rewriting a
				// mission's reward behind their backs, is what makes an app
				// untrustworthy at the table.
				var ignored = new System.Collections.Generic.List<string>();
				LukeAsHero.Check( new[] { "H22" }, availableAllyIds: new[] { "A011" },
					ignoredIds: ignored );
				Eq( 0, ignored.Count, "the campaign's own list is left alone" );
			} );

			Test( "nothing here fires for a party that is not using him", () =>
			{
				False( LukeAsHero.IsPlayingAsHero( new[] { "H1", "H2", "H3", "H4" } ),
					"an ordinary party" );
				False( LukeAsHero.IsPlayingAsHero( null ), "or no party at all" );
				True( LukeAsHero.IsPlayingAsHero( new[] { "H22" } ), "but his id is recognised" );
			} );
		}
	}
}
