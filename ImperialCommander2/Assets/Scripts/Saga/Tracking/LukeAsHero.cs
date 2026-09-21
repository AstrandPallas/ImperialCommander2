using System;
using System.Collections.Generic;
using System.Linq;

namespace Saga.Tracking
{
	/// <summary>Something about the campaign that will not work as set up.</summary>
	public sealed class SetupConflict
	{
		public string What;
		public string Fix;

		public override string ToString() => What + " -- " + Fix;
	}

	/// <summary>
	/// Playing Luke Skywalker as a campaign hero rather than as an ally.
	/// </summary>
	/// <remarks>
	/// HOMEBREW. Luke ships as ally A011 with a full stat block, no hero sheet
	/// and no class deck; both were designed for this fork and neither is an
	/// FFG conversion. See docs/luke-jedi-knight-homebrew.md.
	///
	/// Two things break if nobody checks. He cannot be a hero and an ally in
	/// the same campaign -- the ally card is the same character, at ally power,
	/// and recruiting himself is nonsense. And his own side mission, OTHER2
	/// "A Light in the Darkness", ships in this repo with a reward that reads
	/// "gain Luke Skywalker (Jedi Knight) as an ally", which is void if he is
	/// already standing on the board.
	///
	/// Both are reported rather than silently corrected. Quietly removing a
	/// card the players chose, or rewriting a mission's reward behind their
	/// backs, is exactly the kind of thing that makes an app untrustworthy at
	/// the table.
	/// </remarks>
	public static class LukeAsHero
	{
		/// <summary>The hero entry authored for this fork.</summary>
		public const string HeroId = "H22";

		/// <summary>The ally he ships as.</summary>
		public const string AllyId = "A011";

		/// <summary>His side mission, whose reward recruits the ally.</summary>
		public const string SideMissionId = "OTHER2";

		public static bool IsPlayingAsHero( IEnumerable<string> partyHeroIds )
			=> partyHeroIds != null
				&& partyHeroIds.Any( id => string.Equals( id, HeroId,
					StringComparison.OrdinalIgnoreCase ) );

		/// <summary>
		/// Everything about this campaign that needs a decision before play.
		/// </summary>
		/// <remarks>
		/// Returns an empty list for every campaign that is not using the
		/// homebrew hero, so an ordinary campaign is untouched.
		/// </remarks>
		public static List<SetupConflict> Check(
			IEnumerable<string> partyHeroIds,
			IEnumerable<string> availableAllyIds = null,
			IEnumerable<string> ignoredIds = null,
			IEnumerable<string> plannedMissionIds = null )
		{
			var conflicts = new List<SetupConflict>();
			if ( !IsPlayingAsHero( partyHeroIds ) ) return conflicts;

			var ignored = new HashSet<string>(
				ignoredIds ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase );

			bool allyAvailable = (availableAllyIds ?? Enumerable.Empty<string>())
				.Any( id => string.Equals( id, AllyId, StringComparison.OrdinalIgnoreCase ) );

			if ( allyAvailable && !ignored.Contains( AllyId ) )
				conflicts.Add( new SetupConflict
				{
					What = "Luke is in the party as a hero, and is also still in the ally pool",
					Fix = "add " + AllyId + " to the campaign's ignored list -- he cannot be "
						+ "recruited as an ally version of himself",
				} );

			bool sideMissionPlanned = (plannedMissionIds ?? Enumerable.Empty<string>())
				.Any( id => string.Equals( id, SideMissionId, StringComparison.OrdinalIgnoreCase ) );

			if ( sideMissionPlanned )
				conflicts.Add( new SetupConflict
				{
					What = SideMissionId + " rewards the party with Luke as an ally, which is "
						+ "void while he is a hero",
					Fix = "run it as Luke's personal mission instead: keep the map and the "
						+ "encounter, replace the arrival beat, and award a free class card "
						+ "or Luke-only XP (docs/luke-jedi-knight-homebrew.md)",
				} );

			return conflicts;
		}

		/// <summary>The ally ids a campaign should exclude, given its party.</summary>
		/// <remarks>
		/// Returned for the caller to apply, rather than applied here. Removing
		/// something from the players' own campaign without being asked is not
		/// this function's decision to make.
		/// </remarks>
		public static IEnumerable<string> AlliesToExclude( IEnumerable<string> partyHeroIds )
			=> IsPlayingAsHero( partyHeroIds )
				? new[] { AllyId }
				: Array.Empty<string>();
	}
}
