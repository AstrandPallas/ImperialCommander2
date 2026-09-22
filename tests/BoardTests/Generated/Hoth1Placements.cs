// GENERATED from ..\..\ImperialCommander2\Assets\Resources\SagaMissions\Hoth\HOTH1.json
// Regenerate with: python tools/gen_mission_fixture.py
// Real mission geometry, so the engine is exercised on a shipped map
// rather than only on hand-written fixtures.
using System.Collections.Generic;

namespace Saga.Board.Tests
{
	public static class Hoth1Placements
	{
		public static readonly TilePlacement[] Tiles = new TilePlacement[]
		{
			new TilePlacement { Expansion = "Core", TileId = "19", Side = "B", X = 116, Y = 97, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "35", Side = "B", X = 114, Y = 96, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "B", X = 114, Y = 96, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "2", Side = "A", X = 103, Y = 93, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "6", Side = "A", X = 101, Y = 104, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "8", Side = "B", X = 109, Y = 108, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "9", Side = "B", X = 96, Y = 104, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "10", Side = "A", X = 104, Y = 104, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "15", Side = "A", X = 104, Y = 105, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "17", Side = "A", X = 104, Y = 99, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "17", Side = "B", X = 99, Y = 104, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "19", Side = "A", X = 108, Y = 105, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "20", Side = "A", X = 105, Y = 108, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "20", Side = "A", X = 98, Y = 93, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "20", Side = "B", X = 112, Y = 108, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "20", Side = "B", X = 100, Y = 107, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "21", Side = "B", X = 99, Y = 102, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "22", Side = "A", X = 110, Y = 101, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Hoth", TileId = "23", Side = "B", X = 114, Y = 103, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
		};

		public static readonly DoorPlacement[] Doors = new DoorPlacement[]
		{
			new DoorPlacement { X = 97, Y = 103, Rotation = 0, Open = false },
			new DoorPlacement { X = 105, Y = 105, Rotation = 90, Open = false },
			new DoorPlacement { X = 114, Y = 98, Rotation = 180, Open = false },
		};

		/// <summary>Active highlights, which is where Rebels start.</summary>
		public static readonly (string Name, int C, int R)[] Highlights =
			new (string, int, int)[]
			{
				( "Entrance", 100, 98 ),
			};

		/// <summary>Tile dimensions for every tile this mission uses.</summary>
		public static readonly Dictionary<string, (int w, int h)> Dimensions =
			new Dictionary<string, (int w, int h)>
			{
				{ "Core_19", (6, 6) },
				{ "Core_35", (1, 2) },
				{ "Core_36", (2, 1) },
				{ "Hoth_10", (4, 4) },
				{ "Hoth_15", (2, 2) },
				{ "Hoth_17", (2, 2) },
				{ "Hoth_19", (1, 2) },
				{ "Hoth_2", (6, 6) },
				{ "Hoth_20", (2, 1) },
				{ "Hoth_21", (2, 2) },
				{ "Hoth_22", (2, 2) },
				{ "Hoth_23", (1, 2) },
				{ "Hoth_6", (5, 7) },
				{ "Hoth_8", (4, 5) },
				{ "Hoth_9", (4, 4) },
			};

		/// <summary>Playable shape per tile face, taken from the art's
		/// alpha channel. '-' is a square outside the tile. In Imperial
		/// Assault the tile's SHAPE is the wall, so these are load-bearing
		/// rather than cosmetic: 22% of tile faces are non-rectangular,
		/// and honouring the shapes removes 84% of apparent tile overlaps.</summary>
		public static readonly TileTerrain[] Shapes = new TileTerrain[]
		{
			new TileTerrain { Expansion = "Core", TileId = "19", Side = "B", Width = 6, Height = 6, Rows = new[] { "..dddd", "..dd..", ".ddd..", ".dddd.", "dddddd", "dd..dd" }, Edges = new[] { new TileEdge { C = 0, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 0, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 2, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 3, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 4, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 5, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 5, R = 0, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 1, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 5, R = 1, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 4, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 5, R = 4, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 5, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 5, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 5, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 4, R = 5, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 5, R = 5, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 5, R = 5, Dir = "E", Type = EdgeType.Wall } } },
			new TileTerrain { Expansion = "Core", TileId = "35", Side = "B", Width = 1, Height = 2, Rows = new[] { ".", "." }, Edges = new[] { new TileEdge { C = 0, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 1, Dir = "S", Type = EdgeType.Wall } } },
			new TileTerrain { Expansion = "Core", TileId = "36", Side = "B", Width = 2, Height = 1, Rows = new[] { ".." }, Edges = new[] { new TileEdge { C = 0, R = 0, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 0, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 0, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 0, Dir = "E", Type = EdgeType.Wall } } },
			new TileTerrain { Expansion = "Hoth", TileId = "10", Side = "A", Width = 4, Height = 4, Rows = new[] { "...-", "....", "....", "-..." }, Edges = new[] { new TileEdge { C = 0, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 0, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 2, R = 0, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 3, R = 1, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 2, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 3, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 3, R = 3, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 3, R = 3, Dir = "E", Type = EdgeType.Wall } } },
			new TileTerrain { Expansion = "Hoth", TileId = "15", Side = "A", Width = 2, Height = 2, Rows = new[] { "..", ".." }, Edges = new[] { new TileEdge { C = 0, R = 0, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 0, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 1, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 1, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 1, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 1, Dir = "E", Type = EdgeType.Wall } } },
			new TileTerrain { Expansion = "Hoth", TileId = "17", Side = "A", Width = 2, Height = 2, Rows = new[] { "..", ".." }, Edges = new[] { new TileEdge { C = 0, R = 0, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 1, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 1, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 1, Dir = "S", Type = EdgeType.Wall } } },
			new TileTerrain { Expansion = "Hoth", TileId = "17", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." }, Edges = new[] { new TileEdge { C = 1, R = 0, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 1, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 1, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 1, Dir = "E", Type = EdgeType.Wall } } },
			new TileTerrain { Expansion = "Hoth", TileId = "19", Side = "A", Width = 1, Height = 2, Rows = new[] { ".", "." }, Edges = new[] { new TileEdge { C = 0, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 1, Dir = "S", Type = EdgeType.Wall } } },
			new TileTerrain { Expansion = "Hoth", TileId = "2", Side = "A", Width = 6, Height = 6, Rows = new[] { "-...--", "......", "....dd", "....dd", "....dd", "--..d-" }, Edges = new[] { new TileEdge { C = 4, R = 1, Dir = "W", Type = EdgeType.Impassable }, new TileEdge { C = 1, R = 2, Dir = "W", Type = EdgeType.Impassable }, new TileEdge { C = 1, R = 3, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 2, R = 3, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 3, R = 3, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 1, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 0, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 2, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 3, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 3, R = 0, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 1, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 1, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 5, R = 1, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 2, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 5, R = 2, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 3, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 5, R = 3, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 4, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 5, R = 4, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 5, R = 4, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 2, R = 5, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 2, R = 5, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 3, R = 5, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 4, R = 5, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 4, R = 5, Dir = "E", Type = EdgeType.Wall } } },
			new TileTerrain { Expansion = "Hoth", TileId = "20", Side = "A", Width = 2, Height = 1, Rows = new[] { ".." }, Edges = new[] { new TileEdge { C = 0, R = 0, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 0, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 0, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 0, Dir = "E", Type = EdgeType.Wall } } },
			new TileTerrain { Expansion = "Hoth", TileId = "20", Side = "B", Width = 2, Height = 1, Rows = new[] { ".." }, Edges = new[] { new TileEdge { C = 0, R = 0, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 0, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 0, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 0, Dir = "E", Type = EdgeType.Wall } } },
			new TileTerrain { Expansion = "Hoth", TileId = "21", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." }, Edges = new[] { new TileEdge { C = 0, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 1, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 1, Dir = "S", Type = EdgeType.Wall } } },
			new TileTerrain { Expansion = "Hoth", TileId = "22", Side = "A", Width = 2, Height = 2, Rows = new[] { "..", ".." }, Edges = new[] { new TileEdge { C = 0, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 1, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 1, Dir = "S", Type = EdgeType.Wall } } },
			new TileTerrain { Expansion = "Hoth", TileId = "23", Side = "B", Width = 1, Height = 2, Rows = new[] { ".", "." }, Edges = new[] { new TileEdge { C = 0, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 1, Dir = "S", Type = EdgeType.Wall } } },
			new TileTerrain { Expansion = "Hoth", TileId = "6", Side = "A", Width = 5, Height = 7, Rows = new[] { "dd.dd", "dd.dd", "dd.dd", "..XX.", "ddXX.", "dd.dd", "dd.dd" }, Edges = new[] { new TileEdge { C = 0, R = 0, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 2, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 3, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 4, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 4, R = 0, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 1, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 2, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 3, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 4, R = 3, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 4, R = 4, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 4, R = 5, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 6, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 6, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 6, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 2, R = 6, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 4, R = 6, Dir = "E", Type = EdgeType.Wall } } },
			new TileTerrain { Expansion = "Hoth", TileId = "8", Side = "B", Width = 4, Height = 5, Rows = new[] { "-..-", "-ddd", ".dd.", "....", "...." }, Edges = new[] { new TileEdge { C = 1, R = 0, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 2, R = 0, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 1, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 3, R = 1, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 3, R = 1, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 2, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 2, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 3, R = 2, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 4, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 4, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 2, R = 4, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 3, R = 4, Dir = "S", Type = EdgeType.Wall } } },
			new TileTerrain { Expansion = "Hoth", TileId = "9", Side = "B", Width = 4, Height = 4, Rows = new[] { "...-", "....", "....", "...." }, Edges = new[] { new TileEdge { C = 0, R = 0, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 0, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 2, R = 0, Dir = "E", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 1, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 3, R = 1, Dir = "N", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 2, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 3, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 0, R = 3, Dir = "W", Type = EdgeType.Wall }, new TileEdge { C = 1, R = 3, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 2, R = 3, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 3, R = 3, Dir = "S", Type = EdgeType.Wall }, new TileEdge { C = 3, R = 3, Dir = "E", Type = EdgeType.Wall } } },
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
