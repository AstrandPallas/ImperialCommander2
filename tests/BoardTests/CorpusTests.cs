using System.Collections.Generic;
using System.Linq;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>
	/// Every shipped mission built with the authored walls, checked against
	/// itself built by tile shape alone.
	/// </summary>
	/// <remarks>
	/// The walls were read off the tile art, and a misread edge fails in one
	/// of two directions. A missed wall lets the AI walk through something the
	/// players can see is solid, which they notice. A wall that is not there
	/// severs the map, which the AI does not notice: it just never comes,
	/// because it cannot reach. This sweep catches the second kind. With every
	/// door open, anything the Rebels could reach by shape alone must still be
	/// reachable with the walls in, or a doorway has been read as wall.
	/// </remarks>
	public static class CorpusTests
	{
		private static readonly Sq[] Around =
		{
			new Sq( -1, -1 ), new Sq( 0, -1 ), new Sq( 1, -1 ),
			new Sq( -1, 0 ), new Sq( 1, 0 ),
			new Sq( -1, 1 ), new Sq( 0, 1 ), new Sq( 1, 1 ),
		};

		public static HashSet<Sq> Reach( BoardModel board, IEnumerable<Sq> from )
		{
			var seen = new HashSet<Sq>();
			var queue = new Queue<Sq>();
			foreach ( var s in from )
			{
				if ( board.IsEnterable( s ) && seen.Add( s ) ) queue.Enqueue( s );
			}
			while ( queue.Count > 0 )
			{
				var s = queue.Dequeue();
				foreach ( var d in Around )
				{
					var n = new Sq( s.C + d.C, s.R + d.R );
					if ( !seen.Contains( n ) && board.CanStep( s, n ) )
					{
						seen.Add( n );
						queue.Enqueue( n );
					}
				}
			}
			return seen;
		}

		/// <summary>
		/// Sets of sections that can be on the table together.
		/// </summary>
		/// <remarks>
		/// Sections are revealed at different points of a mission and may reuse
		/// the same squares, so building them all at once invents joins that
		/// never occur in play and lets one section's wall land on another's
		/// doorway. Each set here is one section plus every other that does
		/// not overlap anything already in it, in the mission's own order.
		/// </remarks>
		public static List<HashSet<string>> SectionSets( TilePlacement[] tiles )
		{
			var ids = tiles.Select( t => t.SectionGuid ).Distinct().ToList();
			var squares = ids.ToDictionary( id => id, id =>
				new HashSet<Sq>( BoardBuilder.Build( tiles, Corpus.Lookup, null, null,
					s => s == id ).Board.Squares ) );
			var sets = new List<HashSet<string>>();
			foreach ( var first in ids )
			{
				var set = new HashSet<string> { first };
				var taken = new HashSet<Sq>( squares[first] );
				foreach ( var other in ids )
				{
					if ( set.Contains( other ) || squares[other].Overlaps( taken ) ) continue;
					set.Add( other );
					taken.UnionWith( squares[other] );
				}
				if ( !sets.Any( x => x.SetEquals( set ) ) ) sets.Add( set );
			}
			return sets;
		}

		/// <summary>Squares the map's points of interest lose when the walls go in.</summary>
		public static List<string> Severed( (string Name, string Tiles, string Doors, string Points) m,
			TerrainLibrary withWalls, TerrainLibrary shapeOnly )
		{
			var tiles = Corpus.Tiles( m.Tiles );
			var doors = Corpus.Doors( m.Doors, forceOpen: true );
			var points = Corpus.Points( m.Points );
			var lost = new List<string>();

			foreach ( var set in SectionSets( tiles ) )
			{
				System.Func<string, bool> active = set.Contains;
				var open = BoardBuilder.Build( tiles, Corpus.Lookup, doors, shapeOnly, active ).Board;
				var built = BoardBuilder.Build( tiles, Corpus.Lookup, doors, withWalls, active );
				var walled = built.Board;

				// The Rebels start on the highlights; a map with none is seeded
				// from its deployment points, which is where the Imperials enter.
				var seeds = points.Where( p => p.Kind == 'H' && open.Exists( p.Square ) )
					.Select( p => p.Square ).ToList();
				if ( seeds.Count == 0 )
					seeds = points.Where( p => p.Kind == 'D' && open.Exists( p.Square ) )
						.Select( p => p.Square ).ToList();
				if ( seeds.Count == 0 ) continue;

				var before = Reach( open, seeds );
				var after = Reach( walled, seeds );
				var here = new List<string>();
				foreach ( var p in points )
				{
					if ( before.Contains( p.Square ) && !after.Contains( p.Square ) )
						here.Add( $"{m.Name}: {p.Kind} '{p.Name}' at {p.Square} cut off by walls" );
				}
				if ( here.Count > 0 )
					here.AddRange( Frontier( m.Name, built, after, before ) );
				foreach ( var line in here )
					if ( !lost.Contains( line ) ) lost.Add( line );
			}
			return lost;
		}

		/// <summary>A frontier line whose wall the art did not settle.</summary>
		public static bool Doubtful( string line ) => line.Contains( "UNSURE" );

		/// <summary>
		/// The wall edges standing between what is reachable and what was
		/// lost, named by tile face and local edge so the art can be checked.
		/// </summary>
		private static IEnumerable<string> Frontier( string mission, BoardBuilder.BuildResult built,
			HashSet<Sq> after, HashSet<Sq> before )
		{
			var seen = new HashSet<string>();
			foreach ( var a in after )
			{
				foreach ( var (dc, dr, dir) in new[] { (0, -1, "N"), (1, 0, "E"), (0, 1, "S"), (-1, 0, "W") } )
				{
					var b = new Sq( a.C + dc, a.R + dr );
					if ( after.Contains( b ) || !before.Contains( b ) ) continue;
					if ( built.Board.Edge( a, b ) != EdgeType.Wall ) continue;
					string line = $"{mission}:   wall between {a} and {b}: "
						+ Local( built, a, dir ) + " / " + Local( built, b, Opposite( dir ) );
					if ( seen.Add( line ) ) yield return line;
				}
			}
		}

		private static string Opposite( string d )
			=> d == "N" ? "S" : d == "S" ? "N" : d == "E" ? "W" : "E";

		private static string Local( BoardBuilder.BuildResult built, Sq s, string boardDir )
		{
			if ( !built.TileOf.TryGetValue( s, out var t ) ) return "?";
			// Undo the placement rotation on the direction.
			var order = new[] { "N", "E", "S", "W" };
			int i = System.Array.IndexOf( order, boardDir );
			string local = order[((i - t.Rotation / 90) % 4 + 4) % 4];
			bool unsure = Corpus.IsUnsure( t.Face, t.LocalC, t.LocalR, local );
			return $"{t.Face} ({t.LocalC},{t.LocalR}){local}" + (unsure ? " UNSURE" : "");
		}

		public static void Register()
		{
			Suite( "corpus: walls never sever a mission" );

			Test( "every mission builds with the full terrain library", () =>
			{
				var lib = Corpus.Library();
				int missing = 0;
				foreach ( var m in Corpus.Missions )
				{
					var r = BoardBuilder.Build( Corpus.Tiles( m.Tiles ), Corpus.Lookup,
						Corpus.Doors( m.Doors ), lib );
					missing += r.Warnings.Count( w => w.Contains( "no dimensions" ) );
					True( r.Board.Count > 0, m.Name + " has squares" );
				}
				Eq( 0, missing, "no tile is missing dimensions" );
			} );

			Test( "walls change reachability somewhere (they are actually loaded)", () =>
			{
				var withWalls = Corpus.Library();
				var shapeOnly = Corpus.Library( keepWalls: false );
				var m = Corpus.Missions.First( x => x.Name == "CORE1" );
				var tiles = Corpus.Tiles( m.Tiles );
				var closed = Corpus.Doors( m.Doors, forceOpen: false );
				var seeds = Corpus.Points( m.Points ).Where( p => p.Kind == 'H' ).Select( p => p.Square );
				int before = Reach( BoardBuilder.Build( tiles, Corpus.Lookup, closed, shapeOnly ).Board, seeds ).Count;
				int after = Reach( BoardBuilder.Build( tiles, Corpus.Lookup, closed, withWalls ).Board, seeds ).Count;
				True( after < before, $"closed doors plus walls confine the Rebels on CORE1 ({after} < {before})" );
			} );

			// Some missions place a section as a physically separate map, and
			// some community layouts butt a doorway against a printed wall; a
			// confident wall that severs those is the art, not an error. Only a
			// wall the detector could not settle is held against the data.
			Test( "with every door open, no unsure wall cuts a point of interest off", () =>
			{
				var withWalls = Corpus.Library();
				var shapeOnly = Corpus.Library( keepWalls: false );
				var lost = new List<string>();
				foreach ( var m in Corpus.Missions )
					lost.AddRange( Severed( m, withWalls, shapeOnly ) );
				var doubtful = lost.Where( Doubtful ).ToList();
				var missions = lost.Where( l => l.Contains( "cut off" ) )
					.Select( l => l.Substring( 0, l.IndexOf( ':' ) ) ).Distinct().ToList();
				System.Console.WriteLine( $"        {missions.Count} missions have areas confident walls separate: "
					+ string.Join( " ", missions ) );
				Eq( 0, doubtful.Count, "unsure walls on a severing frontier:\n  "
					+ string.Join( "\n  ", doubtful ) );
			} );
		}
	}
}
