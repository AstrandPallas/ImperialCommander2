// GENERATED from ..\..\ImperialCommander2\Assets\Resources\SagaMissions\Core\CORE8.json
// Regenerate with: python tools/gen_mission_fixture.py
// Real mission geometry, so the engine is exercised on a shipped map
// rather than only on hand-written fixtures.
using System.Collections.Generic;

namespace Saga.Board.Tests
{
	public static class Core8Placements
	{
		public static readonly TilePlacement[] Tiles = new TilePlacement[]
		{
			new TilePlacement { Expansion = "Core", TileId = "3", Side = "A", X = 96, Y = 102, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "5", Side = "A", X = 101, Y = 106, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "10", Side = "A", X = 94, Y = 106, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "12", Side = "A", X = 97, Y = 107, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "14", Side = "A", X = 104, Y = 106, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "14", Side = "A", X = 96, Y = 104, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "38", Side = "B", X = 100, Y = 100, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "26", Side = "B", X = 101, Y = 96, Rotation = 90, SectionGuid = "b18b419b-9db1-4b44-b4e2-99c0c0533b94" },
			new TilePlacement { Expansion = "Core", TileId = "32", Side = "B", X = 97, Y = 99, Rotation = 180, SectionGuid = "b18b419b-9db1-4b44-b4e2-99c0c0533b94" },
			new TilePlacement { Expansion = "Core", TileId = "32", Side = "B", X = 101, Y = 99, Rotation = 270, SectionGuid = "b18b419b-9db1-4b44-b4e2-99c0c0533b94" },
			new TilePlacement { Expansion = "Core", TileId = "22", Side = "B", X = 98, Y = 99, Rotation = 90, SectionGuid = "b18b419b-9db1-4b44-b4e2-99c0c0533b94" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "B", X = 95, Y = 103, Rotation = 0, SectionGuid = "b18b419b-9db1-4b44-b4e2-99c0c0533b94" },
			new TilePlacement { Expansion = "Core", TileId = "23", Side = "B", X = 100, Y = 99, Rotation = 0, SectionGuid = "b18b419b-9db1-4b44-b4e2-99c0c0533b94" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "B", X = 104, Y = 102, Rotation = 270, SectionGuid = "b18b419b-9db1-4b44-b4e2-99c0c0533b94" },
			new TilePlacement { Expansion = "Core", TileId = "25", Side = "B", X = 101, Y = 96, Rotation = 180, SectionGuid = "b18b419b-9db1-4b44-b4e2-99c0c0533b94" },
			new TilePlacement { Expansion = "Core", TileId = "24", Side = "B", X = 105, Y = 97, Rotation = 180, SectionGuid = "b18b419b-9db1-4b44-b4e2-99c0c0533b94" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "B", X = 103, Y = 97, Rotation = 0, SectionGuid = "b18b419b-9db1-4b44-b4e2-99c0c0533b94" },
			new TilePlacement { Expansion = "Core", TileId = "28", Side = "B", X = 97, Y = 96, Rotation = 180, SectionGuid = "b18b419b-9db1-4b44-b4e2-99c0c0533b94" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "B", X = 94, Y = 96, Rotation = 0, SectionGuid = "b18b419b-9db1-4b44-b4e2-99c0c0533b94" },
		};

		public static readonly DoorPlacement[] Doors = new DoorPlacement[]
		{
		};

		/// <summary>Active highlights, which is where Rebels start.</summary>
		public static readonly (string Name, int C, int R)[] Highlights =
			new (string, int, int)[]
			{
				( "Entrance", 99, 108 ),
				( "Corridor Entrance", 98, 100 ),
			};

		/// <summary>Tile dimensions for every tile this mission uses.</summary>
		public static readonly Dictionary<string, (int w, int h)> Dimensions =
			new Dictionary<string, (int w, int h)>
			{
				{ "Core_10", (3, 3) },
				{ "Core_12", (4, 2) },
				{ "Core_14", (2, 2) },
				{ "Core_22", (4, 4) },
				{ "Core_23", (4, 4) },
				{ "Core_24", (4, 4) },
				{ "Core_25", (4, 4) },
				{ "Core_26", (4, 4) },
				{ "Core_28", (3, 3) },
				{ "Core_3", (6, 5) },
				{ "Core_32", (2, 2) },
				{ "Core_36", (2, 1) },
				{ "Core_38", (2, 2) },
				{ "Core_5", (4, 4) },
			};

		/// <summary>Playable shape per tile face, taken from the art's
		/// alpha channel. '-' is a square outside the tile. In Imperial
		/// Assault the tile's SHAPE is the wall, so these are load-bearing
		/// rather than cosmetic: 22% of tile faces are non-rectangular,
		/// and honouring the shapes removes 84% of apparent tile overlaps.</summary>
		public static readonly TileTerrain[] Shapes = new TileTerrain[]
		{
			new TileTerrain { Expansion = "Core", TileId = "10", Side = "A", Width = 3, Height = 3, Rows = new[] { "...", "...", "..." } },
			new TileTerrain { Expansion = "Core", TileId = "12", Side = "A", Width = 4, Height = 2, Rows = new[] { "....", "...." } },
			new TileTerrain { Expansion = "Core", TileId = "14", Side = "A", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "22", Side = "B", Width = 4, Height = 4, Rows = new[] { "....", "....", "....", "...." }, Edges = new[] { new TileEdge { C = 2, R = 1, Dir = "W", Type = EdgeType.Blocking }, new TileEdge { C = 1, R = 2, Dir = "N", Type = EdgeType.Blocking }, new TileEdge { C = 2, R = 2, Dir = "N", Type = EdgeType.Blocking } } },
			new TileTerrain { Expansion = "Core", TileId = "23", Side = "B", Width = 4, Height = 4, Rows = new[] { "....", "....", "..X.", "...." } },
			new TileTerrain { Expansion = "Core", TileId = "24", Side = "B", Width = 4, Height = 4, Rows = new[] { "...-", "....", "....", "-..." } },
			new TileTerrain { Expansion = "Core", TileId = "25", Side = "B", Width = 4, Height = 4, Rows = new[] { "-..-", "....", "....", "...." } },
			new TileTerrain { Expansion = "Core", TileId = "26", Side = "B", Width = 4, Height = 4, Rows = new[] { "-..-", ".X..", "....", "-..-" } },
			new TileTerrain { Expansion = "Core", TileId = "28", Side = "B", Width = 3, Height = 3, Rows = new[] { "...", "...", "..." } },
			new TileTerrain { Expansion = "Core", TileId = "3", Side = "A", Width = 6, Height = 5, Rows = new[] { "--..--", "-....-", "......", "......", "-....-" } },
			new TileTerrain { Expansion = "Core", TileId = "32", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "36", Side = "B", Width = 2, Height = 1, Rows = new[] { ".." } },
			new TileTerrain { Expansion = "Core", TileId = "38", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "5", Side = "A", Width = 4, Height = 4, Rows = new[] { "....", "..dd", ".ddd", "ddd." } },
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
