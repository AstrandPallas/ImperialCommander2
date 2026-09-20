using System;
using System.Collections.Generic;
using System.Linq;

namespace Saga.Board
{
	/// <summary>What kind of thing a correction changes.</summary>
	public enum CorrectionKind
	{
		Square,
		Edge,
	}

	/// <summary>One correction, recorded against the TILE FACE rather than the board.</summary>
	public sealed class Correction
	{
		public CorrectionKind Kind;

		/// <summary>Tile face, such as "Core_19B".</summary>
		public string Face;
		/// <summary>Column in the tile's unrotated grid.</summary>
		public int LocalC;
		/// <summary>Row in the tile's unrotated grid.</summary>
		public int LocalR;

		/// <summary>For Kind == Square.</summary>
		public SquareFlags Flags;

		/// <summary>For Kind == Edge: which side of the local square.</summary>
		public EdgeDir Dir;
		/// <summary>For Kind == Edge.</summary>
		public EdgeType Edge;

		/// <summary>Why the correction was made, in the player's words.</summary>
		public string Reason;

		/// <summary>Round it was made in, for the session log.</summary>
		public int Round;

		public string Key => Kind == CorrectionKind.Square
			? $"sq:{Face}:{LocalC},{LocalR}"
			: $"edge:{Face}:{LocalC},{LocalR},{Dir}";

		public override string ToString()
			=> Kind == CorrectionKind.Square
				? $"{Face} ({LocalC},{LocalR}) -> {Flags}" + ReasonSuffix
				: $"{Face} ({LocalC},{LocalR}) {Dir} -> {Edge}" + ReasonSuffix;

		private string ReasonSuffix
			=> string.IsNullOrEmpty( Reason ) ? "" : "   [" + Reason + "]";
	}

	/// <summary>Player corrections to the terrain, applied on top of the authored data.</summary>
	public sealed class TerrainCorrections
	{
		private readonly Dictionary<string, Correction> _byKey =
			new Dictionary<string, Correction>();

		public int Count => _byKey.Count;
		public IEnumerable<Correction> All => _byKey.Values;

		/// <summary>"This square is actually difficult / blocking / normal." Replaces any earlier correction to the same square, so repeated taps do not pile up.</summary>
		public Correction SetSquare( string face, int localC, int localR,
			SquareFlags flags, string reason, int round = 0 )
		{
			var c = new Correction
			{
				Kind = CorrectionKind.Square,
				Face = face,
				LocalC = localC,
				LocalR = localR,
				Flags = flags,
				Reason = reason,
				Round = round,
			};
			_byKey[c.Key] = c;
			return c;
		}

		/// <summary>"There is a wall / no wall on this side of this square.".</summary>
		public Correction SetEdge( string face, int localC, int localR, EdgeDir dir,
			EdgeType edge, string reason, int round = 0 )
		{
			var c = new Correction
			{
				Kind = CorrectionKind.Edge,
				Face = face,
				LocalC = localC,
				LocalR = localR,
				Dir = dir,
				Edge = edge,
				Reason = reason,
				Round = round,
			};
			_byKey[c.Key] = c;
			return c;
		}

		/// <summary>Lift a correction.</summary>
		public bool Remove( Correction c ) => c != null && _byKey.Remove( c.Key );

		public void Clear() => _byKey.Clear();

		/// <summary>Apply every correction to a freshly built board.</summary>
		public int Apply( BoardBuilder.BuildResult built )
		{
			if ( built == null || built.Board == null ) return 0;
			int changed = 0;

			var byFace = new Dictionary<string, List<KeyValuePair<Sq, BoardBuilder.TileRef>>>();
			foreach ( var kv in built.TileOf )
			{
				if ( !byFace.TryGetValue( kv.Value.Face, out var list ) )
					byFace[kv.Value.Face] = list = new List<KeyValuePair<Sq, BoardBuilder.TileRef>>();
				list.Add( kv );
			}

			foreach ( var c in _byKey.Values )
			{
				if ( !byFace.TryGetValue( c.Face, out var squares ) ) continue;

				foreach ( var kv in squares )
				{
					if ( kv.Value.LocalC != c.LocalC || kv.Value.LocalR != c.LocalR ) continue;

					if ( c.Kind == CorrectionKind.Square )
					{
						built.Board.SetSquare( kv.Key, c.Flags );
						changed++;
					}
					else
					{
						var dir = c.Dir;
						built.Board.SetEdge( kv.Key, dir, c.Edge );
						changed++;
					}
				}
			}
			return changed;
		}

		/// <summary>The corrections as flat records, ready to be written to disk and folded back into the authored terrain.</summary>
		public List<Correction> Export()
			=> _byKey.Values
				.OrderBy( c => c.Face )
				.ThenBy( c => c.LocalR )
				.ThenBy( c => c.LocalC )
				.ToList();

		/// <summary>A human-readable summary for the session log.</summary>
		public string Describe()
		{
			if ( _byKey.Count == 0 ) return "no terrain corrections";
			var lines = new List<string> { _byKey.Count + " terrain correction(s):" };
			foreach ( var c in Export() ) lines.Add( "  " + c );
			return string.Join( "\n", lines );
		}
	}
}
