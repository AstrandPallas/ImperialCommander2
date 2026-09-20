using System.Linq;
using Saga.Tracking;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>The tracking layer: enemy health in the count + current figure model, hero damage and strain, condition tokens, and mission tokens.</summary>
	public static class TrackingTests
	{
		private static GroupCombatState Troopers()
			=> GroupCombatState.Create( "g1", "DG001", "Stormtrooper", figures: 3, perFigureHealth: 3 );

		private static HeroCombatState Diala()
			=> new HeroCombatState { CardId = "H1", Name = "Diala", MaxHealth = 10, Endurance = 4 };

		public static void Register()
		{
			Suite( "tracking: enemy groups" );

			Test( "a new group is at full strength", () =>
			{
				var g = Troopers();
				Eq( 3, g.FiguresAlive, "three figures" );
				Eq( 3, g.EngagedRemaining, "engaged figure at full health" );
				False( g.IsDefeated, "not defeated" );
			} );

			Test( "damage below the threshold wounds but does not kill", () =>
			{
				var g = Troopers();
				Eq( 0, g.ApplyDamage( 2 ), "no figure died" );
				Eq( 3, g.FiguresAlive, "still three" );
				Eq( 1, g.EngagedRemaining, "one health left on the engaged figure" );
			} );

			Test( "reaching the threshold kills one figure and a fresh one steps up", () =>
			{
				var g = Troopers();
				g.AddCondition( Condition.Stunned );
				Eq( 1, g.ApplyDamage( 3 ), "one figure died" );
				Eq( 2, g.FiguresAlive, "two remain" );
				Eq( 3, g.EngagedRemaining, "the next figure is undamaged" );
				False( g.Has( Condition.Stunned ),
					"conditions belonged to the dead figure, not the group" );
			} );

			Test( "overkill does not carry to the next figure", () =>
			{
				// Each figure is damaged separately, so a huge hit kills one
				// figure and the excess is simply lost.
				var g = Troopers();
				Eq( 1, g.ApplyDamage( 99 ), "exactly one figure died" );
				Eq( 2, g.FiguresAlive, "two remain" );
				Eq( 3, g.EngagedRemaining, "the next figure is untouched" );
			} );

			Test( "killing the last figure defeats the group", () =>
			{
				var g = Troopers();
				for ( int i = 0; i < 3; i++ ) g.ApplyDamage( 3 );
				Eq( 0, g.FiguresAlive, "none left" );
				True( g.IsDefeated, "group defeated" );
				Eq( 0, g.ApplyDamage( 5 ), "further damage does nothing" );
			} );

			Test( "players can say which figure is the wounded one", () =>
			{
				var g = Troopers();
				True( g.SetEngaged( 2 ), "figure 2 is alive and can be engaged" );
				Eq( 2, g.EngagedFigureIndex, "engaged index moved" );
				g.ApplyDamage( 3 );
				False( g.SetEngaged( 2 ), "a dead figure cannot be engaged" );
			} );

			Test( "healing the engaged figure works and cannot over-heal", () =>
			{
				var g = Troopers();
				g.ApplyDamage( 2 );
				g.Heal( 5 );
				Eq( 3, g.EngagedRemaining, "back to full, not above it" );
			} );

			Test( "Stunned forbids moving and attacking until discarded", () =>
			{
				// "A Stunned figure cannot voluntarily exit its space and cannot
				// declare an attack... can use the [action] specified on the
				// Stunned condition card to discard the Stunned condition."
				var g = Troopers();
				True( g.CanMove, "normally free to move" );
				True( g.CanDeclareAttack, "normally free to attack" );

				g.AddCondition( Condition.Stunned );
				False( g.CanMove, "Stunned figures cannot voluntarily exit their space" );
				False( g.CanDeclareAttack, "and cannot declare an attack" );
				Eq( 1, g.UsableActions, "one action left after discarding the condition" );

				True( g.DiscardStunned(), "an action discards it" );
				True( g.CanMove, "and the figure is free again" );
				Eq( 2, g.UsableActions, "both actions usable once it is gone" );
			} );

			Test( "Bleeding deals damage after EACH action, not once per activation", () =>
			{
				// "If a figure has Bleeding after it has resolved an action, the
				// figure suffers 1 [damage]." Treating this as once per
				// activation halves the condition's effect.
				var g = Troopers();
				g.AddCondition( Condition.Bleeding );

				Eq( 0, g.ResolveAfterAction(), "first action: 1 damage" );
				Eq( 0, g.ResolveAfterAction(), "second action: another point" );
				Eq( 1, g.EngagedRemaining, "two actions cost two health" );

				Eq( 1, g.ResolveAfterAction(), "the third point kills the figure" );
				Eq( 2, g.FiguresAlive, "one figure down" );
			} );

			Test( "a condition cannot stack with itself", () =>
			{
				// "A figure cannot be affected by multiple instances of the same
				// condition."
				var g = Troopers();
				g.AddCondition( Condition.Bleeding );
				g.AddCondition( Condition.Bleeding );
				Eq( 1, g.Conditions.Count, "still one instance" );
			} );

			Test( "Weakened is discarded at end of activation, Bleeding is not", () =>
			{
				// "Weakened is automatically discarded at the end of a figure's
				// activation."
				var g = Troopers();
				g.AddCondition( Condition.Weakened );
				g.AddCondition( Condition.Bleeding );
				g.EndActivation();
				False( g.Has( Condition.Weakened ), "Weakened expires" );
				True( g.Has( Condition.Bleeding ), "Bleeding persists until discarded" );
				True( g.HasActivated, "marked as activated" );
			} );

			Suite( "tracking: heroes" );

			Test( "the first defeat wounds the hero and discards all damage", () =>
			{
				// "When a hero is defeated for the first time during a mission, he
				// discards all damage tokens from his Hero sheet and flips his
				// Hero sheet to the wounded side."
				var h = Diala();
				h.ApplyDamage( 7 );
				Eq( 3, h.RemainingHealth, "still standing" );
				False( h.IsWounded, "not yet wounded" );

				True( h.ApplyDamage( 3 ), "reaching Health is a defeat" );
				True( h.IsWounded, "flipped to the wounded side" );
				Eq( 0, h.Damage, "damage tokens discarded" );
				Eq( 10, h.RemainingHealth, "wounded side starts fresh" );
				True( h.InPlay, "still in the mission" );
			} );

			Test( "the second defeat withdraws the hero", () =>
			{
				// "If a wounded hero is defeated, he withdraws."
				var h = Diala();
				h.ApplyDamage( 10 );
				h.ApplyDamage( 10 );
				True( h.IsWithdrawn, "withdrawn from the mission" );
				False( h.InPlay, "no longer in play" );
				Eq( 2, h.DefeatCount, "defeated twice" );
			} );

			Test( "strain is capped at Endurance", () =>
			{
				// "A hero can only optionally suffer an amount of strain up to
				// his Endurance."
				var h = Diala();
				True( h.SpendStrain( 4 ), "can spend up to Endurance" );
				False( h.SpendStrain( 1 ), "cannot exceed it" );
				Eq( 4, h.Strain, "strain unchanged by the refused spend" );
			} );

			Test( "resting recovers strain, and excess recovery heals damage", () =>
			{
				// "By resting, a hero can recover strain equal to his Endurance.
				// If a hero recovers strain in excess of the number of strain
				// tokens he has, the hero recovers damage equal to the amount of
				// excess."
				var h = Diala();
				h.SpendStrain( 1 );
				h.ApplyDamage( 5 );

				var (strain, damage) = h.Rest();
				Eq( 1, strain, "cleared the single strain" );
				Eq( 3, damage, "the other 3 of Endurance 4 healed damage" );
				Eq( 0, h.Strain, "no strain left" );
				Eq( 2, h.Damage, "5 damage less 3 recovered" );
			} );

			Test( "resting with no strain converts the whole budget to healing", () =>
			{
				var h = Diala();
				h.ApplyDamage( 6 );
				var (strain, damage) = h.Rest();
				Eq( 0, strain, "no strain to clear" );
				Eq( 4, damage, "all of Endurance went into healing" );
				Eq( 2, h.Damage, "6 less 4" );
			} );

			Test( "resting never heals more damage than the hero has", () =>
			{
				var h = Diala();
				h.ApplyDamage( 1 );
				var (_, damage) = h.Rest();
				Eq( 1, damage, "only the damage actually present" );
				Eq( 0, h.Damage, "fully healed" );
			} );

			Test( "a withdrawn hero stops taking damage", () =>
			{
				var h = Diala();
				h.ApplyDamage( 10 );
				h.ApplyDamage( 10 );
				False( h.ApplyDamage( 5 ), "no further state change" );
			} );

			Suite( "tracking: tokens" );

			Test( "mission tokens carry state and a counter", () =>
			{
				var crate = new MissionTokenState { Name = "Crate1", Kind = TokenKind.Crate };
				False( crate.IsResolved, "starts unopened" );
				crate.State = "claimed";
				crate.ClaimedBy = "Diala";
				True( crate.IsResolved, "claimed counts as resolved" );

				var term = new MissionTokenState { Name = "Terminal A", Kind = TokenKind.Terminal,
					State = "active", Counter = 2 };
				Eq( 2, term.Counter, "terminals can carry a counter" );
			} );

			Suite( "tracking: save and restore" );

			Test( "a full round trip changes nothing", () =>
			{
				var m = new TrackerManager { Round = 4 };
				var g = Troopers();
				g.ApplyDamage( 2 );
				g.AddCondition( Condition.Stunned );
				g.SetEngaged( 1 );
				g.Figures[0].PosC = 101; g.Figures[0].PosR = 97;
				g.HasActivated = true;
				m.Groups.Add( g );

				var h = Diala();
				h.ApplyDamage( 3 );
				h.SpendStrain( 2 );
				h.AddCondition( Condition.Bleeding );
				h.PosC = 99; h.PosR = 100; h.PosRound = 4; h.PosConfidence = "confirmed";
				m.Heroes.Add( h );

				m.Tokens.Add( new MissionTokenState { EntityGuid = "dcf1", Name = "Crate1",
					Kind = TokenKind.Crate, State = "claimed", ClaimedBy = "Diala" } );

				var before = m.Capture();
				var restored = new TrackerManager();
				restored.Restore( before );
				var after = restored.Capture();

				Eq( TrackerManager.Fingerprint( before ), TrackerManager.Fingerprint( after ),
					"state survived the round trip" );
			} );

			Test( "restore rebuilds behaviour, not just fields", () =>
			{
				var m = new TrackerManager();
				var g = Troopers();
				g.ApplyDamage( 3 );        // one figure down
				g.ApplyDamage( 1 );        // next figure at 1 damage
				m.Groups.Add( g );

				var restored = new TrackerManager();
				restored.Restore( m.Capture() );
				var r = restored.Group( "g1" );

				Eq( 2, r.FiguresAlive, "two figures survived the save" );
				Eq( 2, r.EngagedRemaining, "engaged figure still carries its damage" );
				Eq( 1, r.ApplyDamage( 2 ), "and dies on the right hit" );
				Eq( 1, r.FiguresAlive, "leaving one" );
			} );

			Test( "an empty tracker round trips", () =>
			{
				var m = new TrackerManager();
				var restored = new TrackerManager();
				restored.Restore( m.Capture() );
				Eq( TrackerManager.Fingerprint( m.Capture() ),
					TrackerManager.Fingerprint( restored.Capture() ), "empty is stable" );
			} );

			Test( "restoring null clears rather than throwing", () =>
			{
				var m = new TrackerManager();
				m.Heroes.Add( Diala() );
				m.Restore( null );
				Eq( 0, m.Heroes.Count, "cleared" );
			} );

			Test( "end of round clears activation flags and advances the round", () =>
			{
				var m = new TrackerManager { Round = 2 };
				var g = Troopers();
				g.HasActivated = true;
				m.Groups.Add( g );

				m.EndRound();
				False( g.HasActivated, "activation flag cleared for the new round" );
				Eq( 3, m.Round, "round advanced" );
			} );

			Test( "live collections exclude the dead and the withdrawn", () =>
			{
				var m = new TrackerManager();
				var alive = Troopers();
				var dead = GroupCombatState.Create( "g2", "DG004", "Officer", 1, 3 );
				dead.ApplyDamage( 3 );
				m.Groups.Add( alive );
				m.Groups.Add( dead );

				var standing = Diala();
				var gone = Diala();
				gone.CardId = "H2";
				gone.ApplyDamage( 10 );
				gone.ApplyDamage( 10 );
				m.Heroes.Add( standing );
				m.Heroes.Add( gone );

				Eq( 1, m.LiveGroups.Count(), "only the undefeated group" );
				Eq( 1, m.LiveHeroes.Count(), "only the hero still in the mission" );
			} );

		}
	}
}
