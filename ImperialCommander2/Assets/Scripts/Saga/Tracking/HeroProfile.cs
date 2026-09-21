using System;
using System.Collections.Generic;

namespace Saga.Tracking
{
	/// <summary>
	/// A hero's printed sheet numbers: health, endurance, speed and the colour
	/// of the defense die.
	/// </summary>
	/// <remarks>
	/// heroes.json carries none of these, so without them every hero was
	/// tracked as 10 health and 4 endurance. That reaches the AI: the Imperial
	/// priority chain ranks Rebels by health remaining and by total health, so
	/// identical numbers make two of its four rules meaningless and change
	/// which hero gets attacked.
	///
	/// Campaign upgrades raise these during a campaign, so a profile is the
	/// value a hero STARTS a session with, not a constant.
	/// </remarks>
	public sealed class HeroProfile
	{
		public string Id;
		public string Name;
		public int Health = 10;
		public int Endurance = 4;
		public int Speed = 4;

		/// <summary>Die colours, empty when the sheet prints no defense die.</summary>
		public string[] Defense = Array.Empty<string>();

		public static readonly HeroProfile Default = new HeroProfile();

		/// <summary>
		/// Copy the starting numbers onto a tracked hero.
		/// </summary>
		/// <remarks>
		/// Damage and strain already taken are left alone, so applying a
		/// profile mid-mission corrects the sheet without healing anybody.
		/// </remarks>
		public void ApplyTo( HeroCombatState hero )
		{
			if ( hero == null ) return;
			hero.MaxHealth = Health;
			hero.Endurance = Endurance;
			hero.Speed = Speed;
			if ( hero.Damage > hero.MaxHealth ) hero.Damage = hero.MaxHealth;
			if ( hero.Strain > hero.Endurance ) hero.Strain = hero.Endurance;
		}
	}

	/// <summary>Every hero's starting sheet, by card id.</summary>
	public sealed class HeroStats
	{
		private readonly Dictionary<string, HeroProfile> _byId =
			new Dictionary<string, HeroProfile>( StringComparer.OrdinalIgnoreCase );

		public int Count => _byId.Count;

		public IEnumerable<HeroProfile> All => _byId.Values;

		public void Add( HeroProfile profile )
		{
			if ( profile == null || string.IsNullOrEmpty( profile.Id ) ) return;
			_byId[profile.Id] = profile;
		}

		/// <summary>
		/// The sheet for a hero, or the default when none is recorded.
		/// </summary>
		/// <remarks>
		/// A hero the file does not know about is tracked on the defaults
		/// rather than refused. An unknown hero is usually a new expansion or
		/// somebody's homebrew, and refusing to track them would make the app
		/// useless for exactly the party that bought the newest box.
		/// </remarks>
		public HeroProfile For( string cardId )
			=> cardId != null && _byId.TryGetValue( cardId, out var p ) ? p : HeroProfile.Default;

		public bool Knows( string cardId )
			=> cardId != null && _byId.ContainsKey( cardId );
	}
}
