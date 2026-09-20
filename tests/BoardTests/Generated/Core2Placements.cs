// GENERATED from ..\..\ImperialCommander2\Assets\Resources\SagaMissions\Core\CORE2.json
// Regenerate with: python tools/gen_mission_fixture.py
// Real mission geometry, so the engine is exercised on a shipped map
// rather than only on hand-written fixtures.
using System.Collections.Generic;

namespace Saga.Board.Tests
{
	public static class Core2Placements
	{
		public static readonly TilePlacement[] Tiles = new TilePlacement[]
		{
			new TilePlacement { Expansion = "Core", TileId = "7", Side = "A", X = 98, Y = 100, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "10", Side = "A", X = 97, Y = 97, Rotation = 90, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "18", Side = "A", X = 97, Y = 99, Rotation = 270, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "38", Side = "B", X = 100, Y = 104, Rotation = 180, SectionGuid = "11111111-1111-1111-1111-111111111111" },
			new TilePlacement { Expansion = "Core", TileId = "22", Side = "B", X = 102, Y = 102, Rotation = 180, SectionGuid = "e219ad68-310f-4f12-af16-01de12f0aaac" },
			new TilePlacement { Expansion = "Core", TileId = "25", Side = "B", X = 101, Y = 101, Rotation = 0, SectionGuid = "e219ad68-310f-4f12-af16-01de12f0aaac" },
			new TilePlacement { Expansion = "Core", TileId = "32", Side = "B", X = 102, Y = 101, Rotation = 270, SectionGuid = "e219ad68-310f-4f12-af16-01de12f0aaac" },
			new TilePlacement { Expansion = "Core", TileId = "35", Side = "B", X = 100, Y = 102, Rotation = 0, SectionGuid = "e219ad68-310f-4f12-af16-01de12f0aaac" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "B", X = 98, Y = 99, Rotation = 90, SectionGuid = "e219ad68-310f-4f12-af16-01de12f0aaac" },
			new TilePlacement { Expansion = "Core", TileId = "36", Side = "B", X = 105, Y = 104, Rotation = 270, SectionGuid = "e219ad68-310f-4f12-af16-01de12f0aaac" },
		};

		public static readonly DoorPlacement[] Doors = new DoorPlacement[]
		{
			new DoorPlacement { X = 101, Y = 102, Rotation = 90, Open = false },
		};

		/// <summary>Tile dimensions for every tile this mission uses.</summary>
		public static readonly Dictionary<string, (int w, int h)> Dimensions =
			new Dictionary<string, (int w, int h)>
			{
				{ "Core_10", (3, 3) },
				{ "Core_18", (2, 1) },
				{ "Core_22", (4, 4) },
				{ "Core_25", (4, 4) },
				{ "Core_32", (2, 2) },
				{ "Core_35", (1, 2) },
				{ "Core_36", (2, 1) },
				{ "Core_38", (2, 2) },
				{ "Core_7", (4, 4) },
			};

		/// <summary>Playable shape per tile face, taken from the art's
		/// alpha channel. '-' is a square outside the tile. In Imperial
		/// Assault the tile's SHAPE is the wall, so these are load-bearing
		/// rather than cosmetic: 22% of tile faces are non-rectangular,
		/// and honouring the shapes removes 84% of apparent tile overlaps.</summary>
		public static readonly TileTerrain[] Shapes = new TileTerrain[]
		{
			new TileTerrain { Expansion = "Core", TileId = "10", Side = "A", Width = 3, Height = 3, Rows = new[] { "...", "...", "..." } },
			new TileTerrain { Expansion = "Core", TileId = "18", Side = "A", Width = 2, Height = 1, Rows = new[] { ".." } },
			new TileTerrain { Expansion = "Core", TileId = "22", Side = "B", Width = 4, Height = 4, Rows = new[] { "....", "....", "....", "...." }, Edges = new[] { new TileEdge { C = 2, R = 1, Dir = "W", Type = EdgeType.Blocking }, new TileEdge { C = 1, R = 2, Dir = "N", Type = EdgeType.Blocking }, new TileEdge { C = 2, R = 2, Dir = "N", Type = EdgeType.Blocking } } },
			new TileTerrain { Expansion = "Core", TileId = "25", Side = "B", Width = 4, Height = 4, Rows = new[] { "-..-", "....", "....", "...." } },
			new TileTerrain { Expansion = "Core", TileId = "32", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
			new TileTerrain { Expansion = "Core", TileId = "35", Side = "B", Width = 1, Height = 2, Rows = new[] { ".", "." } },
			new TileTerrain { Expansion = "Core", TileId = "36", Side = "B", Width = 2, Height = 1, Rows = new[] { ".." } },
			new TileTerrain { Expansion = "Core", TileId = "38", Side = "B", Width = 2, Height = 2, Rows = new[] { "..", ".." } },
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
