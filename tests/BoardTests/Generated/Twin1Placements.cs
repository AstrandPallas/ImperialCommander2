// GENERATED from ..\..\ImperialCommander2\Assets\Resources\SagaMissions\Twin\TWIN1.json
// Regenerate with: python tools/gen_mission_fixture.py
// Real mission geometry, so the engine is exercised on a shipped map
// rather than only on hand-written fixtures.
using System.Collections.Generic;

namespace Saga.Board.Tests
{
	public static class Twin1Placements
	{
		public static readonly TilePlacement[] Tiles = new TilePlacement[]
		{
			new TilePlacement { Expansion = "Core", TileId = "10", Side = "B", X = 101, Y = 96, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "18", Side = "B", X = 93, Y = 108, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "18", Side = "B", X = 97, Y = 93, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "18", Side = "B", X = 97, Y = 102, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "23", Side = "A", X = 94, Y = 97, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "32", Side = "A", X = 92, Y = 98, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "38", Side = "A", X = 94, Y = 96, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Twin", TileId = "4", Side = "B", X = 99, Y = 102, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Twin", TileId = "5", Side = "B", X = 100, Y = 93, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Twin", TileId = "6", Side = "B", X = 101, Y = 102, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "7", Side = "B", X = 101, Y = 100, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "15", Side = "B", X = 99, Y = 104, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "14", Side = "B", X = 95, Y = 102, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "A", X = 97, Y = 97, Rotation = 270, SectionGuid = "8b367d6e-8ec4-42f2-97e5-76f17f71362a" },
			new TilePlacement { Expansion = "Core", TileId = "31", Side = "A", X = 97, Y = 97, Rotation = 180, SectionGuid = "8b367d6e-8ec4-42f2-97e5-76f17f71362a" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "A", X = 95, Y = 95, Rotation = 90, SectionGuid = "8b367d6e-8ec4-42f2-97e5-76f17f71362a" },
		};

		public static readonly DoorPlacement[] Doors = new DoorPlacement[]
		{
			new DoorPlacement { X = 95, Y = 96, Rotation = 0, Open = false },
		};

		/// <summary>Active highlights, which is where Rebels start.</summary>
		public static readonly (string Name, int C, int R)[] Highlights =
			new (string, int, int)[]
			{
				( "Entrance", 96, 101 ),
				( "Narrow Path", 98, 95 ),
			};

		/// <summary>Tile dimensions for every tile this mission uses.</summary>
		public static readonly Dictionary<string, (int w, int h)> Dimensions =
			new Dictionary<string, (int w, int h)>
			{
				{ "Core_10", (3, 3) },
				{ "Core_14", (2, 2) },
				{ "Core_15", (2, 2) },
				{ "Core_18", (2, 1) },
				{ "Core_23", (4, 4) },
				{ "Core_31", (2, 2) },
				{ "Core_32", (2, 2) },
				{ "Core_36", (2, 1) },
				{ "Core_38", (2, 2) },
				{ "Core_7", (4, 4) },
				{ "Twin_4", (6, 6) },
				{ "Twin_5", (3, 8) },
				{ "Twin_6", (5, 3) },
			};

		/// <summary>Playable shape per tile face, taken from the art's
		/// alpha channel. '-' is a square outside the tile. In Imperial
		/// Assault the tile's SHAPE is the wall, so these are load-bearing
		/// rather than cosmetic: 22% of tile faces are non-rectangular,
		/// and honouring the shapes removes 84% of apparent tile overlaps.</summary>
		public static readonly TileTerrain[] Shapes = new TileTerrain[]
		{
			new TileTerrain { Expansion = "Core", TileId = "10", Side = "B", Width = 3, Height = 3, Rows = new[] { "...", ".d.", "..." } },
			new TileTerrain { Expansion = "Core", TileId = "14", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "15", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "18", Side = "B", Width = 2, Height = 1, Rows = new[] { ".." } },
			new TileTerrain { Expansion = "Core", TileId = "23", Side = "A", Width = 4, Height = 4, Rows = new[] { "....", "....", ".X..", "...." } },
			new TileTerrain { Expansion = "Core", TileId = "31", Side = "A", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "32", Side = "A", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "36", Side = "A", Width = 2, Height = 1, Rows = new[] { ".." } },
			new TileTerrain { Expansion = "Core", TileId = "38", Side = "A", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "7", Side = "B", Width = 4, Height = 4, Rows = new[] { "...-", "....", "....", "-..." } },
			new TileTerrain { Expansion = "Twin", TileId = "4", Side = "B", Width = 6, Height = 6, Rows = new[] { "----..", "----..", "--....", "--....", "......", "......" } },
			new TileTerrain { Expansion = "Twin", TileId = "5", Side = "B", Width = 3, Height = 8, Rows = new[] { "...", "...", ".X-", ".X-", "..-", "..-", "X..", "X.." } },
			new TileTerrain { Expansion = "Twin", TileId = "6", Side = "B", Width = 5, Height = 3, Rows = new[] { "..X..", "..X..", "....." } },
		};

		public static TerrainLibrary Library()
		{
			var lib = new TerrainLibrary();
			foreach ( var t in Shapes ) lib.Add( t );
			return lib;
		}

		public static (int w, int h)? Lookup( string exp, string id )
			=> Dimensions.TryGetValue( exp + "_" + id, out var d ) ? d : ((int, int)?)null;
	}
}
