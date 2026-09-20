using System;
using System.Collections.Generic;
using System.Linq;

namespace Saga.Board
{
	/// <summary>A Rebel figure the Imperial AI may target.</summary>
	public sealed class TargetCandidate
	{
		public string Id;
		public string Name;
		public Sq Position;

		/// <summary>Total Health on the hero sheet.</summary>
		public int MaxHealth;

		/// <summary>Damage tokens currently on the figure.</summary>
		public int Damage;

		/// <summary>Withdrawn or defeated figures are not targets at all.</summary>
		public bool InPlay = true;

		/// <summary>Wounded figures are still targets, but rule 1 prefers healthy ones.</summary>
		public bool IsWounded;

		public string[] Traits = Array.Empty<string>();

		public int RemainingHealth => MaxHealth - Damage;
		public bool IsHealthy => InPlay && !IsWounded;

		public override string ToString() => $"{Name}@{Position}";
	}

	/// <summary>The chosen target plus the reasoning that produced it.</summary>
	public sealed class TargetDecision
	{
		public TargetCandidate Chosen;
		public string Rule;
		public List<string> Trace = new List<string>();

		/// <summary>Candidates the rules could not separate.</summary>
		public List<TargetCandidate> Tied = new List<TargetCandidate>();

		public bool NeedsPlayerDecision => Tied.Count > 1;
	}

	/// <summary>Chooses which Rebel an activating group goes after.</summary>
	public static class TargetSelector
	{
		public static TargetDecision Select(
			BoardModel board,
			Sq attacker,
			IEnumerable<TargetCandidate> candidates,
			string[] preferredTraits = null,
			MoveOptions moveOptions = null,
			int searchLimit = 40 )
		{
			var decision = new TargetDecision();
			var all = candidates.Where( c => c != null && c.InPlay ).ToList();
			if ( all.Count == 0 )
			{
				decision.Rule = "no targets in play";
				return decision;
			}

			// Distances come from one reachability sweep, so every comparison
			// below is consistent with how the figure could actually move.
			var reach = Pathfinder.Compute( board, attacker, searchLimit,
				moveOptions ?? new MoveOptions() );
			var dist = new Dictionary<string, int>();
			foreach ( var c in all )
				dist[c.Id] = PathDistance( board, reach, attacker, c.Position );

			var pool = all;
			if ( preferredTraits != null && preferredTraits.Length > 0 )
			{
				var preferred = all.Where( c => c.Traits != null
					&& c.Traits.Any( t => preferredTraits.Contains( t, StringComparer.OrdinalIgnoreCase ) ) )
					.ToList();
				if ( preferred.Count > 0 )
				{
					pool = preferred;
					decision.Trace.Add( $"preferred traits [{string.Join( ",", preferredTraits )}] "
						+ $"narrowed the pool to {preferred.Count} of {all.Count}" );
				}
				else
				{
					decision.Trace.Add( $"no figure matched preferred traits "
						+ $"[{string.Join( ",", preferredTraits )}], using all {all.Count}" );
				}
			}

			var healthy = pool.Where( c => c.IsHealthy && Reachable( dist, c ) ).ToList();

			// Rule 1: closest healthy.
			if ( healthy.Count > 0 )
			{
				int best = healthy.Min( c => dist[c.Id] );
				var tied = healthy.Where( c => dist[c.Id] == best ).ToList();
				decision.Trace.Add( $"rule 1 closest healthy: distance {best}, "
					+ $"{tied.Count} candidate(s) [{Names( tied )}]" );
				foreach ( var c in healthy.Except( tied ) )
					decision.Trace.Add( $"  {c.Name} rejected, distance {dist[c.Id]}" );
				if ( Settle( decision, tied, "1 closest healthy Rebel" ) ) return decision;

				// Rule 2: least Health remaining.
				int least = tied.Min( c => c.RemainingHealth );
				var tied2 = tied.Where( c => c.RemainingHealth == least ).ToList();
				decision.Trace.Add( $"rule 2 least health remaining: {least}, "
					+ $"{tied2.Count} candidate(s) [{Names( tied2 )}]" );
				if ( Settle( decision, tied2, "2 healthy Rebel with the least Health remaining" ) )
					return decision;

				// Rule 3: most total Health.
				int most = tied2.Max( c => c.MaxHealth );
				var tied3 = tied2.Where( c => c.MaxHealth == most ).ToList();
				decision.Trace.Add( $"rule 3 most total health: {most}, "
					+ $"{tied3.Count} candidate(s) [{Names( tied3 )}]" );
				if ( Settle( decision, tied3, "3 healthy Rebel with the most total Health" ) )
					return decision;

				decision.Tied = tied3;
				decision.Chosen = tied3.First();
				decision.Rule = "players decide";
				decision.Trace.Add( "rules exhausted, players decide" );
				return decision;
			}

			// Rule 4: closest Rebel overall, healthy or not.
			var anyReachable = pool.Where( c => Reachable( dist, c ) ).ToList();
			if ( anyReachable.Count == 0 )
			{
				decision.Trace.Add( "no Rebel is reachable; falling back to straight-line distance" );
				anyReachable = pool;
				foreach ( var c in pool )
					dist[c.Id] = Sq.Chebyshev( attacker, c.Position );
			}

			int nearest = anyReachable.Min( c => dist[c.Id] );
			var tied4 = anyReachable.Where( c => dist[c.Id] == nearest ).ToList();
			decision.Trace.Add( $"rule 4 closest Rebel overall: distance {nearest}, "
				+ $"{tied4.Count} candidate(s) [{Names( tied4 )}]" );
			if ( Settle( decision, tied4, "4 closest Rebel overall" ) ) return decision;

			decision.Tied = tied4;
			decision.Chosen = tied4.First();
			decision.Rule = "players decide";
			decision.Trace.Add( "rules exhausted, players decide" );
			return decision;
		}

		private static bool Settle( TargetDecision d, List<TargetCandidate> tied, string rule )
		{
			if ( tied.Count != 1 ) return false;
			d.Chosen = tied[0];
			d.Rule = rule;
			return true;
		}

		private static bool Reachable( Dictionary<string, int> dist, TargetCandidate c )
			=> dist.TryGetValue( c.Id, out var d ) && d >= 0;

		private static string Names( IEnumerable<TargetCandidate> cs )
			=> string.Join( ", ", cs.Select( c => c.Name ) );

		/// <summary>Distance to a target measured as the cost of reaching a space ADJACENT to it, because a figure never enters the space its target occupies.</summary>
		public static int PathDistance( BoardModel board, Reach reach, Sq from, Sq target )
		{
			if ( from == target ) return 0;
			int best = -1;
			foreach ( var n in board.Neighbours( target ) )
			{
				int c = reach.CostTo( n );
				if ( c < 0 ) continue;
				if ( !board.AreAdjacent( n, target ) ) continue;
				if ( best < 0 || c < best ) best = c;
			}
			// Standing adjacent already costs nothing.
			if ( board.AreAdjacent( from, target ) ) return 0;
			return best;
		}
	}
}
