using System.Linq;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>Correcting the app at the table.</summary>
	public static class CorrectionTests
	{
		private static BoardBuilder.BuildResult Build()
			=> BoardBuilder.Build(
				Core1Placements.Tiles, Core1Placements.Lookup,
				Core1Placements.Doors.Select( d => new DoorPlacement
				{ X = d.X, Y = d.Y, Rotation = d.Rotation, Open = true } ).ToArray(),
				Core1Placements.Library() );

		public static void Register()
		{
			Suite( "table corrections" );

			Test( "every board square knows which tile face it came from", () =>
			{
				// Without provenance a correction cannot be generalised, so
				// this is the precondition for everything below.
				var built = Build();
				Eq( built.Board.Count, built.TileOf.Count,
					"every square should carry a tile reference" );

				var sample = built.TileOf.First();
				True( !string.IsNullOrEmpty( sample.Value.Face ),
					"tile reference should name a face" );
				True( sample.Value.LocalC >= 0 && sample.Value.LocalR >= 0,
					"local coordinates should be inside the tile" );
			} );

			Test( "correcting a square changes the board", () =>
			{
				var built = Build();
				var target = built.TileOf.First( kv =>
					(built.Board.Flags( kv.Key ) & SquareFlags.Difficult) == 0
					&& (built.Board.Flags( kv.Key ) & SquareFlags.Blocking) == 0 );

				var corrections = new TerrainCorrections();
				corrections.SetSquare( target.Value.Face, target.Value.LocalC,
					target.Value.LocalR, SquareFlags.Difficult,
					"players say this square is snow", round: 3 );

				int changed = corrections.Apply( built );
				True( changed >= 1, "the correction should have changed at least one square" );
				True( (built.Board.Flags( target.Key ) & SquareFlags.Difficult) != 0,
					"the corrected square should now be difficult" );
				Eq( 2, built.Board.EnterCost( target.Key ),
					"and should cost 2 to enter, so the AI routes around it" );
			} );

			Test( "one correction applies to every copy of that tile face", () =>
			{
				var built = Build();
				var group = built.TileOf
					.GroupBy( kv => kv.Value.Face + ":" + kv.Value.LocalC + "," + kv.Value.LocalR )
					.FirstOrDefault( g => g.Count() > 1 );

				if ( group == null )
				{
					// CORE1 may use every tile once. Assert the mechanism
					// instead of silently passing on a vacuous case.
					var any = built.TileOf.First();
					var c2 = new TerrainCorrections();
					c2.SetSquare( any.Value.Face, any.Value.LocalC, any.Value.LocalR,
						SquareFlags.Blocking, "single-copy fallback" );
					Eq( 1, c2.Apply( built ), "a single copy should still be corrected" );
					return;
				}

				var corrections = new TerrainCorrections();
				var first = group.First();
				corrections.SetSquare( first.Value.Face, first.Value.LocalC,
					first.Value.LocalR, SquareFlags.Blocking, "duplicate tile" );
				corrections.Apply( built );

				foreach ( var kv in group )
					True( (built.Board.Flags( kv.Key ) & SquareFlags.Blocking) != 0,
						$"every copy of the face should be corrected, {kv.Key} was not" );
			} );

			Test( "a correction is reversible and never mutates the authored data", () =>
			{
				var built = Build();
				var target = built.TileOf.First();
				var before = built.Board.Flags( target.Key );

				var corrections = new TerrainCorrections();
				var c = corrections.SetSquare( target.Value.Face, target.Value.LocalC,
					target.Value.LocalR, SquareFlags.Blocking, "mis-tap" );
				corrections.Apply( built );
				True( corrections.Remove( c ), "removing a correction should report success" );
				Eq( 0, corrections.Count, "and should leave none behind" );

				var rebuilt = Build();
				Eq( before, rebuilt.Board.Flags( target.Key ),
					"rebuilding without corrections should restore the authored terrain" );
			} );

			Test( "correcting the same square twice replaces rather than stacks", () =>
			{
				var corrections = new TerrainCorrections();
				corrections.SetSquare( "Core_1A", 2, 2, SquareFlags.Difficult, "first" );
				corrections.SetSquare( "Core_1A", 2, 2, SquareFlags.Blocking, "changed my mind" );
				Eq( 1, corrections.Count, "repeated taps on one square should not pile up" );
				Eq( SquareFlags.Blocking, corrections.All.First().Flags,
					"the latest correction should win" );
			} );

			Test( "a correction naming a tile not on this map changes nothing", () =>
			{
				var built = Build();
				var corrections = new TerrainCorrections();
				corrections.SetSquare( "Hoth_99Z", 0, 0, SquareFlags.Blocking, "not here" );
				Eq( 0, corrections.Apply( built ), "an absent face should change nothing" );
			} );

			Test( "corrections export with their reasons, ready to fold back", () =>
			{
				var corrections = new TerrainCorrections();
				corrections.SetSquare( "Core_19B", 3, 2, SquareFlags.Difficult,
					"the swamp in the middle, not the grating", round: 2 );
				corrections.SetEdge( "Core_22B", 2, 1, EdgeDir.W, EdgeType.Blocking,
					"there is a shelf here" );

				var exported = corrections.Export();
				Eq( 2, exported.Count, "both corrections should export" );
				True( exported.All( c => !string.IsNullOrEmpty( c.Reason ) ),
					"every exported correction should carry its reason" );
				True( corrections.Describe().Contains( "Core_19B" ),
					"the session summary should name the tile" );
			} );
		}
	}
}
