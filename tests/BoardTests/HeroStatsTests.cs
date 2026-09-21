using System.Linq;
using Saga.Tracking;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>
	/// The printed hero sheets, and the effect they have on which Rebel the
	/// Imperial player is told to attack.
	/// </summary>
	public static class HeroStatsTests
	{
		private static BoardModel Open( int w = 12, int h = 5 )
		{
			var b = new BoardModel();
			for ( int c = 0; c < w; c++ )
				for ( int r = 0; r < h; r++ )
					b.SetSquare( new Sq( c, r ), SquareFlags.None );
			return b;
		}

		private static HeroStats Sheets()
		{
			var stats = new HeroStats();
			foreach ( var s in HeroSheets.All )
				stats.Add( new HeroProfile
				{
					Id = s.Id,
					Name = s.Name,
					Health = s.Health,
					Endurance = s.Endurance,
					Speed = s.Speed,
					Defense = s.Defense,
				} );
			return stats;
		}

		public static void Register()
		{
			Suite( "hero sheets" );

			Test( "every hero in the roster has a sheet", () =>
			{
				Eq( 21, HeroSheets.All.Length, "all 21 campaign heroes" );
				Eq( 21, HeroSheets.All.Select( h => h.Id ).Distinct().Count(),
					"with no duplicate ids" );
				True( HeroSheets.All.All( h => !string.IsNullOrEmpty( h.Name ) ),
					"and every one is named" );
			} );

			Test( "the numbers are inside the band real hero sheets occupy", () =>
			{
				// A tripwire, not a rule: anything outside this means the data
				// was parsed wrong rather than that a hero is unusual.
				foreach ( var h in HeroSheets.All )
				{
					True( h.Health >= 8 && h.Health <= 22, h.Name + " health" );
					True( h.Endurance >= 3 && h.Endurance <= 7, h.Name + " endurance" );
					True( h.Speed >= 3 && h.Speed <= 6, h.Name + " speed" );
				}
			} );

			Test( "defense is a die colour, never a number", () =>
			{
				// The wiki's own template documents "W for white, K for black",
				// and an en-dash for a sheet that prints no defense die at all.
				// A numeric defense would mean the source was misread.
				foreach ( var h in HeroSheets.All )
					True( h.Defense.All( d => d == "White" || d == "Black" ),
						h.Name + "'s defense must be a colour, got '"
							+ string.Join( ",", h.Defense ) + "'" );

				True( HeroSheets.All.Any( h => h.Defense.Length == 0 ),
					"and at least one hero sheet prints no defense die" );
			} );

			Test( "heroes do NOT all share the same numbers", () =>
			{
				// The whole point. Before this file every hero was 10 health
				// and 4 endurance, which silently flattened two of the four
				// target priority rules into no-ops.
				True( HeroSheets.All.Select( h => h.Health ).Distinct().Count() >= 5,
					"health varies across the roster" );
				True( HeroSheets.All.Select( h => h.Endurance ).Distinct().Count() > 1,
					"so does endurance" );
				True( HeroSheets.All.Select( h => h.Speed ).Distinct().Count() > 1,
					"and so does speed" );
			} );

			Test( "a sheet is applied without healing anybody", () =>
			{
				// Applying a corrected sheet mid-mission must not wipe damage
				// the party has already taken.
				var hero = new HeroCombatState { CardId = "H3", Name = "Gaarkhan" };
				hero.ApplyDamage( 3 );
				hero.Strain = 2;

				Sheets().For( "H3" ).ApplyTo( hero );
				Eq( 14, hero.MaxHealth, "Gaarkhan's printed health" );
				Eq( 3, hero.Damage, "damage already taken is kept" );
				Eq( 2, hero.Strain, "and so is strain" );
			} );

			Test( "damage beyond a corrected maximum is clamped, not left dangling", () =>
			{
				var hero = new HeroCombatState { CardId = "H4", Name = "Gideon Argus" };
				hero.MaxHealth = 20;
				hero.ApplyDamage( 18 );

				Sheets().For( "H4" ).ApplyTo( hero );
				Eq( 10, hero.MaxHealth, "Gideon's printed health" );
				True( hero.Damage <= hero.MaxHealth, "damage cannot exceed it" );
				Eq( 0, hero.RemainingHealth, "and he reads as down, not negative" );
			} );

			Test( "an unknown hero is tracked on defaults rather than refused", () =>
			{
				// Homebrew and new expansions must not break a session.
				var stats = Sheets();
				False( stats.Knows( "H99" ), "not a hero we ship" );
				var hero = new HeroCombatState { CardId = "H99", Name = "Somebody New" };
				stats.For( "H99" ).ApplyTo( hero );
				Eq( 10, hero.MaxHealth, "falls back" );
				Eq( 4, hero.Endurance, "and stays playable" );
			} );

			Test( "real health changes which hero the AI attacks", () =>
			{
				// The payoff, and the reason this is not a cosmetic data file.
				// Rule 2 of the priority chain is "healthy Rebel with the least
				// Health remaining". Gaarkhan (14) and Gideon (10) stand the
				// same distance away, so the choice turns entirely on the
				// sheets -- and with both flattened to 10 it could not.
				var b = Open();
				var stats = Sheets();

				var gaarkhan = new HeroCombatState { CardId = "H3", Name = "Gaarkhan" };
				var gideon = new HeroCombatState { CardId = "H4", Name = "Gideon Argus" };
				stats.For( "H3" ).ApplyTo( gaarkhan );
				stats.For( "H4" ).ApplyTo( gideon );
				Eq( 14, gaarkhan.MaxHealth, "Gaarkhan is the tougher of the two" );
				Eq( 10, gideon.MaxHealth, "Gideon the frailer" );

				TrackerBridge.SetHeroPosition( gaarkhan, new Sq( 6, 1 ), 1 );
				TrackerBridge.SetHeroPosition( gideon, new Sq( 6, 3 ), 1 );

				var group = GroupCombatState.Create( "g1", "DG001", "Stormtrooper", 1, 3 );
				group.Profile = UnitProfile.From( "Ranged", "Small1x1", 4, null );
				TrackerBridge.SetFigurePosition( group, 0, new Sq( 2, 2 ) );

				var snap = TrackerBridge.Snapshot( group, new[] { gaarkhan, gideon }, null, 1 );
				Eq( 2, snap.Rebels.Count, "both heroes are targets" );

				// Both stand the same distance off, so rule 1 ties and the
				// decision falls to rule 2, "healthy Rebel with the least
				// Health remaining". Run it through the real chain, not a sort.
				Eq( Sq.Chebyshev( new Sq( 2, 2 ), new Sq( 6, 1 ) ),
					Sq.Chebyshev( new Sq( 2, 2 ), new Sq( 6, 3 ) ),
					"the two heroes must be equidistant for this to test rule 2" );

				var plan = ActivationPlanner.Plan( b, snap.Enemies, snap.Rebels,
					null, snap.Visibility );
				Eq( "H4", plan.GroupTarget?.Chosen?.Id,
					"the frailer hero is the one the chain singles out" );

				// And the control: flatten both to the old default and the
				// distinction the rule depends on disappears.
				foreach ( var r in snap.Rebels ) r.MaxHealth = 10;
				var flat = ActivationPlanner.Plan( b, snap.Enemies, snap.Rebels,
					null, snap.Visibility );
				True( flat.GroupTarget.Tied.Count > 0
						|| flat.GroupTarget.Chosen.Id != "H4",
					"with identical sheets the chain can no longer separate them" );
			} );
		}
	}
}
