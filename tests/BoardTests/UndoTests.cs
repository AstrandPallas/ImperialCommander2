using System.Linq;
using Saga.Tracking;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>Taking back a mis-tap at the table, and the state that must survive it.</summary>
	public static class UndoTests
	{
		private static TrackerManager Tracked()
		{
			var t = new TrackerManager();
			var group = GroupCombatState.Create( "g1", "DG009", "Royal Guard", 3, 8 );
			group.Profile = UnitProfile.From( "Melee", "Large2x2", 5, new[] { "Reach", "Massive" } );
			TrackerBridge.SetFigurePosition( group, 0, new Sq( 4, 4 ) );
			t.Groups.Add( group );

			var hero = new HeroCombatState
			{ CardId = "H3", Name = "Gaarkhan", MaxHealth = 14, Endurance = 4, Speed = 4 };
			TrackerBridge.SetHeroPosition( hero, new Sq( 2, 2 ), 1 );
			t.Heroes.Add( hero );
			return t;
		}

		public static void Register()
		{
			Suite( "undo" );

			Test( "a mis-tapped damage pip is taken back", () =>
			{
				var t = Tracked();
				var undo = new UndoStack( t );

				undo.Record( "Royal Guard damage +1" );
				t.Groups[0].ApplyDamage( 1 );
				Eq( 1, t.Groups[0].CurrentFigureDamage, "the damage landed" );

				Eq( "Royal Guard damage +1", undo.Undo(), "and is named when taken back" );
				Eq( 0, t.Groups[0].CurrentFigureDamage, "the damage is gone" );
			} );

			Test( "a figure killed by mistake comes back", () =>
			{
				// The worst mis-tap: the pip that kills a figure also clears
				// its position and its conditions.
				var t = Tracked();
				var undo = new UndoStack( t );

				undo.Record( "Royal Guard figure defeated" );
				t.Groups[0].ApplyDamage( 8 );
				Eq( 2, t.Groups[0].FiguresAlive, "a figure died" );

				undo.Undo();
				Eq( 3, t.Groups[0].FiguresAlive, "and is standing again" );
				True( t.Groups[0].Figures[0].HasPosition, "back on its square" );
				Eq( new Sq( 4, 4 ),
					new Sq( t.Groups[0].Figures[0].PosC.Value, t.Groups[0].Figures[0].PosR.Value ),
					"the same square it was on" );
			} );

			Test( "undo does NOT reset a group to a ranged 1x1", () =>
			{
				// Restore rebuilds every group from the saved data, so the
				// board profile has to be part of that data. Otherwise every
				// undo quietly re-breaks the planner.
				var t = Tracked();
				var undo = new UndoStack( t );

				undo.Record( "damage" );
				t.Groups[0].ApplyDamage( 1 );
				undo.Undo();

				var p = t.Groups[0].Profile;
				Eq( AttackKind.Melee, p.AttackKind, "still melee" );
				Eq( Footprint.Large2x2, p.Footprint, "still large" );
				Eq( 5, p.Speed, "still its own speed" );
				True( p.HasReach, "still has Reach" );
				True( p.Massive, "still Massive" );
			} );

			Test( "a hero's sheet survives an undo too", () =>
			{
				var t = Tracked();
				var undo = new UndoStack( t );

				undo.Record( "strain" );
				t.Heroes[0].Strain = 3;
				undo.Undo();

				Eq( 14, t.Heroes[0].MaxHealth, "Gaarkhan's printed health" );
				Eq( 4, t.Heroes[0].Speed, "and his speed" );
				Eq( 0, t.Heroes[0].Strain, "with the strain taken back" );
			} );

			Test( "undo can itself be undone", () =>
			{
				var t = Tracked();
				var undo = new UndoStack( t );

				undo.Record( "damage" );
				t.Groups[0].ApplyDamage( 2 );
				undo.Undo();
				Eq( 0, t.Groups[0].CurrentFigureDamage, "taken back" );

				Eq( "damage", undo.Redo(), "and put back again" );
				Eq( 2, t.Groups[0].CurrentFigureDamage, "with the damage restored" );
			} );

			Test( "a new edit ends the redo branch", () =>
			{
				var t = Tracked();
				var undo = new UndoStack( t );

				undo.Record( "first" );
				t.Groups[0].ApplyDamage( 1 );
				undo.Undo();
				True( undo.CanRedo, "the undone edit is available" );

				undo.Record( "second" );
				t.Groups[0].Conditions.Add( Condition.Stunned );
				False( undo.CanRedo, "until something else is done" );
			} );

			Test( "steps are taken back one at a time, newest first", () =>
			{
				var t = Tracked();
				var undo = new UndoStack( t );

				undo.Record( "first" );
				t.Groups[0].ApplyDamage( 1 );
				undo.Record( "second" );
				t.Groups[0].ApplyDamage( 1 );
				Eq( 2, t.Groups[0].CurrentFigureDamage, "two pips on" );

				Eq( "second", undo.NextUndo, "the label names the newest" );
				undo.Undo();
				Eq( 1, t.Groups[0].CurrentFigureDamage, "one comes off" );
				undo.Undo();
				Eq( 0, t.Groups[0].CurrentFigureDamage, "then the other" );
				False( undo.CanUndo, "and there is nothing left" );
			} );

			Test( "a record that changes nothing is not kept", () =>
			{
				// Otherwise the button appears to work and the board does not
				// move, which reads as the app being broken.
				var t = Tracked();
				var undo = new UndoStack( t );

				undo.Record( "damage" );
				t.Groups[0].ApplyDamage( 1 );
				undo.Record( "a tap that did nothing" );
				undo.Record( "and another" );

				// Undo must skip straight past the empty steps to the last one
				// that actually moved something.
				Eq( "damage", undo.Undo(), "the real change is what comes back" );
				Eq( 0, t.Groups[0].CurrentFigureDamage, "and it is genuinely undone" );
				False( undo.CanUndo, "with nothing meaningful left behind it" );
			} );

			Test( "the stack is bounded", () =>
			{
				var t = Tracked();
				var undo = new UndoStack( t );
				for ( int i = 0; i < UndoStack.Depth + 25; i++ )
				{
					undo.Record( "step " + i );
					t.Groups[0].Conditions.Clear();
					if ( i % 2 == 0 ) t.Groups[0].Conditions.Add( Condition.Stunned );
				}
				Eq( UndoStack.Depth, undo.Count, "it does not grow without limit" );
			} );

			Test( "undoing with nothing recorded is harmless", () =>
			{
				var t = Tracked();
				var undo = new UndoStack( t );
				Eq( null, undo.Undo(), "nothing to take back" );
				Eq( null, undo.Redo(), "nor to put back" );
				Eq( 3, t.Groups[0].FiguresAlive, "and the board is untouched" );
			} );

			Suite( "tracker round trip" );

			Test( "a save and load keeps the board profile", () =>
			{
				// The same defect as the undo case, reached the other way:
				// this is what happens across a session boundary.
				var t = Tracked();
				var saved = t.Capture();

				var reloaded = new TrackerManager();
				reloaded.Restore( saved );

				Eq( TrackerManager.Fingerprint( saved ),
					TrackerManager.Fingerprint( reloaded.Capture() ),
					"the round trip must change nothing" );

				var p = reloaded.Groups[0].Profile;
				Eq( AttackKind.Melee, p.AttackKind, "melee survives the save" );
				Eq( Footprint.Large2x2, p.Footprint, "so does the footprint" );
				Eq( 5, p.Speed, "and the speed" );
				True( p.HasReach && p.Massive, "and the keywords" );
				Eq( 4, reloaded.Heroes[0].Speed, "and the hero's speed" );
			} );

			Test( "a session saved before tracking existed still loads", () =>
			{
				// stateManagementVersion 2 has no trackerstate.json, so the
				// restore is handed nothing. Refusing to load would strand
				// anybody mid-campaign; an empty board that the players
				// correct by dragging is the right degradation.
				var t = Tracked();
				Eq( 1, t.Groups.Count, "something is tracked to begin with" );

				t.Restore( null );
				Eq( 0, t.Groups.Count, "the board comes back empty" );
				Eq( 0, t.Heroes.Count, "with nobody on it" );
				Eq( 0, t.Tokens.Count, "and no objectives" );
			} );

			Test( "an empty capture round-trips to an empty tracker", () =>
			{
				var empty = new TrackerManager();
				var reloaded = new TrackerManager();
				reloaded.Restore( empty.Capture() );
				Eq( TrackerManager.Fingerprint( empty.Capture() ),
					TrackerManager.Fingerprint( reloaded.Capture() ),
					"nothing in, nothing out" );
			} );

			Test( "the fingerprint actually notices a dropped profile", () =>
			{
				// If it did not, the round-trip test above would pass while
				// silently putting every figure back to a ranged 1x1.
				var t = Tracked();
				var a = t.Capture();
				var b = t.Capture();
				b.groups[0].speed = 4;
				b.groups[0].attackType = "Ranged";
				b.groups[0].footprint = "Small1x1";
				b.groups[0].massive = false;
				b.groups[0].reach = false;

				True( TrackerManager.Fingerprint( a ) != TrackerManager.Fingerprint( b ),
					"a flattened profile must show up as a different fingerprint" );
			} );
		}
	}
}
