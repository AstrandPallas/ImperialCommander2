using System;
using System.Collections.Generic;

namespace Saga.Board
{
	/// <summary>Exact line of sight.</summary>
	public static class LineOfSight
	{
		public static bool HasLos( BoardModel b, Sq from, Sq to, Func<Sq, bool> occupied = null )
		{
			if ( from == to ) return true;
			if ( !b.Exists( from ) || !b.Exists( to ) ) return false;

			if ( Sq.AreOrthogonal( from, to ) )
				return !BoardModel.EdgeBlocksLos( b.Edge( from, to ) );

			if ( Sq.AreDiagonal( from, to ) )
				return DiagonalOpen( b, from, to );

			if ( to.C < from.C || (to.C == from.C && to.R < from.R) )
			{
				var swap = from;
				from = to;
				to = swap;
			}

			// Collect blockers once, restricted to the bounding box of the pair.
			int minC = Math.Min( from.C, to.C ) - 1, maxC = Math.Max( from.C, to.C ) + 1;
			int minR = Math.Min( from.R, to.R ) - 1, maxR = Math.Max( from.R, to.R ) + 1;

			var blockRects = new List<(long, long, long, long)>();
			var blockSegs = new List<(long, long, long, long)>();

			for ( int r = minR; r <= maxR; r++ )
			{
				for ( int c = minC; c <= maxC; c++ )
				{
					var s = new Sq( c, r );
					if ( s != from && s != to )
					{
						bool blocks = b.Exists( s ) && b.SquareBlocksLos( s );
						if ( !blocks && occupied != null && b.Exists( s ) && occupied( s ) )
							blocks = true;   // figures block line of sight
						if ( blocks )
							blockRects.Add( (2L * c, 2L * r, 2L * c + 2, 2L * r + 2) );
					}

					if ( BoardModel.EdgeBlocksLos( b.Edge( s, EdgeDir.N ) ) )
						blockSegs.Add( (2L * c, 2L * r, 2L * c + 2, 2L * r) );
					if ( BoardModel.EdgeBlocksLos( b.Edge( s, EdgeDir.W ) ) )
						blockSegs.Add( (2L * c, 2L * r, 2L * c, 2L * r + 2) );
				}
			}

			var fromCentre = (x: 2L * from.C + 1, y: 2L * from.R + 1);
			var toCentre = (x: 2L * to.C + 1, y: 2L * to.R + 1);

			foreach ( var corner in Corners( from ) )
			{
				if ( b.StrictBarrierCorners
					&& CornerIsBehindBarrier( corner, fromCentre, toCentre, blockSegs ) )
					continue;

				foreach ( var (p, q) in TargetEdges( to ) )
				{
					// Degenerate choice: the two traced lines overlap, which the
					// rule forbids.
					if ( Orient( corner, p, q ) == 0 ) continue;
					if ( RegionClear( corner, p, q, blockRects, blockSegs ) ) return true;
				}
			}
			return false;
		}

		/// <summary>True when the chosen corner lies on a blocking segment that separates the attacker's space from the target's space, which makes that corner an illegal place to trace from.</summary>
		private static bool CornerIsBehindBarrier(
			(long x, long y) corner, (long x, long y) fromCentre, (long x, long y) toCentre,
			List<(long, long, long, long)> segs )
		{
			foreach ( var (x0, y0, x1, y1) in segs )
			{
				var p = (x: x0, y: y0);
				var q = (x: x1, y: y1);

				if ( Orient( p, q, corner ) != 0 ) continue;              // not on this line
				if ( corner.x < Math.Min( x0, x1 ) || corner.x > Math.Max( x0, x1 ) ) continue;
				if ( corner.y < Math.Min( y0, y1 ) || corner.y > Math.Max( y0, y1 ) ) continue;

				long a = Orient( p, q, fromCentre );
				long b = Orient( p, q, toCentre );
				if ( a != 0 && b != 0 && ((a > 0) != (b > 0)) ) return true;
			}
			return false;
		}

		/// <summary>Diagonal neighbours: sight passes the shared corner if at least one of the two orthogonal routes around it is clear.</summary>
		private static bool DiagonalOpen( BoardModel b, Sq a, Sq t )
		{
			var viaRow = new Sq( t.C, a.R );
			var viaCol = new Sq( a.C, t.R );

			bool rowClear = b.Exists( viaRow )
				&& !BoardModel.EdgeBlocksLos( b.Edge( a, viaRow ) )
				&& !BoardModel.EdgeBlocksLos( b.Edge( viaRow, t ) )
				&& !b.SquareBlocksLos( viaRow );
			bool colClear = b.Exists( viaCol )
				&& !BoardModel.EdgeBlocksLos( b.Edge( a, viaCol ) )
				&& !BoardModel.EdgeBlocksLos( b.Edge( viaCol, t ) )
				&& !b.SquareBlocksLos( viaCol );

			return rowClear || colClear;
		}

		// ---- geometry, all integers, coordinates pre-doubled ----

		private static IEnumerable<(long, long)> Corners( Sq s )
		{
			long c = 2L * s.C, r = 2L * s.R;
			yield return (c, r);
			yield return (c + 2, r);
			yield return (c + 2, r + 2);
			yield return (c, r + 2);
		}

		/// <summary>The four sides of the target square, each a pair of adjacent corners.</summary>
		private static IEnumerable<((long, long), (long, long))> TargetEdges( Sq s )
		{
			long c = 2L * s.C, r = 2L * s.R;
			yield return ((c, r), (c + 2, r));
			yield return ((c + 2, r), (c + 2, r + 2));
			yield return ((c + 2, r + 2), (c, r + 2));
			yield return ((c, r + 2), (c, r));
		}

		private static long Orient( (long x, long y) a, (long x, long y) b, (long x, long y) c )
			=> (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

		private static bool RegionClear(
			(long, long) a, (long, long) b, (long, long) c,
			List<(long, long, long, long)> rects,
			List<(long, long, long, long)> segs )
		{
			foreach ( var rect in rects )
				if ( TriangleOverlapsRect( a, b, c, rect ) ) return false;
			foreach ( var seg in segs )
				if ( TriangleOverlapsSegment( a, b, c, seg ) ) return false;
			return true;
		}

		/// <summary>Open-interior overlap via the separating axis theorem.</summary>
		private static bool TriangleOverlapsRect(
			(long x, long y) a, (long x, long y) b, (long x, long y) c,
			(long x0, long y0, long x1, long y1) rect )
		{
			var tri = new[] { a, b, c };
			var box = new[]
			{
				(x: rect.x0, y: rect.y0), (x: rect.x1, y: rect.y0),
				(x: rect.x1, y: rect.y1), (x: rect.x0, y: rect.y1)
			};

			// Axis-aligned axes first; they reject most pairs immediately.
			if ( !Overlap( Project( tri, 1, 0 ), Project( box, 1, 0 ) ) ) return false;
			if ( !Overlap( Project( tri, 0, 1 ), Project( box, 0, 1 ) ) ) return false;

			for ( int i = 0; i < 3; i++ )
			{
				var p = tri[i];
				var q = tri[(i + 1) % 3];
				long ax = -(q.y - p.y), ay = q.x - p.x;   // edge normal
				if ( !Overlap( Project( tri, ax, ay ), Project( box, ax, ay ) ) ) return false;
			}
			return true;
		}

		private static (long min, long max) Project( (long x, long y)[] pts, long ax, long ay )
		{
			long min = long.MaxValue, max = long.MinValue;
			foreach ( var p in pts )
			{
				long v = p.x * ax + p.y * ay;
				if ( v < min ) min = v;
				if ( v > max ) max = v;
			}
			return (min, max);
		}

		private static bool Overlap( (long min, long max) a, (long min, long max) b )
			=> !(a.max <= b.min || b.max <= a.min);

		private static bool TriangleOverlapsSegment(
			(long x, long y) a, (long x, long y) b, (long x, long y) c,
			(long x0, long y0, long x1, long y1) seg )
		{
			var p = (x: seg.x0, y: seg.y0);
			var q = (x: seg.x1, y: seg.y1);
			var mid = (x: (seg.x0 + seg.x1) / 2, y: (seg.y0 + seg.y1) / 2);

			if ( StrictlyInside( a, b, c, p ) ) return true;
			if ( StrictlyInside( a, b, c, q ) ) return true;
			if ( StrictlyInside( a, b, c, mid ) ) return true;

			var tri = new[] { a, b, c };
			for ( int i = 0; i < 3; i++ )
				if ( ProperCross( p, q, tri[i], tri[(i + 1) % 3] ) ) return true;
			return false;
		}

		private static bool StrictlyInside(
			(long x, long y) a, (long x, long y) b, (long x, long y) c, (long x, long y) p )
		{
			long d1 = Orient( a, b, p ), d2 = Orient( b, c, p ), d3 = Orient( c, a, p );
			bool neg = d1 < 0 || d2 < 0 || d3 < 0;
			bool pos = d1 > 0 || d2 > 0 || d3 > 0;
			if ( d1 == 0 || d2 == 0 || d3 == 0 ) return false;   // on the boundary
			return !(neg && pos);
		}

		/// <summary>Proper crossing only: shared endpoints and collinear overlap do not count, so a blocker lying along the traced line glances.</summary>
		private static bool ProperCross(
			(long x, long y) p1, (long x, long y) p2,
			(long x, long y) q1, (long x, long y) q2 )
		{
			long d1 = Orient( q1, q2, p1 );
			long d2 = Orient( q1, q2, p2 );
			long d3 = Orient( p1, p2, q1 );
			long d4 = Orient( p1, p2, q2 );
			return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0))
				&& ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
		}
	}
}
