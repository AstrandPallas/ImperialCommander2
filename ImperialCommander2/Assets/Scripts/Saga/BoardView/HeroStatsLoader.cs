using System.Collections.Generic;
using Newtonsoft.Json;
using Saga.Tracking;
using UnityEngine;

namespace Saga
{
	/// <summary>Reads the printed hero sheets from Resources into a HeroStats.</summary>
	/// <remarks>
	/// Kept beside TerrainLoader and shaped the same way: the file is shipped
	/// data, read once, and a missing or unreadable file degrades to the
	/// defaults with a warning rather than stopping a mission.
	/// </remarks>
	public static class HeroStatsLoader
	{
		public const string ResourcePath = "CardData/herostats";

		/// <summary>
		/// Homebrew sheets, merged on top of the sourced ones.
		/// </summary>
		/// <remarks>
		/// Kept in a separate file so provenance survives: everything in
		/// herostats.json was read off a printed sheet, everything here was
		/// designed. Mixing them would make the sourced file unverifiable.
		/// </remarks>
		public const string HomebrewResourcePath = "CardData/herostats-homebrew";

		private static HeroStats _cached;

		private sealed class HeroStatsFile
		{
			public int schemaVersion;
			public List<HeroRow> heroes;
		}

		private sealed class HeroRow
		{
			public string id;
			public string name;
			public int health;
			public int endurance;
			public int speed;
			public string[] defense;
		}

		public static HeroStats Stats => _cached ?? (_cached = Load());

		/// <summary>Drop the cache so an edited file is picked up.</summary>
		public static void Reload() => _cached = null;

		private static HeroStats Load()
		{
			var stats = new HeroStats();
			int printed = Merge( stats, ResourcePath, required: true );
			int homebrew = Merge( stats, HomebrewResourcePath, required: false );

			Utils.LogWarning( "HeroStatsLoader::" + printed + " printed hero sheets"
				+ (homebrew > 0 ? " and " + homebrew + " homebrew" : "") + " loaded" );
			return stats;
		}

		/// <summary>Read one file into the set, returning how many sheets it held.</summary>
		private static int Merge( HeroStats stats, string path, bool required )
		{
			var asset = Resources.Load<TextAsset>( path );
			if ( asset == null )
			{
				if ( required )
					Utils.LogWarning( "HeroStatsLoader::" + path
						+ " is missing, so heroes fall back to 10 health and 4 endurance" );
				return 0;
			}

			HeroStatsFile file = null;
			try
			{
				file = JsonConvert.DeserializeObject<HeroStatsFile>( asset.text );
			}
			catch ( System.Exception e )
			{
				Utils.LogWarning( "HeroStatsLoader::could not read " + path + ": " + e.Message );
			}

			int n = 0;
			foreach ( var row in file?.heroes ?? new List<HeroRow>() )
			{
				if ( row == null || string.IsNullOrEmpty( row.id ) ) continue;
				stats.Add( new HeroProfile
				{
					Id = row.id,
					Name = row.name,
					Health = row.health > 0 ? row.health : 10,
					Endurance = row.endurance > 0 ? row.endurance : 4,
					Speed = row.speed > 0 ? row.speed : 4,
					Defense = row.defense ?? System.Array.Empty<string>(),
				} );
				n++;
			}
			return n;
		}
	}
}
