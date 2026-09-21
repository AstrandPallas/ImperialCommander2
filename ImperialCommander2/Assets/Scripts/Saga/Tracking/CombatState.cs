using System;
using System.Collections.Generic;
using System.Linq;

namespace Saga.Tracking
{
	/// <summary>Condition tokens.</summary>
	public enum Condition
	{
		Stunned = 0,
		Bleeding = 1,
		Focused = 2,
		Weakened = 3,
		Hidden = 4,
	}

	/// <summary>Where one figure of a group stands.</summary>
	public sealed class FigureSlot
	{
		public int Index;
		public bool Alive = true;
		public int? PosC;
		public int? PosR;

		public bool HasPosition => PosC.HasValue && PosR.HasValue;
	}

	/// <summary>An enemy group's combat state, in the count + current figure model.</summary>
	public sealed class GroupCombatState
	{
		public string InstanceId;
		public string CardId;
		public string CardName;
		public bool IsElite;
		public int MaxFigures = 1;
		public int PerFigureHealth = 1;

		/// <summary>
		/// Speed, attack type, footprint and keywords, taken from the deployment
		/// card when the group is deployed.
		/// </summary>
		/// <remarks>
		/// Never null. A group deployed without a card still plans, using the
		/// defaults, because an advisory app that refuses to plan is worse at the
		/// table than one that plans cautiously.
		/// </remarks>
		public UnitProfile Profile = UnitProfile.Default;

		public int FiguresAlive;
		public int EngagedFigureIndex;
		public int CurrentFigureDamage;
		public readonly HashSet<Condition> Conditions = new HashSet<Condition>();
		public readonly List<FigureSlot> Figures = new List<FigureSlot>();
		public bool HasActivated;

		public bool IsDefeated => FiguresAlive <= 0;
		public int EngagedRemaining => Math.Max( 0, PerFigureHealth - CurrentFigureDamage );

		public static GroupCombatState Create( string instanceId, string cardId, string name,
			int figures, int perFigureHealth, bool elite = false )
		{
			var g = new GroupCombatState
			{
				InstanceId = instanceId,
				CardId = cardId,
				CardName = name,
				IsElite = elite,
				MaxFigures = Math.Max( 1, figures ),
				PerFigureHealth = Math.Max( 1, perFigureHealth ),
			};
			g.FiguresAlive = g.MaxFigures;
			for ( int i = 0; i < g.MaxFigures; i++ )
				g.Figures.Add( new FigureSlot { Index = i, Alive = true } );
			g.EngagedFigureIndex = 0;
			return g;
		}

		/// <summary>Apply damage to the engaged figure.</summary>
		public int ApplyDamage( int amount )
		{
			if ( amount <= 0 || IsDefeated ) return 0;
			CurrentFigureDamage += amount;
			if ( CurrentFigureDamage < PerFigureHealth ) return 0;
			KillEngaged();
			return 1;
		}

		/// <summary>Heal the engaged figure.</summary>
		public void Heal( int amount )
		{
			if ( amount <= 0 || IsDefeated ) return;
			CurrentFigureDamage = Math.Max( 0, CurrentFigureDamage - amount );
		}

		/// <summary>Remove the engaged figure from play and re-point at another.</summary>
		public void KillEngaged()
		{
			if ( IsDefeated ) return;
			var slot = Figures.FirstOrDefault( f => f.Index == EngagedFigureIndex && f.Alive )
					   ?? Figures.FirstOrDefault( f => f.Alive );
			if ( slot != null )
			{
				slot.Alive = false;
				slot.PosC = null;
				slot.PosR = null;
			}
			FiguresAlive = Math.Max( 0, FiguresAlive - 1 );

			// A fresh figure steps up: damage and conditions belonged to the one
			// that died, not to the group.
			CurrentFigureDamage = 0;
			Conditions.Clear();

			var next = Figures.FirstOrDefault( f => f.Alive );
			EngagedFigureIndex = next?.Index ?? -1;
		}

		/// <summary>Players may say the other figure is the wounded one.</summary>
		public bool SetEngaged( int figureIndex )
		{
			var slot = Figures.FirstOrDefault( f => f.Index == figureIndex && f.Alive );
			if ( slot == null ) return false;
			EngagedFigureIndex = figureIndex;
			return true;
		}

		/// <summary>"A figure cannot be affected by multiple instances of the same condition", which a set gives for free.</summary>
		public void AddCondition( Condition c ) => Conditions.Add( c );
		public void RemoveCondition( Condition c ) => Conditions.Remove( c );
		public bool Has( Condition c ) => Conditions.Contains( c );

		/// <summary>Rules Reference, STUNNED: "A Stunned figure cannot voluntarily
		/// exit its space and cannot declare an attack."</summary>
		public bool CanMove => !Has( Condition.Stunned );
		public bool CanDeclareAttack => !Has( Condition.Stunned );

		/// <summary>A figure always has two actions.</summary>
		public const int ActionsPerActivation = 2;

		/// <summary>Actions left for moving and attacking, assuming a Stunned figure spends its first action discarding the condition.</summary>
		public int UsableActions => Has( Condition.Stunned ) ? 1 : ActionsPerActivation;

		/// <summary>Spend an action to discard Stunned, per the condition card.</summary>
		public bool DiscardStunned()
		{
			if ( !Has( Condition.Stunned ) ) return false;
			Conditions.Remove( Condition.Stunned );
			return true;
		}

		/// <summary>Rules Reference, BLEEDING: "If a figure has Bleeding after it has resolved an action, the figure suffers 1 [damage]." Note AFTER AN ACTION, not at the end of the activation -- a figure taking two actions while Bleeding suffers two damage.</summary>
		public int ResolveAfterAction()
		{
			if ( !Has( Condition.Bleeding ) || IsDefeated ) return 0;
			return ApplyDamage( 1 );
		}

		/// <summary>Rules Reference, WEAKENED: "Weakened is automatically discarded at the end of a figure's activation." Bleeding and Stunned are not: they persist until discarded by an action or ability.</summary>
		public void EndActivation()
		{
			Conditions.Remove( Condition.Weakened );
			HasActivated = true;
		}
	}

	/// <summary>A hero's combat state.</summary>
	public sealed class HeroCombatState
	{
		public string CardId;
		public string Name;
		public int MaxHealth = 10;
		public int Endurance = 4;

		/// <summary>Spaces per move action, from the hero sheet.</summary>
		/// <remarks>
		/// The app never moves a hero -- the players do -- so this is carried for
		/// the player-side range and reachability queries rather than for the AI.
		/// </remarks>
		public int Speed = 4;
		public int Damage;
		public int Strain;
		public bool IsWounded;
		public bool IsDefeated;
		public bool IsWithdrawn;
		public readonly HashSet<Condition> Conditions = new HashSet<Condition>();
		public int? PosC, PosR;
		public int PosRound;
		public string PosConfidence = "assumed";

		public int RemainingHealth => Math.Max( 0, MaxHealth - Damage );
		public int RemainingStrain => Math.Max( 0, Endurance - Strain );
		public bool InPlay => !IsWithdrawn && !IsDefeated;
		public bool IsHealthy => InPlay && !IsWounded;

		/// <summary>Apply damage.</summary>
		public bool ApplyDamage( int amount )
		{
			if ( amount <= 0 || !InPlay ) return false;
			Damage += amount;
			if ( Damage < MaxHealth ) return false;

			DefeatCount++;
			if ( !IsWounded )
			{
				IsWounded = true;
				Damage = 0;       // damage tokens are discarded on the flip
				return true;
			}
			IsWithdrawn = true;
			IsDefeated = true;
			Damage = MaxHealth;
			return true;
		}

		/// <summary>How many times this hero has been defeated this mission.</summary>
		public int DefeatCount;

		public void Heal( int amount )
		{
			if ( amount <= 0 ) return;
			Damage = Math.Max( 0, Damage - amount );
		}

		/// <summary>Spend strain.</summary>
		public bool SpendStrain( int amount )
		{
			if ( amount <= 0 ) return true;
			if ( Strain + amount > Endurance ) return false;
			Strain += amount;
			return true;
		}

		public void RecoverStrain( int amount )
			=> Strain = Math.Max( 0, Strain - Math.Max( 0, amount ) );

		/// <summary>The Rest action.</summary>
		public (int strainRecovered, int damageRecovered) Rest()
		{
			if ( !InPlay ) return (0, 0);
			int budget = Endurance;
			int strainBack = Math.Min( Strain, budget );
			Strain -= strainBack;
			int excess = budget - strainBack;
			int damageBack = Math.Min( Damage, excess );
			Damage -= damageBack;
			return (strainBack, damageBack);
		}

		public void AddCondition( Condition c ) => Conditions.Add( c );
		public void RemoveCondition( Condition c ) => Conditions.Remove( c );
		public bool Has( Condition c ) => Conditions.Contains( c );
	}

	public enum TokenKind { Crate = 0, Terminal = 1, Objective = 2, Door = 3, Other = 4 }

	/// <summary>A mission token the players interact with.</summary>
	public sealed class MissionTokenState
	{
		public string EntityGuid;
		public string Name;
		public TokenKind Kind = TokenKind.Crate;
		public string State = "unopened";
		public int Counter;
		public string ClaimedBy;
		public int? PosC, PosR;

		public bool IsResolved => State == "opened" || State == "claimed" || State == "done";
	}
}
