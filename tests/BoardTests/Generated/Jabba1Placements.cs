// GENERATED from ..\..\ImperialCommander2\Assets\Resources\SagaMissions\Jabba\JABBA1.json
// Regenerate with: python tools/gen_mission_fixture.py
// Real mission geometry, so the engine is exercised on a shipped map
// rather than only on hand-written fixtures.
using System.Collections.Generic;

namespace Saga.Board.Tests
{
	public static class Jabba1Placements
	{
		public static readonly TilePlacement[] Tiles = new TilePlacement[]
		{
			new TilePlacement { Expansion = "Core", TileId = "18", Side = "B", X = 94, Y = 96, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "A", X = 99, Y = 95, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "A", X = 97, Y = 101, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Jabba", TileId = "2", Side = "A", X = 95, Y = 95, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Jabba", TileId = "7", Side = "A", X = 105, Y = 100, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "38", Side = "A", X = 94, Y = 96, Rotation = 0, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "31", Side = "A", X = 107, Y = 100, Rotation = 180, SectionGuid = "8c1869d4-376b-4b06-a4e2-7e1f62cf43de" },
			new TilePlacement { Expansion = "Core", TileId = "28", Side = "A", X = 110, Y = 101, Rotation = 180, SectionGuid = "8c1869d4-376b-4b06-a4e2-7e1f62cf43de" },
			new TilePlacement { Expansion = "Core", TileId = "38", Side = "A", X = 103, Y = 102, Rotation = 270, SectionGuid = "8c1869d4-376b-4b06-a4e2-7e1f62cf43de" },
			new TilePlacement { Expansion = "Core", TileId = "35", Side = "A", X = 107, Y = 100, Rotation = 90, SectionGuid = "8c1869d4-376b-4b06-a4e2-7e1f62cf43de" },
			new TilePlacement { Expansion = "Core", TileId = "32", Side = "A", X = 105, Y = 101, Rotation = 0, SectionGuid = "8c1869d4-376b-4b06-a4e2-7e1f62cf43de" },
			new TilePlacement { Expansion = "Core", TileId = "35", Side = "A", X = 107, Y = 101, Rotation = 0, SectionGuid = "8c1869d4-376b-4b06-a4e2-7e1f62cf43de" },
			new TilePlacement { Expansion = "Core", TileId = "32", Side = "A", X = 108, Y = 103, Rotation = 270, SectionGuid = "8c1869d4-376b-4b06-a4e2-7e1f62cf43de" },
			new TilePlacement { Expansion = "Core", TileId = "5", Side = "B", X = 99, Y = 102, Rotation = 90, SectionGuid = "d5e67129-fa1f-45f9-8c74-a8f5a3d4c67a" },
			new TilePlacement { Expansion = "Core", TileId = "18", Side = "B", X = 95, Y = 106, Rotation = 90, SectionGuid = "d5e67129-fa1f-45f9-8c74-a8f5a3d4c67a" },
			new TilePlacement { Expansion = "Core", TileId = "11", Side = "B", X = 95, Y = 106, Rotation = 0, SectionGuid = "d5e67129-fa1f-45f9-8c74-a8f5a3d4c67a" },
			new TilePlacement { Expansion = "Jabba", TileId = "14", Side = "B", X = 99, Y = 106, Rotation = 0, SectionGuid = "d5e67129-fa1f-45f9-8c74-a8f5a3d4c67a" },
			new TilePlacement { Expansion = "Core", TileId = "7", Side = "B", X = 99, Y = 108, Rotation = 0, SectionGuid = "d5e67129-fa1f-45f9-8c74-a8f5a3d4c67a" },
			new TilePlacement { Expansion = "Core", TileId = "14", Side = "B", X = 103, Y = 110, Rotation = 0, SectionGuid = "d5e67129-fa1f-45f9-8c74-a8f5a3d4c67a" },
			new TilePlacement { Expansion = "Core", TileId = "3", Side = "B", X = 101, Y = 110, Rotation = 270, SectionGuid = "d5e67129-fa1f-45f9-8c74-a8f5a3d4c67a" },
			new TilePlacement { Expansion = "Core", TileId = "18", Side = "B", X = 103, Y = 102, Rotation = 180, SectionGuid = "d5e67129-fa1f-45f9-8c74-a8f5a3d4c67a" },
			new TilePlacement { Expansion = "Core", TileId = "13", Side = "B", X = 103, Y = 104, Rotation = 270, SectionGuid = "d5e67129-fa1f-45f9-8c74-a8f5a3d4c67a" },
			new TilePlacement { Expansion = "Jabba", TileId = "8", Side = "B", X = 103, Y = 106, Rotation = 180, SectionGuid = "d5e67129-fa1f-45f9-8c74-a8f5a3d4c67a" },
		};

		public static readonly DoorPlacement[] Doors = new DoorPlacement[]
		{
			new DoorPlacement { X = 104, Y = 96, Rotation = 90, Open = false },
			new DoorPlacement { X = 105, Y = 102, Rotation = 180, Open = false },
		};

		/// <summary>Tile dimensions for every tile this mission uses.</summary>
		public static readonly Dictionary<string, (int w, int h)> Dimensions =
			new Dictionary<string, (int w, int h)>
			{
				{ "Core_11", (4, 2) },
				{ "Core_13", (2, 2) },
				{ "Core_14", (2, 2) },
				{ "Core_18", (2, 1) },
				{ "Core_28", (3, 3) },
				{ "Core_3", (6, 5) },
				{ "Core_31", (2, 2) },
				{ "Core_32", (2, 2) },
				{ "Core_35", (1, 2) },
				{ "Core_36", (2, 1) },
				{ "Core_38", (2, 2) },
				{ "Core_5", (4, 4) },
				{ "Core_7", (4, 4) },
				{ "Jabba_14", (2, 2) },
				{ "Jabba_2", (6, 6) },
				{ "Jabba_7", (5, 4) },
				{ "Jabba_8", (4, 4) },
			};

		/// <summary>Playable shape per tile face, taken from the art's
		/// alpha channel. '-' is a square outside the tile. In Imperial
		/// Assault the tile's SHAPE is the wall, so these are load-bearing
		/// rather than cosmetic: 22% of tile faces are non-rectangular,
		/// and honouring the shapes removes 84% of apparent tile overlaps.</summary>
		public static readonly TileTerrain[] Shapes = new TileTerrain[]
		{
			new TileTerrain { Expansion = "Core", TileId = "11", Side = "B", Width = 4, Height = 2, Rows = new[] { "....", "...." } },
			new TileTerrain { Expansion = "Core", TileId = "13", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "14", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "18", Side = "B", Width = 2, Height = 1, Rows = new[] { ".." } },
			new TileTerrain { Expansion = "Core", TileId = "28", Side = "A", Width = 3, Height = 3, Rows = new[] { "...", "...", "..." } },
			new TileTerrain { Expansion = "Core", TileId = "3", Side = "B", Width = 6, Height = 5, Rows = new[] { "--..--", "-....-", "..X...", "......", "-....-" } },
			new TileTerrain { Expansion = "Core", TileId = "31", Side = "A", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "32", Side = "A", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "35", Side = "A", Width = 1, Height = 2, Rows = new[] { ".", "." } },
			new TileTerrain { Expansion = "Core", TileId = "36", Side = "A", Width = 2, Height = 1, Rows = new[] { ".." } },
			new TileTerrain { Expansion = "Core", TileId = "38", Side = "A", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "5", Side = "B", Width = 4, Height = 4, Rows = new[] { "....", ".X..", "....", "...." }, Edges = new[] { new TileEdge { C = 0, R = 2, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 3, R = 3, Dir = "N", Type = EdgeType.Impassable } } },
			new TileTerrain { Expansion = "Core", TileId = "7", Side = "B", Width = 4, Height = 4, Rows = new[] { "...-", "....", "....", "-..." } },
			new TileTerrain { Expansion = "Jabba", TileId = "14", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Jabba", TileId = "2", Side = "A", Width = 6, Height = 6, Rows = new[] { "-....-", "-....-", "-....-", "......", "......", "......" }, Edges = new[] { new TileEdge { C = 1, R = 1, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 4, R = 1, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 1, R = 3, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 4, R = 3, Dir = "N", Type = EdgeType.Impassable }, new TileEdge { C = 3, R = 1, Dir = "W", Type = EdgeType.Blocking }, new TileEdge { C = 3, R = 2, Dir = "W", Type = EdgeType.Blocking }, new TileEdge { C = 2, R = 4, Dir = "N", Type = EdgeType.Blocking }, new TileEdge { C = 3, R = 4, Dir = "N", Type = EdgeType.Blocking } } },
			new TileTerrain { Expansion = "Jabba", TileId = "7", Side = "A", Width = 5, Height = 4, Rows = new[] { "..---", "..---", "..X..", "....." } },
			new TileTerrain { Expansion = "Jabba", TileId = "8", Side = "B", Width = 4, Height = 4, Rows = new[] { "-d..", "dd..", "....", "...-" } },
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
