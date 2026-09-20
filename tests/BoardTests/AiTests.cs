using System.Collections.Generic;
using System.Linq;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>Target selection: the documented priority chain, and the behaviours that distinguish a position-aware AI from the upstream random pick.</summary>
	public static class AiTests
	{
		private static TargetCandidate Hero( string name, Sq at, int max = 10, int dmg = 0,
			bool wounded = false, bool inPlay = true, params string[] traits )
			=> new TargetCandidate
			{
				Id = name,
				Name = name,
				Position = at,
				MaxHealth = max,
				Damage = dmg,
				IsWounded = wounded,
				InPlay = inPlay,
				Traits = traits ?? new string[0],
			};

		public static void Register()
		{
			Suite( "target selection" );

			Test( "rule 1 picks the closest healthy Rebel", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+-+
|A . B . C|
+-+-+-+-+-+" );
				var near = Hero( "Near", f.Marker( 'B' ) );
				var far = Hero( "Far", f.Marker( 'C' ) );
				var d = TargetSelector.Select( f.Board, f.Marker( 'A' ), new[] { far, near } );
				Eq( "Near", d.Chosen.Name, "closest healthy" );
				True( d.Rule.StartsWith( "1" ), "resolved by rule 1, got: " + d.Rule );
				False( d.NeedsPlayerDecision, "no tie" );
			} );

			Test( "closest means PATH distance, not straight line", () =>
			{
				// This is the behaviour that separates a position-aware AI from
				// a distance-table one. "Walled" is ONE space from the attacker
				// in a straight line but sealed behind a wall, reachable only by
				// going the long way round. "Open" is four spaces away in a
				// straight line but much closer to actually reach.
				//
				// A Chebyshev-distance AI charges the wall. A path-distance AI
				// goes after the figure it can actually get to.
				var f = Fixture.Parse( @"
+-+-+-+-+-+
|A|B . . C|
+ +-+-+-+ +
|. . . . .|
+-+-+-+-+-+" );
				var walled = Hero( "Walled", f.Marker( 'B' ) );
				var open = Hero( "Open", f.Marker( 'C' ) );

				Eq( 1, Sq.Chebyshev( f.Marker( 'A' ), f.Marker( 'B' ) ), "Walled is 1 away in a straight line" );
				Eq( 4, Sq.Chebyshev( f.Marker( 'A' ), f.Marker( 'C' ) ), "Open is 4 away in a straight line" );

				var reach = Pathfinder.Compute( f.Board, f.Marker( 'A' ), 40 );
				int dWalled = TargetSelector.PathDistance( f.Board, reach, f.Marker( 'A' ), f.Marker( 'B' ) );
				int dOpen = TargetSelector.PathDistance( f.Board, reach, f.Marker( 'A' ), f.Marker( 'C' ) );
				True( dWalled > dOpen,
					$"by path, Walled ({dWalled}) must be further than Open ({dOpen})" );

				var d = TargetSelector.Select( f.Board, f.Marker( 'A' ), new[] { walled, open } );
				Eq( "Open", d.Chosen.Name, "the AI goes after the reachable figure" );
			} );

			Test( "rule 2 breaks a distance tie on least Health remaining", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|B . C|
+ + + +
|. A .|
+-+-+-+" );
				var hurt = Hero( "Hurt", f.Marker( 'B' ), max: 10, dmg: 7 );   // 3 remaining
				var fresh = Hero( "Fresh", f.Marker( 'C' ), max: 10, dmg: 1 ); // 9 remaining
				var d = TargetSelector.Select( f.Board, f.Marker( 'A' ), new[] { fresh, hurt } );
				Eq( "Hurt", d.Chosen.Name, "the wounded-down figure is finished off" );
				True( d.Rule.StartsWith( "2" ), "resolved by rule 2, got: " + d.Rule );
			} );

			Test( "rule 3 breaks a remaining-health tie on most total Health", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|B . C|
+ + + +
|. A .|
+-+-+-+" );
				var small = Hero( "Small", f.Marker( 'B' ), max: 8, dmg: 3 );   // 5 remaining
				var big = Hero( "Big", f.Marker( 'C' ), max: 12, dmg: 7 );      // 5 remaining
				var d = TargetSelector.Select( f.Board, f.Marker( 'A' ), new[] { small, big } );
				Eq( "Big", d.Chosen.Name, "tie on remaining, prefer the bigger pool" );
				True( d.Rule.StartsWith( "3" ), "resolved by rule 3, got: " + d.Rule );
			} );

			Test( "identical figures are handed to the players, not picked silently", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|B . C|
+ + + +
|. A .|
+-+-+-+" );
				var one = Hero( "One", f.Marker( 'B' ), max: 10, dmg: 2 );
				var two = Hero( "Two", f.Marker( 'C' ), max: 10, dmg: 2 );
				var d = TargetSelector.Select( f.Board, f.Marker( 'A' ), new[] { one, two } );
				True( d.NeedsPlayerDecision, "unresolvable tie must ask the players" );
				Eq( 2, d.Tied.Count, "both remain tied" );
				Eq( "players decide", d.Rule, "rule" );
			} );

			Test( "rule 4 targets a wounded Rebel when no healthy one remains", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A . B|
+-+-+-+" );
				var wounded = Hero( "Wounded", f.Marker( 'B' ), wounded: true );
				var d = TargetSelector.Select( f.Board, f.Marker( 'A' ), new[] { wounded } );
				Eq( "Wounded", d.Chosen.Name, "falls through to rule 4" );
				True( d.Rule.StartsWith( "4" ), "resolved by rule 4, got: " + d.Rule );
			} );

			Test( "healthy Rebels outrank closer wounded ones", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+
|A B . C|
+-+-+-+-+" );
				var closeWounded = Hero( "CloseWounded", f.Marker( 'B' ), wounded: true );
				var farHealthy = Hero( "FarHealthy", f.Marker( 'C' ) );
				var d = TargetSelector.Select( f.Board, f.Marker( 'A' ),
					new[] { closeWounded, farHealthy } );
				Eq( "FarHealthy", d.Chosen.Name, "rule 1 only considers healthy figures" );
			} );

			Test( "withdrawn figures are not targets at all", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A B C|
+-+-+-+" );
				var gone = Hero( "Gone", f.Marker( 'B' ), inPlay: false );
				var here = Hero( "Here", f.Marker( 'C' ) );
				var d = TargetSelector.Select( f.Board, f.Marker( 'A' ), new[] { gone, here } );
				Eq( "Here", d.Chosen.Name, "out-of-play figures are skipped" );
			} );

			Test( "preferred traits filter before the priority chain", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+-+
|A B . C|
+-+-+-+-+" );
				var close = Hero( "Close", f.Marker( 'B' ) );
				var wookiee = Hero( "Wookiee", f.Marker( 'C' ), 10, 0, false, true, "Wookiee" );

				var plain = TargetSelector.Select( f.Board, f.Marker( 'A' ), new[] { close, wookiee } );
				Eq( "Close", plain.Chosen.Name, "without preference, closest wins" );

				var hunting = TargetSelector.Select( f.Board, f.Marker( 'A' ),
					new[] { close, wookiee }, preferredTraits: new[] { "Wookiee" } );
				Eq( "Wookiee", hunting.Chosen.Name, "preference overrides proximity" );
			} );

			Test( "an unmatched trait preference falls back to the whole pool", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A B .|
+-+-+-+" );
				var only = Hero( "Only", f.Marker( 'B' ) );
				var d = TargetSelector.Select( f.Board, f.Marker( 'A' ), new[] { only },
					preferredTraits: new[] { "Wookiee" } );
				Eq( "Only", d.Chosen.Name, "does not fail to pick when nothing matches" );
			} );

			Test( "every decision carries a why-trace", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A B C|
+-+-+-+" );
				var d = TargetSelector.Select( f.Board, f.Marker( 'A' ),
					new[] { Hero( "B", f.Marker( 'B' ) ), Hero( "C", f.Marker( 'C' ) ) } );
				True( d.Trace.Count > 0, "trace is populated" );
				True( d.Trace.Any( t => t.Contains( "rule 1" ) ), "names the rule applied" );
			} );
		}
	}
}
