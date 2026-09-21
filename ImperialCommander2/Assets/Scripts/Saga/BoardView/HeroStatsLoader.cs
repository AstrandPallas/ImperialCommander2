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
			var asset = Resources.Load<TextAsset>( ResourcePath );
			if ( asset == null )
			{
				Utils.LogWarning( "HeroStatsLoader::" + ResourcePath
					+ " is missing, so heroes fall back to 10 health and 4 endurance" );
				return stats;
			}

			HeroStatsFile file = null;
			try
			{
				file = JsonConvert.DeserializeObject<HeroStatsFile>( asset.text );
			}
			catch ( System.Exception e )
			{
				Utils.LogWarning( "HeroStatsLoader::could not read " + ResourcePath
					+ ": " + e.Message );
			}

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
			}

			Utils.LogWarning( "HeroStatsLoader::" + stats.Count + " hero sheets loaded" );
			return stats;
		}
	}
}
