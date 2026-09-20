using System;

namespace Saga.Board
{
	/// <summary>Terrain occupying a space.</summary>
	[Flags]
	public enum SquareFlags
	{
		None = 0,
		/// <summary>Ordinary space.</summary>
		Normal = 1 << 0,
		/// <summary>+1 MP to enter (2 total).</summary>
		Difficult = 1 << 1,
		/// <summary>Cannot enter, cannot count spaces through, and BLOCKS line of sight.</summary>
		Blocking = 1 << 2,
		/// <summary>Cannot enter, but spaces remain ADJACENT and line of sight PASSES THROUGH.</summary>
		Impassable = 1 << 3,
		/// <summary>Chasm or pit.</summary>
		Pit = 1 << 4,
		/// <summary>Not a playable space at all (tile art with a non-rectangular play area).</summary>
		Void = 1 << 5,
	}

	/// <summary>What sits on the boundary between two spaces.</summary>
	public enum EdgeType
	{

		Open = 0,

		Wall = 1,
		/// <summary>Blocking on this edge only.</summary>
		Blocking = 2,
		/// <summary>Impassable on this edge only.</summary>
		Impassable = 3,
		/// <summary>A printed door frame with no door token placed.</summary>
		Doorway = 4,
		/// <summary>A closed door.</summary>
		DoorClosed = 5,
		/// <summary>An opened door.</summary>
		DoorOpen = 6,
	}

	/// <summary>Canonical edge direction.</summary>
	public enum EdgeDir
	{
		N = 0,
		W = 1,
	}

	/// <summary>Footprint of a figure, from the deployment card's miniSize.</summary>
	public enum Footprint
	{
		Small1x1 = 0,
		Medium1x2 = 1,
		Large2x2 = 2,
		Huge2x3 = 3,
	}

	/// <summary>Orientation of a non-square footprint.</summary>
	public enum Facing
	{
		NorthSouth = 0,
		EastWest = 1,
	}
}
