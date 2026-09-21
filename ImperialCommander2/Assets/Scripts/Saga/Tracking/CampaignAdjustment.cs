using System;
using System.Collections.Generic;

namespace Saga.Tracking
{
	/// <summary>
	/// What a campaign has added to a hero's printed sheet.
	/// </summary>
	/// <remarks>
	/// A hero sheet is where a hero STARTS a campaign, not what they are by
	/// mission six. Rewards, items and class cards raise health, endurance and
	/// occasionally speed, and those gains persist between missions.
	///
	/// Stored as a bonus rather than an absolute value on purpose. A bonus of
	/// zero means "whatever the sheet says", so a corrected sheet -- or a hero
	/// the printed data did not know about -- flows through without anybody
	/// re-entering a number, and a carry-over recorded against the wrong hero
	/// degrades to the printed sheet rather than to a wrong one.
	/// </remarks>
	public sealed class CampaignAdjustment
	{
		public string CardId;

		public int BonusHealth;
		public int BonusEndurance;
		public int BonusSpeed;

		/// <summary>Where the gain came from, in the players' words.</summary>
		/// <remarks>
		/// Required in spirit for the same reason a terrain correction carries
		/// one: an unexplained +2 Health is unreviewable three sessions later,
		/// and these accumulate over a whole campaign.
		/// </remarks>
		public string Reason = "";

		public bool IsEmpty => BonusHealth == 0 && BonusEndurance == 0 && BonusSpeed == 0;

		/// <summary>
		/// Raise a hero's sheet by what the campaign has granted.
		/// </summary>
		/// <remarks>
		/// Applied AFTER the printed sheet, never instead of it. Damage and
		/// strain already taken are left alone -- a mid-campaign upgrade makes
		/// a hero tougher, it does not heal them -- and a negative total is
		/// floored at 1 so a mis-entered penalty cannot produce a hero who is
		/// dead on arrival.
		/// </remarks>
		public void ApplyTo( HeroCombatState hero )
		{
			if ( hero == null ) return;
			hero.MaxHealth = Math.Max( 1, hero.MaxHealth + BonusHealth );
			hero.Endurance = Math.Max( 1, hero.Endurance + BonusEndurance );
			hero.Speed = Math.Max( 1, hero.Speed + BonusSpeed );

			if ( hero.Damage > hero.MaxHealth ) hero.Damage = hero.MaxHealth;
			if ( hero.Strain > hero.Endurance ) hero.Strain = hero.Endurance;
		}

		public override string ToString()
			=> IsEmpty
				? CardId + ": no campaign gains"
				: $"{CardId}: {Sign( BonusHealth )} health, {Sign( BonusEndurance )} endurance, "
				  + $"{Sign( BonusSpeed )} speed"
				  + (string.IsNullOrEmpty( Reason ) ? "" : " -- " + Reason);

		private static string Sign( int n ) => n >= 0 ? "+" + n : n.ToString();
	}

	/// <summary>Every hero's campaign gains, by card id.</summary>
	public sealed class CampaignCarryOver
	{
		private readonly Dictionary<string, CampaignAdjustment> _byId =
			new Dictionary<string, CampaignAdjustment>( StringComparer.OrdinalIgnoreCase );

		public int Count => _byId.Count;

		public IEnumerable<CampaignAdjustment> All => _byId.Values;

		public static readonly CampaignAdjustment None = new CampaignAdjustment();

		public void Set( CampaignAdjustment adjustment )
		{
			if ( adjustment == null || string.IsNullOrEmpty( adjustment.CardId ) ) return;

			// Repeated entries replace rather than stack, the same way a
			// terrain correction does. Two taps must not become +2.
			_byId[adjustment.CardId] = adjustment;
		}

		public CampaignAdjustment For( string cardId )
			=> cardId != null && _byId.TryGetValue( cardId, out var a ) ? a : None;

		/// <summary>
		/// Seat a hero on their printed sheet, then add what the campaign gave
		/// them.
		/// </summary>
		/// <remarks>
		/// The order matters and is the whole point: the sheet is data we
		/// shipped and can correct, the carry-over is the players' own record
		/// of their campaign, and neither should overwrite the other.
		/// </remarks>
		public void Seat( HeroCombatState hero, HeroStats sheets )
		{
			if ( hero == null ) return;
			(sheets?.For( hero.CardId ) ?? HeroProfile.Default).ApplyTo( hero );
			For( hero.CardId ).ApplyTo( hero );
		}
	}
}
