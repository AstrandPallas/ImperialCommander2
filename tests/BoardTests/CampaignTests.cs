using System.Linq;
using Saga.Tracking;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>What a campaign adds to a hero's printed sheet, and what it must not touch.</summary>
	public static class CampaignTests
	{
		private static HeroStats Sheets()
		{
			var stats = new HeroStats();
			foreach ( var s in HeroSheets.All )
				stats.Add( new HeroProfile
				{
					Id = s.Id, Name = s.Name, Health = s.Health,
					Endurance = s.Endurance, Speed = s.Speed, Defense = s.Defense,
				} );
			return stats;
		}

		private static HeroCombatState Gaarkhan()
			=> new HeroCombatState { CardId = "H3", Name = "Gaarkhan" };

		public static void Register()
		{
			Suite( "campaign carry-over" );

			Test( "a hero with no campaign gains is seated on the printed sheet", () =>
			{
				var hero = Gaarkhan();
				new CampaignCarryOver().Seat( hero, Sheets() );
				Eq( 14, hero.MaxHealth, "Gaarkhan's printed health" );
				Eq( 4, hero.Endurance, "and endurance" );
			} );

			Test( "gains are added on top of the sheet, not instead of it", () =>
			{
				// The distinction that matters: a hero sheet is where a hero
				// STARTS a campaign, not what they are by mission six.
				var carry = new CampaignCarryOver();
				carry.Set( new CampaignAdjustment
				{
					CardId = "H3", BonusHealth = 3, BonusEndurance = 1,
					Reason = "Rugged Survivor and a Tactical Vest",
				} );

				var hero = Gaarkhan();
				carry.Seat( hero, Sheets() );
				Eq( 17, hero.MaxHealth, "14 printed plus 3 earned" );
				Eq( 5, hero.Endurance, "4 printed plus 1 earned" );
			} );

			Test( "a corrected sheet flows through without re-entering the gains", () =>
			{
				// Why gains are stored as a bonus rather than an absolute. If
				// the shipped sheet turns out to be wrong and is fixed, the
				// campaign record does not have to be edited too.
				var carry = new CampaignCarryOver();
				carry.Set( new CampaignAdjustment { CardId = "H3", BonusHealth = 3 } );

				var corrected = new HeroStats();
				corrected.Add( new HeroProfile
				{ Id = "H3", Name = "Gaarkhan", Health = 15, Endurance = 4, Speed = 4 } );

				var hero = Gaarkhan();
				carry.Seat( hero, corrected );
				Eq( 18, hero.MaxHealth, "the correction carries the bonus with it" );
			} );

			Test( "repeated entries replace rather than stack", () =>
			{
				// Two taps must not become +2, the same rule a terrain
				// correction follows.
				var carry = new CampaignCarryOver();
				carry.Set( new CampaignAdjustment { CardId = "H3", BonusHealth = 2 } );
				carry.Set( new CampaignAdjustment { CardId = "H3", BonusHealth = 2 } );

				Eq( 1, carry.Count, "one record for the hero" );
				var hero = Gaarkhan();
				carry.Seat( hero, Sheets() );
				Eq( 16, hero.MaxHealth, "14 plus 2, not plus 4" );
			} );

			Test( "an upgrade makes a hero tougher, it does not heal them", () =>
			{
				var carry = new CampaignCarryOver();
				carry.Set( new CampaignAdjustment { CardId = "H3", BonusHealth = 3 } );

				var hero = Gaarkhan();
				Sheets().For( "H3" ).ApplyTo( hero );
				hero.ApplyDamage( 6 );

				carry.For( "H3" ).ApplyTo( hero );
				Eq( 17, hero.MaxHealth, "tougher" );
				Eq( 6, hero.Damage, "but just as hurt" );
				Eq( 11, hero.RemainingHealth, "with the gain showing in what is left" );
			} );

			Test( "a gain recorded against a hero not in the party is simply ignored", () =>
			{
				// Carry-over outlives the party that made it, the same way a
				// terrain correction outlives the mission.
				var carry = new CampaignCarryOver();
				carry.Set( new CampaignAdjustment { CardId = "H99", BonusHealth = 5 } );

				var hero = Gaarkhan();
				carry.Seat( hero, Sheets() );
				Eq( 14, hero.MaxHealth, "untouched by somebody else's upgrade" );
			} );

			Test( "a mis-entered penalty cannot produce a hero who is dead on arrival", () =>
			{
				var carry = new CampaignCarryOver();
				carry.Set( new CampaignAdjustment
				{ CardId = "H3", BonusHealth = -50, BonusEndurance = -50, BonusSpeed = -50 } );

				var hero = Gaarkhan();
				carry.Seat( hero, Sheets() );
				True( hero.MaxHealth >= 1, "health is floored" );
				True( hero.Endurance >= 1, "so is endurance" );
				True( hero.Speed >= 1, "and speed" );
			} );

			Test( "an empty record reads as empty", () =>
			{
				True( new CampaignAdjustment().IsEmpty, "nothing granted" );
				False( new CampaignAdjustment { BonusHealth = 1 }.IsEmpty, "something granted" );
				True( CampaignCarryOver.None.IsEmpty, "and the fallback grants nothing" );
			} );

			Test( "gains survive a save and load, because they land in MaxHealth", () =>
			{
				// The tracker persists the seated numbers rather than the
				// bonus, so a reload mid-mission restores the hero the players
				// have been playing, not the one on the printed card.
				var carry = new CampaignCarryOver();
				carry.Set( new CampaignAdjustment { CardId = "H3", BonusHealth = 3, BonusSpeed = 1 } );

				var t = new TrackerManager();
				var hero = Gaarkhan();
				carry.Seat( hero, Sheets() );
				TrackerBridge.SetHeroPosition( hero, new Sq( 2, 2 ), 1 );
				t.Heroes.Add( hero );

				var reloaded = new TrackerManager();
				reloaded.Restore( t.Capture() );

				Eq( 17, reloaded.Heroes[0].MaxHealth, "the upgraded health is what reloads" );
				Eq( 5, reloaded.Heroes[0].Speed, "and the upgraded speed" );
			} );

			Test( "the record says where a gain came from", () =>
			{
				// An unexplained +2 Health is unreviewable three sessions
				// later, and these accumulate across a whole campaign.
				var a = new CampaignAdjustment
				{ CardId = "H3", BonusHealth = 2, Reason = "Reward: Wookiee Rage" };
				True( a.ToString().Contains( "Wookiee Rage" ),
					"the reason travels with the number: " + a );
			} );
		}
	}
}
