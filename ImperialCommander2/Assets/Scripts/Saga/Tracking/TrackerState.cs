using System;
using System.Collections.Generic;
using System.Linq;
using Saga.Board;

namespace Saga.Tracking
{

	[Serializable]
	public class FigureSlotData
	{
		public int index;
		public bool alive;
		public int posC = int.MinValue;
		public int posR = int.MinValue;
	}

	[Serializable]
	public class GroupStateData
	{
		public string instanceId;
		public string cardId;
		public string cardName;
		public bool isElite;
		public int maxFigures;
		public int perFigureHealth;
		public int figuresAlive;
		public int engagedFigureIndex;
		public int currentFigureDamage;
		public string[] conditions = Array.Empty<string>();
		public FigureSlotData[] figures = Array.Empty<FigureSlotData>();
		public bool hasActivated;

		// The board profile. Without these a reload rebuilds every group on
		// the defaults, which silently puts each figure back to a ranged 1x1
		// at speed 4 -- the exact defect UnitProfile exists to close, and one
		// that would reappear on every save, load and undo.
		public int speed = 4;
		public string attackType = "Ranged";
		public string footprint = "Small1x1";
		public bool massive;
		public bool mobile;
		public bool reach;
	}

	[Serializable]
	public class HeroStateData
	{
		public string cardId;
		public string name;
		public int maxHealth;
		public int endurance;
		public int damage;
		public int strain;
		public bool isWounded;
		public bool isWithdrawn;
		public int defeatCount;
		public string[] conditions = Array.Empty<string>();
		public int posC = int.MinValue;
		public int posR = int.MinValue;
		public int posRound;
		public string posConfidence;

		/// <summary>From the hero sheet; used by the player-side range queries.</summary>
		public int speed = 4;
		public bool isAlly;
	}

	[Serializable]
	public class TokenStateData
	{
		public string entityGuid;
		public string name;
		public string kind;
		public string state;
		public int counter;
		public string claimedBy;
		public int posC = int.MinValue;
		public int posR = int.MinValue;
	}

	[Serializable]
	public class TrackerStateData
	{
		public int schemaVersion = 1;
		public int round;
		public GroupStateData[] groups = Array.Empty<GroupStateData>();
		public HeroStateData[] heroes = Array.Empty<HeroStateData>();
		public TokenStateData[] tokens = Array.Empty<TokenStateData>();
	}

	/// <summary>Owns all tracked state and converts it to and from serialisable records.</summary>
	public sealed class TrackerManager
	{
		public int Round;
		public readonly List<GroupCombatState> Groups = new List<GroupCombatState>();
		public readonly List<HeroCombatState> Heroes = new List<HeroCombatState>();
		public readonly List<MissionTokenState> Tokens = new List<MissionTokenState>();

		public GroupCombatState Group( string instanceId )
			=> Groups.FirstOrDefault( g => g.InstanceId == instanceId );

		public HeroCombatState Hero( string cardId )
			=> Heroes.FirstOrDefault( h => h.CardId == cardId );

		/// <summary>Groups still on the board.</summary>
		public IEnumerable<GroupCombatState> LiveGroups => Groups.Where( g => !g.IsDefeated );

		/// <summary>Heroes still in the mission.</summary>
		public IEnumerable<HeroCombatState> LiveHeroes => Heroes.Where( h => h.InPlay );

		/// <summary>A group finishes its activation: discard Weakened and mark it activated.</summary>
		public void EndActivation( GroupCombatState g ) => g.EndActivation();

		/// <summary>End of round: clear activation flags and advance the round.</summary>
		public void EndRound()
		{
			foreach ( var g in Groups ) g.HasActivated = false;
			Round++;
		}

		// ---- capture ----

		public TrackerStateData Capture()
		{
			return new TrackerStateData
			{
				schemaVersion = 1,
				round = Round,
				groups = Groups.Select( CaptureGroup ).ToArray(),
				heroes = Heroes.Select( CaptureHero ).ToArray(),
				tokens = Tokens.Select( CaptureToken ).ToArray(),
			};
		}

		private static GroupStateData CaptureGroup( GroupCombatState g ) => new GroupStateData
		{
			instanceId = g.InstanceId,
			cardId = g.CardId,
			cardName = g.CardName,
			isElite = g.IsElite,
			maxFigures = g.MaxFigures,
			perFigureHealth = g.PerFigureHealth,
			figuresAlive = g.FiguresAlive,
			engagedFigureIndex = g.EngagedFigureIndex,
			currentFigureDamage = g.CurrentFigureDamage,
			conditions = g.Conditions.Select( c => c.ToString() ).OrderBy( s => s ).ToArray(),
			speed = (g.Profile ?? UnitProfile.Default).Speed,
			attackType = (g.Profile ?? UnitProfile.Default).AttackKind.ToString(),
			footprint = (g.Profile ?? UnitProfile.Default).Footprint.ToString(),
			massive = (g.Profile ?? UnitProfile.Default).Massive,
			mobile = (g.Profile ?? UnitProfile.Default).Mobile,
			reach = (g.Profile ?? UnitProfile.Default).HasReach,
			hasActivated = g.HasActivated,
			figures = g.Figures.Select( f => new FigureSlotData
			{
				index = f.Index,
				alive = f.Alive,
				posC = f.PosC ?? int.MinValue,
				posR = f.PosR ?? int.MinValue,
			} ).ToArray(),
		};

		private static HeroStateData CaptureHero( HeroCombatState h ) => new HeroStateData
		{
			cardId = h.CardId,
			name = h.Name,
			maxHealth = h.MaxHealth,
			endurance = h.Endurance,
			damage = h.Damage,
			strain = h.Strain,
			isWounded = h.IsWounded,
			isWithdrawn = h.IsWithdrawn,
			defeatCount = h.DefeatCount,
			conditions = h.Conditions.Select( c => c.ToString() ).OrderBy( s => s ).ToArray(),
			speed = h.Speed,
			isAlly = h.IsAlly,
			posC = h.PosC ?? int.MinValue,
			posR = h.PosR ?? int.MinValue,
			posRound = h.PosRound,
			posConfidence = h.PosConfidence,
		};

		private static TokenStateData CaptureToken( MissionTokenState t ) => new TokenStateData
		{
			entityGuid = t.EntityGuid,
			name = t.Name,
			kind = t.Kind.ToString(),
			state = t.State,
			counter = t.Counter,
			claimedBy = t.ClaimedBy,
			posC = t.PosC ?? int.MinValue,
			posR = t.PosR ?? int.MinValue,
		};

		// ---- restore ----

		public void Restore( TrackerStateData data )
		{
			Groups.Clear();
			Heroes.Clear();
			Tokens.Clear();
			if ( data == null ) return;

			Round = data.round;

			foreach ( var d in data.groups ?? Array.Empty<GroupStateData>() )
			{
				var g = GroupCombatState.Create( d.instanceId, d.cardId, d.cardName,
					d.maxFigures, d.perFigureHealth, d.isElite );
				g.FiguresAlive = d.figuresAlive;
				g.EngagedFigureIndex = d.engagedFigureIndex;
				g.CurrentFigureDamage = d.currentFigureDamage;
				g.HasActivated = d.hasActivated;
				g.Profile = new UnitProfile
				{
					Speed = d.speed > 0 ? d.speed : 4,
					AttackKind = Enum.TryParse( d.attackType, true, out AttackKind ak )
						? ak : AttackKind.Ranged,
					Footprint = Enum.TryParse( d.footprint, true, out Footprint fp )
						? fp : Footprint.Small1x1,
					Massive = d.massive,
					Mobile = d.mobile,
					HasReach = d.reach,
				};
				g.Conditions.Clear();
				foreach ( var c in d.conditions ?? Array.Empty<string>() )
					if ( Enum.TryParse( c, out Condition parsed ) ) g.Conditions.Add( parsed );

				g.Figures.Clear();
				foreach ( var f in d.figures ?? Array.Empty<FigureSlotData>() )
				{
					g.Figures.Add( new FigureSlot
					{
						Index = f.index,
						Alive = f.alive,
						PosC = f.posC == int.MinValue ? (int?)null : f.posC,
						PosR = f.posR == int.MinValue ? (int?)null : f.posR,
					} );
				}
				Groups.Add( g );
			}

			foreach ( var d in data.heroes ?? Array.Empty<HeroStateData>() )
			{
				var h = new HeroCombatState
				{
					CardId = d.cardId,
					Name = d.name,
					MaxHealth = d.maxHealth,
					Endurance = d.endurance,
					Damage = d.damage,
					Strain = d.strain,
					IsWounded = d.isWounded,
					IsWithdrawn = d.isWithdrawn,
					IsDefeated = d.isWithdrawn,
					DefeatCount = d.defeatCount,
					PosC = d.posC == int.MinValue ? (int?)null : d.posC,
					PosR = d.posR == int.MinValue ? (int?)null : d.posR,
					PosRound = d.posRound,
					PosConfidence = d.posConfidence,
					Speed = d.speed > 0 ? d.speed : 4,
					IsAlly = d.isAlly,
				};
				foreach ( var c in d.conditions ?? Array.Empty<string>() )
					if ( Enum.TryParse( c, out Condition parsed ) ) h.Conditions.Add( parsed );
				Heroes.Add( h );
			}

			foreach ( var d in data.tokens ?? Array.Empty<TokenStateData>() )
			{
				Tokens.Add( new MissionTokenState
				{
					EntityGuid = d.entityGuid,
					Name = d.name,
					Kind = Enum.TryParse( d.kind, out TokenKind k ) ? k : TokenKind.Other,
					State = d.state,
					Counter = d.counter,
					ClaimedBy = d.claimedBy,
					PosC = d.posC == int.MinValue ? (int?)null : d.posC,
					PosR = d.posR == int.MinValue ? (int?)null : d.posR,
				} );
			}
		}

		/// <summary>Stable text form of the whole tracker, used to prove a save/load round trip changed nothing.</summary>
		public static string Fingerprint( TrackerStateData d )
		{
			if ( d == null ) return "<null>";
			var sb = new System.Text.StringBuilder();
			sb.Append( "v" ).Append( d.schemaVersion ).Append( " round=" ).Append( d.round ).Append( '\n' );
			foreach ( var g in (d.groups ?? Array.Empty<GroupStateData>()).OrderBy( x => x.instanceId ) )
			{
				sb.Append( "G " ).Append( g.instanceId ).Append( ' ' ).Append( g.cardId )
				  .Append( " elite=" ).Append( g.isElite )
				  .Append( " alive=" ).Append( g.figuresAlive ).Append( '/' ).Append( g.maxFigures )
				  .Append( " hp=" ).Append( g.perFigureHealth )
				  .Append( " eng=" ).Append( g.engagedFigureIndex )
				  .Append( " dmg=" ).Append( g.currentFigureDamage )
				  .Append( " act=" ).Append( g.hasActivated )
				  .Append( " cond=[" ).Append( string.Join( ",", g.conditions ) ).Append( ']' )
				  // The profile belongs in the fingerprint too: without it a
				  // round trip could drop every figure back to a ranged 1x1
				  // and the test asserting nothing changed would still pass.
				  .Append( " spd=" ).Append( g.speed )
				  .Append( ' ' ).Append( g.attackType ).Append( ' ' ).Append( g.footprint )
				  .Append( g.massive ? " Massive" : "" )
				  .Append( g.mobile ? " Mobile" : "" )
				  .Append( g.reach ? " Reach" : "" );
				foreach ( var f in (g.figures ?? Array.Empty<FigureSlotData>()).OrderBy( x => x.index ) )
					sb.Append( " f" ).Append( f.index ).Append( ':' ).Append( f.alive ? "a" : "d" )
					  .Append( '@' ).Append( f.posC ).Append( ',' ).Append( f.posR );
				sb.Append( '\n' );
			}
			foreach ( var h in (d.heroes ?? Array.Empty<HeroStateData>()).OrderBy( x => x.cardId ) )
			{
				sb.Append( "H " ).Append( h.cardId )
				  .Append( " hp=" ).Append( h.damage ).Append( '/' ).Append( h.maxHealth )
				  .Append( " str=" ).Append( h.strain ).Append( '/' ).Append( h.endurance )
				  .Append( " wounded=" ).Append( h.isWounded )
				  .Append( " withdrawn=" ).Append( h.isWithdrawn )
				  .Append( " defeats=" ).Append( h.defeatCount )
				  .Append( " pos=" ).Append( h.posC ).Append( ',' ).Append( h.posR )
				  .Append( '@' ).Append( h.posRound )
				  .Append( " spd=" ).Append( h.speed )
				  .Append( " cond=[" ).Append( string.Join( ",", h.conditions ) ).Append( "]\n" );
			}
			foreach ( var t in (d.tokens ?? Array.Empty<TokenStateData>()).OrderBy( x => x.entityGuid ?? x.name ) )
			{
				sb.Append( "T " ).Append( t.entityGuid ).Append( ' ' ).Append( t.kind )
				  .Append( ' ' ).Append( t.state ).Append( " n=" ).Append( t.counter )
				  .Append( " by=" ).Append( t.claimedBy ?? "-" ).Append( '\n' );
			}
			return sb.ToString();
		}
	}
}
