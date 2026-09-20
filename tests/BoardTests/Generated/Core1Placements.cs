// GENERATED from ..\..\ImperialCommander2\Assets\Resources\SagaMissions\Core\CORE1.json
// Regenerate with: python tools/gen_mission_fixture.py
// Real mission geometry, so the engine is exercised on a shipped map
// rather than only on hand-written fixtures.
using System.Collections.Generic;

namespace Saga.Board.Tests
{
	public static class Core1Placements
	{
		public static readonly TilePlacement[] Tiles = new TilePlacement[]
		{
			new TilePlacement { Expansion = "Core", TileId = "1", Side = "A", X = 96, Y = 98, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "2", Side = "A", X = 102, Y = 98, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "14", Side = "A", X = 104, Y = 98, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "38", Side = "B", X = 100, Y = 111, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "14", Side = "A", X = 96, Y = 100, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "12", Side = "A", X = 96, Y = 102, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "17", Side = "A", X = 96, Y = 106, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "7", Side = "A", X = 98, Y = 107, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "38", Side = "B", X = 100, Y = 96, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "4", Side = "A", X = 105, Y = 108, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "5", Side = "A", X = 101, Y = 104, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "10", Side = "A", X = 105, Y = 107, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "38", Side = "B", X = 108, Y = 102, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "25", Side = "B", X = 97, Y = 95, Rotation = 90, SectionGuid = "bf444e72-ce40-4eb1-810b-57b7e1a7a287" },
			new TilePlacement { Expansion = "Core", TileId = "28", Side = "B", X = 96, Y = 96, Rotation = 270, SectionGuid = "bf444e72-ce40-4eb1-810b-57b7e1a7a287" },
			new TilePlacement { Expansion = "Core", TileId = "31", Side = "B", X = 97, Y = 96, Rotation = 0, SectionGuid = "bf444e72-ce40-4eb1-810b-57b7e1a7a287" },
			new TilePlacement { Expansion = "Core", TileId = "32", Side = "B", X = 96, Y = 95, Rotation = 180, SectionGuid = "bf444e72-ce40-4eb1-810b-57b7e1a7a287" },
			new TilePlacement { Expansion = "Core", TileId = "35", Side = "B", X = 99, Y = 96, Rotation = 0, SectionGuid = "bf444e72-ce40-4eb1-810b-57b7e1a7a287" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "B", X = 94, Y = 99, Rotation = 0, SectionGuid = "bf444e72-ce40-4eb1-810b-57b7e1a7a287" },
			new TilePlacement { Expansion = "Core", TileId = "33", Side = "B", X = 108, Y = 102, Rotation = 180, SectionGuid = "a35ed683-5641-420d-88c8-363f1c02a80f" },
			new TilePlacement { Expansion = "Core", TileId = "22", Side = "B", X = 100, Y = 108, Rotation = 0, SectionGuid = "fd7a53a2-a400-48ad-9cc2-c3ed042eafe9" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "B", X = 104, Y = 111, Rotation = 270, SectionGuid = "fd7a53a2-a400-48ad-9cc2-c3ed042eafe9" },
		};

		public static readonly DoorPlacement[] Doors = new DoorPlacement[]
		{
			new DoorPlacement { X = 101, Y = 96, Rotation = 90, Open = false },
			new DoorPlacement { X = 108, Y = 103, Rotation = 180, Open = false },
			new DoorPlacement { X = 99, Y = 111, Rotation = 270, Open = false },
		};

		/// <summary>Active highlights, which is where Rebels start.</summary>
		public static readonly (string Name, int C, int R)[] Highlights =
			new (string, int, int)[]
			{
				( "Entrance", 98, 100 ),
			};

		/// <summary>Tile dimensions for every tile this mission uses.</summary>
		public static readonly Dictionary<string, (int w, int h)> Dimensions =
			new Dictionary<string, (int w, int h)>
			{
				{ "Core_1", (6, 6) },
				{ "Core_10", (3, 3) },
				{ "Core_12", (4, 2) },
				{ "Core_14", (2, 2) },
				{ "Core_17", (1, 2) },
				{ "Core_2", (4, 6) },
				{ "Core_22", (4, 4) },
				{ "Core_25", (4, 4) },
				{ "Core_28", (3, 3) },
				{ "Core_31", (2, 2) },
				{ "Core_32", (2, 2) },
				{ "Core_33", (2, 2) },
				{ "Core_35", (1, 2) },
				{ "Core_36", (2, 1) },
				{ "Core_38", (2, 2) },
				{ "Core_4", (4, 4) },
				{ "Core_5", (4, 4) },
				{ "Core_7", (4, 4) },
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
			new TileTerrain { Expansion = "Core", TileId = "12", Side = "A", Width = 4, Height = 2, Rows = new[] { "....", "...." } },
			new TileTerrain { Expansion = "Core", TileId = "14", Side = "A", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "17", Side = "A", Width = 1, Height = 2, Rows = new[] { ".", "." } },
			new TileTerrain { Expansion = "Core", TileId = "2", Side = "A", Width = 4, Height = 6, Rows = new[] { "....", "....", "..X.", "....", "X...", "...." }, Edges = new[] { new TileEdge { C = 2, R = 1, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 3, R = 1, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 2, R = 5, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 3, R = 5, Dir = "N", Type = EdgeType.Impassable } } },
			new TileTerrain { Expansion = "Core", TileId = "22", Side = "B", Width = 4, Height = 4, Rows = new[] { "....", "....", "....", "...." }, Edges = new[] { new TileEdge { C = 2, R = 1, Dir = "W", Type = EdgeType.Blocking }, new TileEdge { C = 1, R = 2, Dir = "N", Type = EdgeType.Blocking }, new TileEdge { C = 2, R = 2, Dir = "N", Type = EdgeType.Blocking } } },
			new TileTerrain { Expansion = "Core", TileId = "25", Side = "B", Width = 4, Height = 4, Rows = new[] { "-..-", "....", "....", "...." } },
			new TileTerrain { Expansion = "Core", TileId = "28", Side = "B", Width = 3, Height = 3, Rows = new[] { "...", "...", "..." } },
			new TileTerrain { Expansion = "Core", TileId = "31", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "32", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "33", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "35", Side = "B", Width = 1, Height = 2, Rows = new[] { ".", "." } },
			new TileTerrain { Expansion = "Core", TileId = "36", Side = "B", Width = 2, Height = 1, Rows = new[] { ".." } },
			new TileTerrain { Expansion = "Core", TileId = "38", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "4", Side = "A", Width = 4, Height = 4, Rows = new[] { "....", ".Xd.", ".dd.", "...." } },
			new TileTerrain { Expansion = "Core", TileId = "5", Side = "A", Width = 4, Height = 4, Rows = new[] { "....", "..dd", ".ddd", "ddd." } },
			new TileTerrain { Expansion = "Core", TileId = "7", Side = "A", Width = 4, Height = 4, Rows = new[] { "-...", "....", "..dd", "..d-" } },
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
