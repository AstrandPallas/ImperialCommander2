using System;
using System.Collections.Generic;

namespace Saga.Board
{
	/// <summary>Translates the game's mission data into the rules engine's board model.</summary>
	public static class SagaBoardBridge
	{
		/// <summary>One tile as the engine needs to see it.</summary>
		public struct TileInput
		{
			public string Expansion;
			public string TileId;
			public string Side;
			public int X;
			public int Y;
			public int Rotation;
			public string SectionGuid;
		}

		/// <summary>One door as the engine needs to see it.</summary>
		public struct DoorInput
		{
			public int X;
			public int Y;
			public int Rotation;
			public bool Open;
		}

		/// <summary>The board for the currently active sections, or null before a mission has been set up.</summary>
		public static BoardModel Current { get; private set; }

		/// <summary>Diagnostics from the last build, for the debug overlay.</summary>
		public static BoardBuilder.BuildResult LastBuild { get; private set; }

		/// <summary>Rebuild the board from the mission's tiles, honouring which sections are currently active.</summary>
		public static BoardModel Rebuild(
			IEnumerable<TileInput> tiles,
			IEnumerable<DoorInput> doors,
			Func<string, string, (int w, int h)?> dimensions,
			TerrainLibrary terrain,
			Func<string, bool> isSectionActive )
		{
			var placements = new List<TilePlacement>();
			foreach ( var t in tiles )
			{
				placements.Add( new TilePlacement
				{
					Expansion = t.Expansion,
					TileId = t.TileId,
					Side = t.Side,
					X = t.X,
					Y = t.Y,
					Rotation = t.Rotation,
					SectionGuid = t.SectionGuid,
				} );
			}

			var doorList = new List<DoorPlacement>();
			foreach ( var d in doors )
				doorList.Add( new DoorPlacement
				{ X = d.X, Y = d.Y, Rotation = d.Rotation, Open = d.Open } );

			LastBuild = BoardBuilder.Build( placements.ToArray(), dimensions,
				doorList.ToArray(), terrain, isSectionActive );
			Current = LastBuild.Board;
			return Current;
		}

		/// <summary>Forget the board.</summary>
		public static void Clear()
		{
			Current = null;
			LastBuild = null;
		}
	}
}
