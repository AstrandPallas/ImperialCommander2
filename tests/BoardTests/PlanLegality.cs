using System;
using System.Collections.Generic;
using System.Linq;

namespace Saga.Board.Tests
{
	/// <summary>Is an activation plan LEGAL under the rules? This lives apart from any one test because two very different callers need the same answer: the randomised fuzzer, which asserts the list is empty across hundreds of synthetic boards, and the simulated-play harness, which reports violations round by round on real shipped maps.</summary>
	public static class PlanLegality
	{
		/// <summary>Every way `plan` breaks the rules.</summary>
		public static List<string> Violations( BoardModel board, ActivationPlan plan,
			IEnumerable<EnemyFigure> group, IEnumerable<TargetCandidate> rebelsIn,
			string where )
		{
			var problems = new List<string>();
			var rebels = rebelsIn.ToList();

			void Require( bool ok, string msg ) { if ( !ok ) problems.Add( msg ); }
			void Forbid( bool bad, string msg ) { if ( bad ) problems.Add( msg ); }
			void Same( object a, object b, string msg )
			{
				if ( !Equals( a, b ) ) problems.Add( $"{msg} (expected {a}, got {b})" );
			}

			foreach ( var fp in plan.Figures )
			{
				// The figure must exist and the plan must reference it.
				Require( fp.Figure != null, $"{where}: plan has no figure" );
				Require( board.Exists( fp.End ), $"{where}: {fp.Figure.Name} ends off board at {fp.End}" );
				Require( board.IsEnterable( fp.End ) || fp.Figure.Massive,
					$"{where}: {fp.Figure.Name} ends on a space it cannot occupy: {fp.End}" );

				// Every square of a large figure's footprint must be on the board.
				var footprint = Pathfinder.Cells(
					new MoveState( fp.End, Facing.NorthSouth ), fp.Figure.Footprint ).ToList();
				foreach ( var cell in footprint )
					Require( board.Exists( cell ),
						$"{where}: {fp.Figure.Name} footprint leaves the board at {cell}" );

				// Large figures may not move diagonally.
				if ( fp.Figure.Footprint != Footprint.Small1x1 )
				{
					for ( int i = 1; i < fp.Path.Count; i++ )
					{
						var from = fp.Path[i - 1];
						var to = fp.Path[i];
						if ( from == to ) continue;   // rotation
						Forbid( Sq.AreDiagonal( from, to ),
							$"{where}: large figure {fp.Figure.Name} moved diagonally "
							+ $"{from} -> {to}" );
					}
				}

				// A figure may pass through others but never finish on one --
				// unless it is Massive. Consolidated Rules p.41: "A Massive
				// figure can end its movement in spaces that contain blocking
				// terrain and/or other figures", pushing whatever is there
				// clear. The push is resolved at the table, so for planning
				// purposes the square is simply legal.
				//
				// The exception has its own exception: "Massive figures cannot
				// enter spaces containing other Massive figures", so two of
				// them sharing an end square is still a violation.
				foreach ( var other in plan.Figures.Where( x => x != fp ) )
				{
					if ( other.End != fp.End ) continue;
					bool bothMassive = fp.Figure.Massive && other.Figure.Massive;
					bool eitherMassive = fp.Figure.Massive || other.Figure.Massive;
					Require( eitherMassive && !bothMassive,
						bothMassive
							? $"{where}: Massive {fp.Figure.Name} and Massive "
								+ $"{other.Figure.Name} both end on {fp.End}"
							: $"{where}: {fp.Figure.Name} and {other.Figure.Name} "
								+ $"both end on {fp.End}" );
				}
				foreach ( var reb in rebels.Where( x => x.InPlay ) )
					Require( reb.Position != fp.End || fp.Figure.Massive,
						$"{where}: {fp.Figure.Name} ends on a Rebel at {fp.End}" );

				// Path must start where the figure stood, end where it claims,
				// and every step must be a legal move.
				Require( fp.Path.Count >= 1, $"{where}: {fp.Figure.Name} has an empty path" );
				Same( fp.Start, fp.Path.First(), $"{where}: path does not start at the figure" );
				Same( fp.End, fp.Path.Last(), $"{where}: path does not end where the plan says" );
				for ( int i = 1; i < fp.Path.Count; i++ )
				{
					if ( fp.Path[i - 1] == fp.Path[i] ) continue;   // rotation in place
					if ( fp.Figure.Footprint != Footprint.Small1x1 || fp.Figure.Massive ) continue;
					Require( board.CanStep( fp.Path[i - 1], fp.Path[i] ),
						$"{where}: illegal step {fp.Path[i - 1]} -> {fp.Path[i]}" );
				}

				// Movement must fit the action economy.
				int budget = fp.ActionsAvailable * fp.Figure.Speed;
				Require( fp.MovementSpent <= budget,
					$"{where}: {fp.Figure.Name} spent {fp.MovementSpent} of {budget}" );
				Require( fp.MovementSpent >= 0, $"{where}: negative movement" );

				// Stunned figures get one action.
				Same( fp.Figure.Stunned ? 1 : 2, fp.ActionsAvailable,
					$"{where}: wrong action count for {fp.Figure.Name}" );

				// A claimed attack must actually be legal from the end square.
				if ( fp.WillAttack )
				{
					Require( fp.Target != null, $"{where}: attack with no target" );
					Require( fp.Target.InPlay, $"{where}: attacking an out-of-play figure" );

					var recheck = AttackEvaluator.Assess( board, fp.End, fp.Target.Position,
						fp.Figure.AttackKind );
					Require( recheck.CanDeclare,
						$"{where}: {fp.Figure.Name} claims an attack from {fp.End} "
						+ $"that re-checks as illegal: {recheck.Reason}" );

					if ( fp.Figure.AttackKind == AttackKind.Melee )
						Require( board.AreAdjacent( fp.End, fp.Target.Position ),
							$"{where}: melee from {fp.End} is not adjacent to {fp.Target.Position}" );
					else
						Require( LineOfSight.HasLos( board, fp.End, fp.Target.Position ),
							$"{where}: ranged from {fp.End} has no line of sight" );

					Require( fp.Attack.RequiredAccuracy >= 0, $"{where}: negative accuracy" );
				}

				// Every decision is explained.
				Require( fp.Trace.Count > 0, $"{where}: {fp.Figure.Name} has no why-trace" );
			}

			if ( plan.GroupTarget?.Chosen != null )
				Require( plan.GroupTarget.Chosen.InPlay, $"{where}: chose an out-of-play target" );

			return problems;
		}
	}
}
