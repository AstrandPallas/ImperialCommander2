using System;
using System.Collections.Generic;
using Saga.Board;

namespace Saga.Tracking
{
	/// <summary>
	/// The parts of a deployment card the board engine needs: how far a figure
	/// moves, how it attacks, how much room it takes up, and the keywords that
	/// change the rules for it.
	/// </summary>
	/// <remarks>
	/// This is kept free of UnityEngine and takes plain strings rather than a
	/// DeploymentCard so the mapping can be tested headlessly. The mapping is
	/// the part worth testing: the engine was complete and correct long before
	/// anything fed it a real card, and every figure was being planned as a
	/// ranged 1x1 with speed 4 because these fields never crossed the gap.
	/// </remarks>
	public sealed class UnitProfile
	{
		/// <summary>Spaces per move action.</summary>
		public int Speed = 4;

		public AttackKind AttackKind = AttackKind.Ranged;

		public Footprint Footprint = Footprint.Small1x1;

		public bool Massive;

		public bool Mobile;

		/// <summary>The Reach keyword, named so it cannot be read as <see cref="Saga.Board.Reach"/>.</summary>
		public bool HasReach;

		public static readonly UnitProfile Default = new UnitProfile();

		/// <summary>
		/// Build a profile from the raw card fields.
		/// </summary>
		/// <remarks>
		/// Anything unrecognised keeps its default rather than throwing. A card
		/// this does not understand should produce a figure the app plans
		/// conservatively, not a mission that cannot be played.
		/// </remarks>
		public static UnitProfile From( string attackType, string miniSize, int speed,
			IEnumerable<string> keywords )
		{
			var p = new UnitProfile();

			if ( speed > 0 ) p.Speed = speed;

			if ( string.Equals( attackType, "Melee", StringComparison.OrdinalIgnoreCase ) )
				p.AttackKind = AttackKind.Melee;
			else if ( string.Equals( attackType, "Ranged", StringComparison.OrdinalIgnoreCase ) )
				p.AttackKind = AttackKind.Ranged;

			if ( !string.IsNullOrEmpty( miniSize )
				&& Enum.TryParse( miniSize, true, out Footprint fp ) )
				p.Footprint = fp;

			foreach ( var k in keywords ?? Array.Empty<string>() )
			{
				// Only the whole word counts. The keyword list also carries
				// entries like "Cleave 2 {H}" and "+3 Accuracy", so matching on
				// a substring would start finding keywords that are not there.
				var word = (k ?? string.Empty).Trim();
				if ( Matches( word, "Massive" ) ) p.Massive = true;
				else if ( Matches( word, "Mobile" ) ) p.Mobile = true;
				else if ( Matches( word, "Reach" ) ) p.HasReach = true;
			}

			return p;
		}

		private static bool Matches( string word, string keyword )
			=> string.Equals( word, keyword, StringComparison.OrdinalIgnoreCase );

		public override string ToString()
		{
			var flags = (Massive ? " Massive" : "") + (Mobile ? " Mobile" : "")
				+ (HasReach ? " Reach" : "");
			return $"speed {Speed}, {AttackKind}, {Footprint}{flags}";
		}
	}
}
