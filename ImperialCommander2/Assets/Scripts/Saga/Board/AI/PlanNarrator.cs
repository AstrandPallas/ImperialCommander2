using System;
using System.Collections.Generic;
using System.Linq;

namespace Saga.Board
{
	/// <summary>Something on the table a player can point at.</summary>
	public sealed class Landmark
	{
		public string Name;
		public Sq Square;

		public Landmark() { }

		public Landmark( string name, Sq square )
		{
			Name = name;
			Square = square;
		}
	}

	/// <summary>
	/// Says where a figure should end up, in terms of things visible on the
	/// table.
	/// </summary>
	/// <remarks>
	/// The map view is the real output -- you watch the token slide and copy
	/// it. This is the caption, and the degraded mode for when the animation
	/// cannot speak: accessibility, a dispute, a stale position, or missing
	/// terrain.
	///
	/// Two rules decide everything here. It describes the DESTINATION, not the
	/// route, because players move figures better from "end up north-east of
	/// the crate" than from a five-step list, and because a legal-but-wandering
	/// path reads as a bug when written out. And it anchors to a named object
	/// rather than to coordinates, because "square (98,101)" requires the
	/// players to hold a coordinate system the cardboard does not have.
	///
	/// Bare coordinates are the last resort, never the first.
	/// </remarks>
	public static class PlanNarrator
	{
		/// <summary>How far a landmark can be and still be worth naming.</summary>
		/// <remarks>
		/// Beyond about three squares "five north-west of the terminal" is
		/// harder to follow than the tile it stands on, so the tile takes over.
		/// </remarks>
		public const int MaxLandmarkDistance = 3;

		/// <summary>
		/// Describe a square: relative to a landmark, else by tile, else raw.
		/// </summary>
		public static string Where( Sq square,
			IEnumerable<Landmark> landmarks = null,
			IReadOnlyDictionary<Sq, BoardBuilder.TileRef> tileOf = null )
		{
			var near = Nearest( square, landmarks );
			if ( near != null )
			{
				int d = Sq.Chebyshev( square, near.Square );
				if ( d == 0 ) return "on " + Article( near.Name );
				return Count( d ) + " " + Direction( near.Square, square )
					+ " of " + Article( near.Name );
			}

			if ( tileOf != null && tileOf.TryGetValue( square, out var tile ) )
				return "on tile " + tile.Expansion + " " + tile.TileId + tile.Side;

			// Nothing to anchor to. Say the coordinates rather than nothing,
			// and say them as coordinates so nobody mistakes them for spaces.
			return "at square " + square.C + "," + square.R;
		}

		/// <summary>One line telling the players what a figure does.</summary>
		public static string Narrate( FigurePlan plan,
			IEnumerable<Landmark> landmarks = null,
			IReadOnlyDictionary<Sq, BoardBuilder.TileRef> tileOf = null )
		{
			if ( plan == null ) return "";
			var list = landmarks?.ToList();

			var name = string.IsNullOrEmpty( plan.Figure?.Name ) ? "The figure" : plan.Figure.Name;
			var move = plan.Moved
				? $"move {plan.MovementSpent} to {Where( plan.End, list, tileOf )}"
				: $"hold {Where( plan.End, list, tileOf )}";

			if ( !plan.WillAttack )
				return name + ": " + move + ", no attack";

			var target = string.IsNullOrEmpty( plan.Target?.Name ) ? "the target" : plan.Target.Name;
			var accuracy = plan.Attack.RequiredAccuracy > 0
				? $" (needs accuracy {plan.Attack.RequiredAccuracy})"
				: "";
			return name + ": " + move + ", then attack " + target + accuracy;
		}

		/// <summary>Every figure's line, in the order the planner committed them.</summary>
		public static List<string> Narrate( ActivationPlan plan,
			IEnumerable<Landmark> landmarks = null,
			IReadOnlyDictionary<Sq, BoardBuilder.TileRef> tileOf = null )
		{
			var lines = new List<string>();
			if ( plan == null ) return lines;
			var list = landmarks?.ToList();
			foreach ( var fp in plan.Figures )
				lines.Add( Narrate( fp, list, tileOf ) );
			return lines;
		}

		private static Landmark Nearest( Sq square, IEnumerable<Landmark> landmarks )
		{
			Landmark best = null;
			int bestDistance = int.MaxValue;

			foreach ( var l in landmarks ?? Enumerable.Empty<Landmark>() )
			{
				if ( l == null || string.IsNullOrEmpty( l.Name ) ) continue;
				int d = Sq.Chebyshev( square, l.Square );
				if ( d > MaxLandmarkDistance ) continue;

				// Ties go to the lower name so the same board always reads the
				// same way; a description that changes between two identical
				// plans looks like the app changed its mind.
				if ( d < bestDistance
					|| (d == bestDistance && string.CompareOrdinal( l.Name, best?.Name ) < 0) )
				{
					best = l;
					bestDistance = d;
				}
			}
			return best;
		}

		/// <summary>
		/// Compass direction from one square to another.
		/// </summary>
		/// <remarks>
		/// Column grows east and ROW GROWS SOUTH, which follows the mission
		/// data: entity Y grows downward on screen and maps to −Z in the world.
		/// Getting this backwards would send every figure the wrong way while
		/// remaining entirely self-consistent, so it is stated here and pinned
		/// by a test rather than left to be inferred.
		/// </remarks>
		public static string Direction( Sq from, Sq to )
		{
			int dc = to.C - from.C;
			int dr = to.R - from.R;

			string ns = dr < 0 ? "north" : dr > 0 ? "south" : "";
			string ew = dc > 0 ? "east" : dc < 0 ? "west" : "";

			if ( ns.Length > 0 && ew.Length > 0 ) return ns + "-" + ew;
			if ( ns.Length > 0 ) return ns;
			if ( ew.Length > 0 ) return ew;
			return "on";
		}

		private static string Count( int squares )
			=> squares == 1 ? "1 square" : squares + " squares";

		/// <summary>"the crate", but not "the the crate".</summary>
		private static string Article( string name )
		{
			var trimmed = (name ?? "").Trim();
			if ( trimmed.Length == 0 ) return "it";
			return trimmed.StartsWith( "the ", StringComparison.OrdinalIgnoreCase )
				? trimmed : "the " + trimmed;
		}
	}
}
