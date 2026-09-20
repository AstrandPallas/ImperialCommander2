// GENERATED from ..\..\ImperialCommander2\Assets\Resources\SagaMissions\Bespin\BESPIN1.json
// Regenerate with: python tools/gen_mission_fixture.py
// Real mission geometry, so the engine is exercised on a shipped map
// rather than only on hand-written fixtures.
using System.Collections.Generic;

namespace Saga.Board.Tests
{
	public static class Bespin1Placements
	{
		public static readonly TilePlacement[] Tiles = new TilePlacement[]
		{
			new TilePlacement { Expansion = "Core", TileId = "23", Side = "A", X = 100, Y = 97, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "31", Side = "A", X = 103, Y = 97, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "33", Side = "A", X = 96, Y = 106, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "A", X = 98, Y = 104, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "A", X = 103, Y = 97, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Bespin", TileId = "2", Side = "A", X = 96, Y = 98, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Bespin", TileId = "3", Side = "A", X = 96, Y = 104, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Bespin", TileId = "6", Side = "A", X = 101, Y = 95, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Bespin", TileId = "8", Side = "A", X = 100, Y = 100, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "20", Side = "A", X = 100, Y = 103, Rotation = 0, SectionGuid = "15830aba-8636-46f0-a923-a83a71c31db2" },
			new TilePlacement { Expansion = "Core", TileId = "32", Side = "A", X = 104, Y = 101, Rotation = 90, SectionGuid = "15830aba-8636-46f0-a923-a83a71c31db2" },
			new TilePlacement { Expansion = "Core", TileId = "32", Side = "A", X = 106, Y = 103, Rotation = 180, SectionGuid = "15830aba-8636-46f0-a923-a83a71c31db2" },
			new TilePlacement { Expansion = "Core", TileId = "27", Side = "A", X = 104, Y = 109, Rotation = 270, SectionGuid = "15830aba-8636-46f0-a923-a83a71c31db2" },
			new TilePlacement { Expansion = "Core", TileId = "29", Side = "A", X = 103, Y = 109, Rotation = 0, SectionGuid = "15830aba-8636-46f0-a923-a83a71c31db2" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "A", X = 107, Y = 111, Rotation = 270, SectionGuid = "15830aba-8636-46f0-a923-a83a71c31db2" },
			new TilePlacement { Expansion = "Bespin", TileId = "4", Side = "A", X = 103, Y = 112, Rotation = 180, SectionGuid = "cbe8f26c-8c3e-4c9d-80d5-3dc13a96d310" },
		};

		public static readonly DoorPlacement[] Doors = new DoorPlacement[]
		{
			new DoorPlacement { X = 98, Y = 99, Rotation = 0, Open = false },
			new DoorPlacement { X = 104, Y = 109, Rotation = 90, Open = false },
		};

		/// <summary>Tile dimensions for every tile this mission uses.</summary>
		public static readonly Dictionary<string, (int w, int h)> Dimensions =
			new Dictionary<string, (int w, int h)>
			{
				{ "Bespin_2", (8, 4) },
				{ "Bespin_3", (4, 6) },
				{ "Bespin_4", (6, 4) },
				{ "Bespin_6", (3, 3) },
				{ "Bespin_8", (4, 2) },
				{ "Core_20", (4, 6) },
				{ "Core_23", (4, 4) },
				{ "Core_27", (6, 2) },
				{ "Core_29", (4, 2) },
				{ "Core_31", (2, 2) },
				{ "Core_32", (2, 2) },
				{ "Core_33", (2, 2) },
				{ "Core_36", (2, 1) },
			};

		/// <summary>Playable shape per tile face, taken from the art's
		/// alpha channel. '-' is a square outside the tile. In Imperial
		/// Assault the tile's SHAPE is the wall, so these are load-bearing
		/// rather than cosmetic: 22% of tile faces are non-rectangular,
		/// and honouring the shapes removes 84% of apparent tile overlaps.</summary>
		public static readonly TileTerrain[] Shapes = new TileTerrain[]
		{
			new TileTerrain { Expansion = "Bespin", TileId = "2", Side = "A", Width = 8, Height = 4, Rows = new[] { "..X..X..", "..X.....", "........", "-......-" }, Edges = new[] { new TileEdge { C = 3, R = 3, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 4, R = 3, Dir = "N", Type = EdgeType.Impassable } } },
			new TileTerrain { Expansion = "Bespin", TileId = "3", Side = "A", Width = 4, Height = 6, Rows = new[] { "-..-", "-..-", "....", "....", "-..-", "-..-" } },
			new TileTerrain { Expansion = "Bespin", TileId = "4", Side = "A", Width = 6, Height = 4, Rows = new[] { "---..-", "d.....", "d.....", "---..-" } },
			new TileTerrain { Expansion = "Bespin", TileId = "6", Side = "A", Width = 3, Height = 3, Rows = new[] { "..-", ".I.", "..." } },
			new TileTerrain { Expansion = "Bespin", TileId = "8", Side = "A", Width = 4, Height = 2, Rows = new[] { "....", "...." } },
			new TileTerrain { Expansion = "Core", TileId = "20", Side = "A", Width = 4, Height = 6, Rows = new[] { "....", "X...", "....", "X...", "....", "...." } },
			new TileTerrain { Expansion = "Core", TileId = "23", Side = "A", Width = 4, Height = 4, Rows = new[] { "....", "....", ".X..", "...." } },
			new TileTerrain { Expansion = "Core", TileId = "27", Side = "A", Width = 6, Height = 2, Rows = new[] { "......", "......" } },
			new TileTerrain { Expansion = "Core", TileId = "29", Side = "A", Width = 4, Height = 2, Rows = new[] { "....", "...." } },
			new TileTerrain { Expansion = "Core", TileId = "31", Side = "A", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "32", Side = "A", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "33", Side = "A", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "36", Side = "A", Width = 2, Height = 1, Rows = new[] { ".." } },
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
