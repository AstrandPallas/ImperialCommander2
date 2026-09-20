using System;
using System.Collections.Generic;
using System.Text;

namespace Saga.Board.Tests
{
	/// <summary>Parses a small ASCII board into a BoardModel.</summary>
	public sealed class Fixture
	{
		public BoardModel Board { get; private set; }
		public Dictionary<char, Sq> Markers { get; } = new Dictionary<char, Sq>();
		public int Width { get; private set; }
		public int Height { get; private set; }

		public Sq Marker( char c )
		{
			if ( !Markers.TryGetValue( c, out var s ) )
				throw new ArgumentException( $"fixture has no marker '{c}'" );
			return s;
		}

		public static Fixture Parse( string ascii )
		{
			var lines = ascii.Replace( "\r\n", "\n" ).Split( '\n' );
			var rows = new List<string>();
			foreach ( var raw in lines )
			{
				if ( raw.Trim().Length == 0 && rows.Count == 0 ) continue; // leading blank
				rows.Add( raw );
			}
			while ( rows.Count > 0 && rows[rows.Count - 1].Trim().Length == 0 )
				rows.RemoveAt( rows.Count - 1 );

			if ( rows.Count < 3 || rows.Count % 2 == 0 )
				throw new ArgumentException( $"fixture must have an odd number of lines >= 3, got {rows.Count}" );

			int h = (rows.Count - 1) / 2;
			int widest = 0;
			foreach ( var r in rows ) widest = Math.Max( widest, r.Length );
			if ( widest % 2 == 0 )
				throw new ArgumentException( $"fixture lines must be an odd width, widest was {widest}" );
			int w = (widest - 1) / 2;

			// Pad short lines so trailing open edges do not need trailing spaces
			// in the literal, which editors strip.
			for ( int i = 0; i < rows.Count; i++ )
				rows[i] = rows[i].PadRight( widest );

			var f = new Fixture { Width = w, Height = h, Board = new BoardModel() };
			var b = f.Board;

			for ( int r = 0; r < h; r++ )
			{
				for ( int c = 0; c < w; c++ )
				{
					char g = rows[2 * r + 1][2 * c + 1];
					b.SetSquare( new Sq( c, r ), SquareFromGlyph( g ) );
					if ( char.IsLetterOrDigit( g ) && g != 'd' && g != 'X' && g != 'I' && g != 'P' )
						f.Markers[g] = new Sq( c, r );
				}
			}

			// North edges: even lines, odd columns.
			for ( int r = 0; r <= h; r++ )
			{
				for ( int c = 0; c < w; c++ )
				{
					var t = EdgeFromGlyph( rows[2 * r][2 * c + 1] );
					if ( t != EdgeType.Open ) b.SetEdge( new Sq( c, r ), EdgeDir.N, t );
				}
			}

			// West edges: odd lines, even columns.
			for ( int r = 0; r < h; r++ )
			{
				for ( int c = 0; c <= w; c++ )
				{
					var t = EdgeFromGlyph( rows[2 * r + 1][2 * c] );
					if ( t != EdgeType.Open ) b.SetEdge( new Sq( c, r ), EdgeDir.W, t );
				}
			}

			return f;
		}

		private static SquareFlags SquareFromGlyph( char g )
		{
			switch ( g )
			{
				case 'd': return SquareFlags.Difficult;
				case 'X': return SquareFlags.Blocking;
				case 'I': return SquareFlags.Impassable;
				case 'P': return SquareFlags.Pit;
				case '-': return SquareFlags.Void;
				default: return SquareFlags.Normal;
			}
		}

		private static EdgeType EdgeFromGlyph( char g )
		{
			switch ( g )
			{
				case '-':
				case '|': return EdgeType.Wall;
				case '=':
				case '#': return EdgeType.Blocking;
				case ':': return EdgeType.Impassable;
				case 'D': return EdgeType.DoorClosed;
				case 'o': return EdgeType.DoorOpen;
				default: return EdgeType.Open;   // ' ' and '+'
			}
		}

		/// <summary>Render the board back out, for failure messages.</summary>
		public string Render()
		{
			var sb = new StringBuilder();
			for ( int r = 0; r <= Height; r++ )
			{
				for ( int c = 0; c <= Width; c++ )
				{
					sb.Append( '+' );
					if ( c < Width )
						sb.Append( GlyphForEdge( Board.Edge( new Sq( c, r ), EdgeDir.N ), true ) );
				}
				sb.Append( '\n' );
				if ( r == Height ) break;
				for ( int c = 0; c <= Width; c++ )
				{
					sb.Append( GlyphForEdge( Board.Edge( new Sq( c, r ), EdgeDir.W ), false ) );
					if ( c < Width ) sb.Append( GlyphForSquare( Board.Flags( new Sq( c, r ) ) ) );
				}
				sb.Append( '\n' );
			}
			return sb.ToString();
		}

		private static char GlyphForEdge( EdgeType t, bool horizontal )
		{
			switch ( t )
			{
				case EdgeType.Wall: return horizontal ? '-' : '|';
				case EdgeType.Blocking: return horizontal ? '=' : '#';
				case EdgeType.Impassable: return ':';
				case EdgeType.DoorClosed: return 'D';
				case EdgeType.DoorOpen: return 'o';
				default: return ' ';
			}
		}

		private static char GlyphForSquare( SquareFlags f )
		{
			if ( (f & SquareFlags.Difficult) != 0 ) return 'd';
			if ( (f & SquareFlags.Blocking) != 0 ) return 'X';
			if ( (f & SquareFlags.Impassable) != 0 ) return 'I';
			if ( (f & SquareFlags.Pit) != 0 ) return 'P';
			if ( (f & SquareFlags.Void) != 0 ) return '-';
			return '.';
		}
	}
}
