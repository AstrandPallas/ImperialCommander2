// GENERATED from ..\..\ImperialCommander2\Assets\Resources\SagaMissions\Lothal\LOTHAL2.json
// Regenerate with: python tools/gen_mission_fixture.py
// Real mission geometry, so the engine is exercised on a shipped map
// rather than only on hand-written fixtures.
using System.Collections.Generic;

namespace Saga.Board.Tests
{
	public static class Lothal2Placements
	{
		public static readonly TilePlacement[] Tiles = new TilePlacement[]
		{
			new TilePlacement { Expansion = "Lothal", TileId = "2", Side = "A", X = 98, Y = 106, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "2", Side = "A", X = 102, Y = 106, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "3", Side = "A", X = 109, Y = 95, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "7", Side = "A", X = 103, Y = 101, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "17", Side = "A", X = 102, Y = 100, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "18", Side = "A", X = 102, Y = 108, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "18", Side = "A", X = 109, Y = 99, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "18", Side = "A", X = 107, Y = 95, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "38", Side = "B", X = 102, Y = 98, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "28", Side = "B", X = 103, Y = 98, Rotation = 180, SectionGuid = "da5c7f0f-a2af-4e8c-b29c-845dd85ceb99" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "B", X = 103, Y = 97, Rotation = 270, SectionGuid = "da5c7f0f-a2af-4e8c-b29c-845dd85ceb99" },
		};

		public static readonly DoorPlacement[] Doors = new DoorPlacement[]
		{
			new DoorPlacement { X = 100, Y = 97, Rotation = 0, Open = false },
		};

		/// <summary>Tile dimensions for every tile this mission uses.</summary>
		public static readonly Dictionary<string, (int w, int h)> Dimensions =
			new Dictionary<string, (int w, int h)>
			{
				{ "Core_17", (1, 2) },
				{ "Core_18", (2, 1) },
				{ "Core_2", (4, 6) },
				{ "Core_28", (3, 3) },
				{ "Core_3", (6, 5) },
				{ "Core_36", (2, 1) },
				{ "Core_38", (2, 2) },
				{ "Core_7", (4, 4) },
				{ "Lothal_2", (5, 5) },
			};

		/// <summary>Playable shape per tile face, taken from the art's
		/// alpha channel. '-' is a square outside the tile. In Imperial
		/// Assault the tile's SHAPE is the wall, so these are load-bearing
		/// rather than cosmetic: 22% of tile faces are non-rectangular,
		/// and honouring the shapes removes 84% of apparent tile overlaps.</summary>
		public static readonly TileTerrain[] Shapes = new TileTerrain[]
		{
			new TileTerrain { Expansion = "Core", TileId = "17", Side = "A", Width = 1, Height = 2, Rows = new[] { ".", "." } },
			new TileTerrain { Expansion = "Core", TileId = "18", Side = "A", Width = 2, Height = 1, Rows = new[] { ".." } },
			new TileTerrain { Expansion = "Core", TileId = "2", Side = "A", Width = 4, Height = 6, Rows = new[] { "....", "....", "..X.", "....", "X...", "...." }, Edges = new[] { new TileEdge { C = 2, R = 1, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 3, R = 1, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 2, R = 5, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 3, R = 5, Dir = "N", Type = EdgeType.Impassable } } },
			new TileTerrain { Expansion = "Core", TileId = "28", Side = "B", Width = 3, Height = 3, Rows = new[] { "...", "...", "..." } },
			new TileTerrain { Expansion = "Core", TileId = "3", Side = "A", Width = 6, Height = 5, Rows = new[] { "--..--", "-....-", "......", "......", "-....-" } },
			new TileTerrain { Expansion = "Core", TileId = "36", Side = "B", Width = 2, Height = 1, Rows = new[] { ".." } },
			new TileTerrain { Expansion = "Core", TileId = "38", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "7", Side = "A", Width = 4, Height = 4, Rows = new[] { "-...", "....", "..dd", "..d-" } },
			new TileTerrain { Expansion = "Lothal", TileId = "2", Side = "A", Width = 5, Height = 5, Rows = new[] { ".....", ".X.X.", ".....", ".X.X.", "....." } },
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
