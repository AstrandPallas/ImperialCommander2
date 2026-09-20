// GENERATED from ..\..\ImperialCommander2\Assets\Resources\SagaMissions\Twin\TWIN3.json
// Regenerate with: python tools/gen_mission_fixture.py
// Real mission geometry, so the engine is exercised on a shipped map
// rather than only on hand-written fixtures.
using System.Collections.Generic;

namespace Saga.Board.Tests
{
	public static class Twin3Placements
	{
		public static readonly TilePlacement[] Tiles = new TilePlacement[]
		{
			new TilePlacement { Expansion = "Core", TileId = "5", Side = "B", X = 100, Y = 106, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "17", Side = "B", X = 103, Y = 101, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "22", Side = "B", X = 100, Y = 98, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "23", Side = "B", X = 95, Y = 105, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "28", Side = "B", X = 99, Y = 109, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "32", Side = "B", X = 100, Y = 109, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "B", X = 100, Y = 109, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Twin", TileId = "7", Side = "B", X = 100, Y = 102, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "38", Side = "A", X = 101, Y = 101, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "37", Side = "B", X = 97, Y = 98, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Twin", TileId = "6", Side = "A", X = 102, Y = 96, Rotation = 180, SectionGuid = "97b8f546-f516-412d-98f6-3a63b90eb32a" },
			new TilePlacement { Expansion = "Twin", TileId = "3", Side = "A", X = 103, Y = 96, Rotation = 90, SectionGuid = "97b8f546-f516-412d-98f6-3a63b90eb32a" },
		};

		public static readonly DoorPlacement[] Doors = new DoorPlacement[]
		{
			new DoorPlacement { X = 97, Y = 96, Rotation = 0, Open = false },
			new DoorPlacement { X = 101, Y = 98, Rotation = 0, Open = false },
		};

		/// <summary>Tile dimensions for every tile this mission uses.</summary>
		public static readonly Dictionary<string, (int w, int h)> Dimensions =
			new Dictionary<string, (int w, int h)>
			{
				{ "Core_17", (1, 2) },
				{ "Core_22", (4, 4) },
				{ "Core_23", (4, 4) },
				{ "Core_28", (3, 3) },
				{ "Core_32", (2, 2) },
				{ "Core_36", (2, 1) },
				{ "Core_37", (2, 2) },
				{ "Core_38", (2, 2) },
				{ "Core_5", (4, 4) },
				{ "Twin_3", (3, 3) },
				{ "Twin_6", (5, 3) },
				{ "Twin_7", (3, 4) },
			};

		/// <summary>Playable shape per tile face, taken from the art's
		/// alpha channel. '-' is a square outside the tile. In Imperial
		/// Assault the tile's SHAPE is the wall, so these are load-bearing
		/// rather than cosmetic: 22% of tile faces are non-rectangular,
		/// and honouring the shapes removes 84% of apparent tile overlaps.</summary>
		public static readonly TileTerrain[] Shapes = new TileTerrain[]
		{
			new TileTerrain { Expansion = "Core", TileId = "17", Side = "B", Width = 1, Height = 2, Rows = new[] { ".", "." } },
			new TileTerrain { Expansion = "Core", TileId = "22", Side = "B", Width = 4, Height = 4, Rows = new[] { "....", "....", "....", "...." }, Edges = new[] { new TileEdge { C = 2, R = 1, Dir = "W", Type = EdgeType.Blocking }, new TileEdge { C = 1, R = 2, Dir = "N", Type = EdgeType.Blocking }, new TileEdge { C = 2, R = 2, Dir = "N", Type = EdgeType.Blocking } } },
			new TileTerrain { Expansion = "Core", TileId = "23", Side = "B", Width = 4, Height = 4, Rows = new[] { "....", "....", "..X.", "...." } },
			new TileTerrain { Expansion = "Core", TileId = "28", Side = "B", Width = 3, Height = 3, Rows = new[] { "...", "...", "..." } },
			new TileTerrain { Expansion = "Core", TileId = "32", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "36", Side = "B", Width = 2, Height = 1, Rows = new[] { ".." } },
			new TileTerrain { Expansion = "Core", TileId = "37", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "38", Side = "A", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "5", Side = "B", Width = 4, Height = 4, Rows = new[] { "....", ".X..", "....", "...." }, Edges = new[] { new TileEdge { C = 0, R = 2, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 3, R = 3, Dir = "N", Type = EdgeType.Impassable } } },
			new TileTerrain { Expansion = "Twin", TileId = "3", Side = "A", Width = 3, Height = 3, Rows = new[] { "-..", "...", "..." } },
			new TileTerrain { Expansion = "Twin", TileId = "6", Side = "A", Width = 5, Height = 3, Rows = new[] { "..X..", "..X..", "....." } },
			new TileTerrain { Expansion = "Twin", TileId = "7", Side = "B", Width = 3, Height = 4, Rows = new[] { "...", "...", "...", "..." } },
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
