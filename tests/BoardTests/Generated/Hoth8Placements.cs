// GENERATED from ..\..\ImperialCommander2\Assets\Resources\SagaMissions\Hoth\HOTH8.json
// Regenerate with: python tools/gen_mission_fixture.py
// Real mission geometry, so the engine is exercised on a shipped map
// rather than only on hand-written fixtures.
using System.Collections.Generic;

namespace Saga.Board.Tests
{
	public static class Hoth8Placements
	{
		public static readonly TilePlacement[] Tiles = new TilePlacement[]
		{
			new TilePlacement { Expansion = "Hoth", TileId = "5", Side = "A", X = 92, Y = 94, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "20", Side = "A", X = 92, Y = 94, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "2", Side = "A", X = 104, Y = 103, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "5", Side = "A", X = 103, Y = 103, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "6", Side = "A", X = 99, Y = 99, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "7", Side = "A", X = 99, Y = 104, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "10", Side = "A", X = 95, Y = 104, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "14", Side = "A", X = 103, Y = 98, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "17", Side = "A", X = 103, Y = 98, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "17", Side = "A", X = 100, Y = 96, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "18", Side = "A", X = 100, Y = 109, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "1", Side = "A", X = 98, Y = 103, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
		};

		public static readonly DoorPlacement[] Doors = new DoorPlacement[]
		{
		};

		/// <summary>Active highlights, which is where Rebels start.</summary>
		public static readonly (string Name, int C, int R)[] Highlights =
			new (string, int, int)[]
			{
				( "Entrance", 101, 106 ),
			};

		/// <summary>Tile dimensions for every tile this mission uses.</summary>
		public static readonly Dictionary<string, (int w, int h)> Dimensions =
			new Dictionary<string, (int w, int h)>
			{
				{ "Core_1", (6, 6) },
				{ "Core_10", (3, 3) },
				{ "Core_14", (2, 2) },
				{ "Core_17", (1, 2) },
				{ "Core_18", (2, 1) },
				{ "Core_2", (4, 6) },
				{ "Core_5", (4, 4) },
				{ "Core_6", (4, 4) },
				{ "Core_7", (4, 4) },
				{ "Hoth_20", (2, 1) },
				{ "Hoth_5", (8, 6) },
			};

		/// <summary>Playable shape per tile face, taken from the art's
		/// alpha channel. '-' is a square outside the tile. In Imperial
		/// Assault the tile's SHAPE is the wall, so these are load-bearing
		/// rather than cosmetic: 22% of tile faces are non-rectangular,
		/// and honouring the shapes removes 84% of apparent tile overlaps.</summary>
		public static readonly TileTerrain[] Shapes = new TileTerrain[]
		{
			new TileTerrain { Expansion = "Core", TileId = "1", Side = "A", Width = 6, Height = 6, Rows = new[] { "......", ".XX.X.", "...X..", "......", "......", "......" } },
			new TileTerrain { Expansion = "Core", TileId = "10", Side = "A", Width = 3, Height = 3, Rows = new[] { "...", "...", "..." } },
			new TileTerrain { Expansion = "Core", TileId = "14", Side = "A", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "17", Side = "A", Width = 1, Height = 2, Rows = new[] { ".", "." } },
			new TileTerrain { Expansion = "Core", TileId = "18", Side = "A", Width = 2, Height = 1, Rows = new[] { ".." } },
			new TileTerrain { Expansion = "Core", TileId = "2", Side = "A", Width = 4, Height = 6, Rows = new[] { "....", "....", "..X.", "....", "X...", "...." }, Edges = new[] { new TileEdge { C = 2, R = 1, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 3, R = 1, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 2, R = 5, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 3, R = 5, Dir = "N", Type = EdgeType.Impassable } } },
			new TileTerrain { Expansion = "Core", TileId = "5", Side = "A", Width = 4, Height = 4, Rows = new[] { "....", "..dd", ".ddd", "ddd." } },
			new TileTerrain { Expansion = "Core", TileId = "6", Side = "A", Width = 4, Height = 4, Rows = new[] { "-...", ".X..", "....", "...." } },
			new TileTerrain { Expansion = "Core", TileId = "7", Side = "A", Width = 4, Height = 4, Rows = new[] { "-...", "....", "..dd", "..d-" } },
			new TileTerrain { Expansion = "Hoth", TileId = "20", Side = "A", Width = 2, Height = 1, Rows = new[] { ".." } },
			new TileTerrain { Expansion = "Hoth", TileId = "5", Side = "A", Width = 8, Height = 6, Rows = new[] { "....----", "....----", "........", "........", "........", "......--" }, Edges = new[] { new TileEdge { C = 0, R = 2, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 1, R = 2, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 6, R = 2, Dir = "W", Type = EdgeType.Impassable }, new TileEdge { C = 4, R = 3, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 4, R = 3, Dir = "W", Type = EdgeType.Impassable }, new TileEdge { C = 5, R = 3, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 2, R = 4, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 3, R = 4, Dir = "N", Type = EdgeType.Impassable } } },
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
