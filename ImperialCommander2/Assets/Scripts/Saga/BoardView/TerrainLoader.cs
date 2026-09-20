using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Saga.Board;
using UnityEngine;

namespace Saga
{
	/// <summary>Reads the authored terrain from Resources into a TerrainLibrary.</summary>
	public static class TerrainLoader
	{
		public const string ResourceFolder = "TerrainData";

		private static TerrainLibrary _cached;

		private sealed class TerrainFile
		{
			public int schemaVersion;
			public string expansion;
			public List<TerrainTile> tiles;
		}

		private sealed class TerrainTile
		{
			public string tileId;
			public string side;
			public int width;
			public int height;
			public string[] squares;
			public List<TerrainEdge> edges;
		}

		private sealed class TerrainEdge
		{
			public int[] sq;
			public string dir;
			public string type;
		}

		/// <summary>Every expansion's terrain, loaded once.</summary>
		public static TerrainLibrary Library => _cached ?? (_cached = Load());

		/// <summary>Drop the cache so edited terrain is picked up.</summary>
		public static void Reload() => _cached = null;

		public static TerrainLibrary Load()
		{
			var library = new TerrainLibrary();
			var assets = Resources.LoadAll<TextAsset>( ResourceFolder );
			if ( assets == null || assets.Length == 0 )
			{
				Utils.LogWarning( "TerrainLoader::no terrain found in Resources/"
					+ ResourceFolder + ", so every tile will read as open floor" );
				return library;
			}

			foreach ( var asset in assets )
			{
				TerrainFile file;
				try
				{
					file = JsonConvert.DeserializeObject<TerrainFile>( asset.text );
				}
				catch ( System.Exception e )
				{
					Utils.LogWarning( "TerrainLoader::" + asset.name + " did not parse: "
						+ e.Message );
					continue;
				}

				if ( file?.tiles == null ) continue;

				foreach ( var tile in file.tiles )
				{
					if ( tile.squares == null ) continue;
					library.Add( new TileTerrain
					{
						Expansion = file.expansion,
						TileId = tile.tileId,
						Side = tile.side,
						Width = tile.width,
						Height = tile.height,
						Rows = tile.squares,
						Edges = ToEdges( tile.edges ),
					} );
				}
			}
			return library;
		}

		private static TileEdge[] ToEdges( List<TerrainEdge> edges )
		{
			if ( edges == null || edges.Count == 0 ) return null;
			return edges
				.Where( e => e?.sq != null && e.sq.Length == 2 )
				.Select( e => new TileEdge
				{
					C = e.sq[0],
					R = e.sq[1],
					Dir = e.dir,
					Type = ParseEdge( e.type ),
				} )
				.ToArray();
		}

		/// <summary>
		/// An unrecognised edge type becomes a WALL rather than being skipped.
		/// </summary>
		/// <remarks>
		/// The two failure directions are not equal. Dropping an edge lets the
		/// AI walk through something the players can see is solid, and produces
		/// a confident illegal order. Treating it as a wall is at worst too
		/// cautious, and shows up as the AI going the long way round, which
		/// somebody will notice and report.
		/// </remarks>
		private static EdgeType ParseEdge( string type )
		{
			switch ( (type ?? "").ToLowerInvariant() )
			{
				case "open": return EdgeType.Open;
				case "blocking": return EdgeType.Blocking;
				case "impassable": return EdgeType.Impassable;
				case "doorway": return EdgeType.Open;
				default: return EdgeType.Wall;
			}
		}
	}
}
