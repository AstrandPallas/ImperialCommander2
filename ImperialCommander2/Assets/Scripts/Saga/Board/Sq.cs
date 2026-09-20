using System;

namespace Saga.Board
{
	/// <summary>A board square, addressed by column and row in mission space.</summary>
	public readonly struct Sq : IEquatable<Sq>
	{
		public readonly int C;
		public readonly int R;

		public Sq( int c, int r )
		{
			C = c;
			R = r;
		}

		public Sq North => new Sq( C, R - 1 );
		public Sq South => new Sq( C, R + 1 );
		public Sq West => new Sq( C - 1, R );
		public Sq East => new Sq( C + 1, R );

		public Sq Step( int dc, int dr ) => new Sq( C + dc, R + dr );

		/// <summary>Chebyshev distance.</summary>
		public static int Chebyshev( Sq a, Sq b )
		{
			int dc = Math.Abs( a.C - b.C );
			int dr = Math.Abs( a.R - b.R );
			return dc > dr ? dc : dr;
		}

		public static bool AreOrthogonal( Sq a, Sq b )
		{
			int dc = Math.Abs( a.C - b.C );
			int dr = Math.Abs( a.R - b.R );
			return dc + dr == 1;
		}

		public static bool AreDiagonal( Sq a, Sq b )
		{
			return Math.Abs( a.C - b.C ) == 1 && Math.Abs( a.R - b.R ) == 1;
		}

		public bool Equals( Sq other ) => C == other.C && R == other.R;
		public override bool Equals( object obj ) => obj is Sq o && Equals( o );
		public override int GetHashCode() => unchecked(C * 397) ^ R;
		public override string ToString() => $"({C},{R})";

		public static bool operator ==( Sq a, Sq b ) => a.Equals( b );
		public static bool operator !=( Sq a, Sq b ) => !a.Equals( b );
	}
}
