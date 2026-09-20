using System;
using System.Collections.Generic;
using System.Linq;
using Saga.Board;

namespace Saga.Tracking
{
	/// <summary>Why a figure or hero could not be handed to the planner.</summary>
	public sealed class TrackingGap
	{
		public string Who;
		public string Reason;
		public override string ToString() => Who + ": " + Reason;
	}

	/// <summary>What the planner and the board view need, taken from tracked state.</summary>
	public sealed class BoardSnapshot
	{
		public List<EnemyFigure> Enemies = new List<EnemyFigure>();
		public List<TargetCandidate> Rebels = new List<TargetCandidate>();
		public List<FigureVisibility> Visibility = new List<FigureVisibility>();

		/// <summary>Figures left out, and why. Never silent.</summary>
		public List<TrackingGap> Gaps = new List<TrackingGap>();

		public bool CanPlan => Enemies.Count > 0 && Rebels.Count > 0;
	}

	/// <summary>Turns tracked state into planner inputs, and plan results back into state.</summary>
	public static class TrackerBridge
	{
		/// <summary>A hero position this many rounds old is treated as stale.</summary>
		public const int StaleAfterRounds = 2;

		/// <summary>
		/// Identifier shared by the tracker, the planner and the board view.
		/// </summary>
		/// <remarks>
		/// A group holds several figures that each need their own token and
		/// their own orders, so the group's own id is not enough on its own.
		/// </remarks>
		public static string FigureId( string instanceId, int index )
			=> instanceId + "#" + index;

		/// <summary>Split a figure id back into its group and index.</summary>
		public static bool TryParseFigureId( string figureId, out string instanceId, out int index )
		{
			instanceId = null;
			index = -1;
			if ( string.IsNullOrEmpty( figureId ) ) return false;
			int at = figureId.LastIndexOf( '#' );
			if ( at <= 0 ) return false;
			instanceId = figureId.Substring( 0, at );
			return int.TryParse( figureId.Substring( at + 1 ), out index );
		}

		/// <summary>
		/// Collect everything the planner needs for one activation.
		/// </summary>
		/// <remarks>
		/// A figure with no recorded position is REPORTED rather than guessed
		/// at. The app is advisory: telling the players where a figure they
		/// have not placed ought to move would be inventing the board state it
		/// is supposed to be reading.
		/// </remarks>
		public static BoardSnapshot Snapshot(
			GroupCombatState activating,
			IEnumerable<HeroCombatState> heroes,
			IEnumerable<GroupCombatState> otherGroups = null,
			int round = 0 )
		{
			var snap = new BoardSnapshot();

			if ( activating == null )
			{
				snap.Gaps.Add( new TrackingGap
				{ Who = "(group)", Reason = "no group is activating" } );
				return snap;
			}

			foreach ( var slot in activating.Figures )
			{
				if ( !slot.Alive ) continue;
				string id = FigureId( activating.InstanceId, slot.Index );
				if ( !slot.HasPosition )
				{
					snap.Gaps.Add( new TrackingGap
					{
						Who = activating.CardName + " #" + (slot.Index + 1),
						Reason = "has no position on the board yet",
					} );
					continue;
				}

				snap.Enemies.Add( new EnemyFigure
				{
					Id = id,
					Name = activating.CardName + " #" + (slot.Index + 1),
					Position = new Sq( slot.PosC.Value, slot.PosR.Value ),
					Stunned = activating.Has( Condition.Stunned ),
				} );
			}

			foreach ( var hero in heroes ?? Enumerable.Empty<HeroCombatState>() )
			{
				if ( !hero.InPlay ) continue;
				if ( hero.PosC == null || hero.PosR == null )
				{
					snap.Gaps.Add( new TrackingGap
					{ Who = hero.Name, Reason = "has no position on the board yet" } );
					continue;
				}

				int age = round - hero.PosRound;
				if ( round > 0 && age >= StaleAfterRounds )
				{
					// Stale positions are still USED -- the app must never
					// refuse to answer -- but the caller is told, so the plan
					// can be shown with a warning rather than false confidence.
					snap.Gaps.Add( new TrackingGap
					{
						Who = hero.Name,
						Reason = "position is " + age + " rounds old",
					} );
				}

				var pos = new Sq( hero.PosC.Value, hero.PosR.Value );
				snap.Rebels.Add( new TargetCandidate
				{
					Id = hero.CardId,
					Name = hero.Name,
					Position = pos,
					MaxHealth = hero.MaxHealth,
					Damage = hero.Damage,
					IsWounded = hero.IsWounded,
					InPlay = hero.InPlay,
				} );
				snap.Visibility.Add( new FigureVisibility { Id = hero.CardId, Position = pos } );
			}

			// Other groups still stand on the board: they block line of sight
			// and have to be pathed around, even though they are not acting.
			foreach ( var group in otherGroups ?? Enumerable.Empty<GroupCombatState>() )
			{
				if ( group == null || group.InstanceId == activating.InstanceId ) continue;
				foreach ( var slot in group.Figures )
				{
					if ( !slot.Alive || !slot.HasPosition ) continue;
					snap.Visibility.Add( new FigureVisibility
					{
						Id = FigureId( group.InstanceId, slot.Index ),
						Position = new Sq( slot.PosC.Value, slot.PosR.Value ),
					} );
				}
			}

			foreach ( var enemy in snap.Enemies )
				snap.Visibility.Add( new FigureVisibility
				{ Id = enemy.Id, Position = enemy.Position } );

			return snap;
		}

		/// <summary>
		/// Write a plan's destinations back into the tracker once the players
		/// have carried the moves out on the table.
		/// </summary>
		public static int Commit( ActivationPlan plan, GroupCombatState group )
		{
			if ( plan == null || group == null ) return 0;
			int moved = 0;
			foreach ( var fp in plan.Figures )
			{
				if ( !TryParseFigureId( fp.Figure?.Id, out var instanceId, out int index ) )
					continue;
				if ( instanceId != group.InstanceId ) continue;

				var slot = group.Figures.FirstOrDefault( f => f.Index == index );
				if ( slot == null ) continue;
				slot.PosC = fp.End.C;
				slot.PosR = fp.End.R;
				moved++;
			}
			return moved;
		}

		/// <summary>Record where the players say a hero now stands.</summary>
		public static void SetHeroPosition( HeroCombatState hero, Sq square, int round )
		{
			if ( hero == null ) return;
			hero.PosC = square.C;
			hero.PosR = square.R;
			hero.PosRound = round;
			hero.PosConfidence = "confirmed";
		}

		/// <summary>Place a figure of a group, for deployment or a correction.</summary>
		public static void SetFigurePosition( GroupCombatState group, int index, Sq square )
		{
			var slot = group?.Figures.FirstOrDefault( f => f.Index == index );
			if ( slot == null ) return;
			slot.PosC = square.C;
			slot.PosR = square.R;
		}
	}
}
