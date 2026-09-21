using System;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>
	/// The conversion between what a mission file stores and what an entity
	/// carries once a prefab has placed it.
	/// </summary>
	/// <remarks>
	/// This is where the runtime silently disagreed with everything the rest
	/// of the suite proved. A mission stores entityPosition in MISSION UNITS --
	/// "980,1000", always multiples of 10 -- but every prefab's Init finishes
	/// with
	///
	///     mapEntity.entityPosition = transform.position.ToSagaVector();
	///
	/// replacing it with WORLD coordinates. Code written against the file's
	/// convention then rejects every entity, and no test could see it: the
	/// fixtures read the JSON directly and never run a prefab.
	///
	/// So the prefabs' own arithmetic is reproduced here, exactly as they
	/// write it, and the conversion is required to survive the round trip.
	/// </remarks>
	public static class RuntimeCoordinateTests
	{
		/// <summary>HighlightPrefab: (X/10, 0, -Y/10), with no half-space offset.</summary>
		private static (float x, float z) HighlightWorld( int missionX, int missionY )
			=> (missionX / 10f, -missionY / 10f);

		/// <summary>Crate, terminal, token and deployment point: centred in the space.</summary>
		private static (float x, float z) CentredWorld( int missionX, int missionY )
			=> (missionX / 10f + 0.5f, -missionY / 10f - 0.5f);

		/// <summary>DoorPrefab: shifted diagonally to the lattice point it renders on.</summary>
		private static (float x, float z) DoorWorld( int missionX, int missionY, int rotation )
		{
			var (xmod, ymod) = SagaBoardBridge.DoorOffset( rotation );
			return (missionX / 10f + xmod, -missionY / 10f - ymod);
		}

		public static void Register()
		{
			Suite( "runtime coordinates" );

			Test( "CORE1's entrance survives the trip through its prefab", () =>
			{
				// The exact entity that was lost: CORE1 stores the Entrance
				// highlight at "980,1000", and the party is seated on it. When
				// this conversion was wrong the log read "no entrance
				// highlight, the party must be placed by hand" and not a single
				// hero token appeared.
				var (x, z) = HighlightWorld( 980, 1000 );
				Eq( new Sq( 98, 100 ), SagaBoardBridge.WorldToSquare( x, z ),
					"the entrance is square (98,100)" );
			} );

			Test( "a centred entity lands on the space it is drawn in", () =>
			{
				// CORE1's Crate1 at "990,980" and Terminal Red at "940,980".
				var crate = CentredWorld( 990, 980 );
				Eq( new Sq( 99, 98 ), SagaBoardBridge.WorldToSquare( crate.x, crate.z ),
					"the crate is square (99,98)" );

				var terminal = CentredWorld( 940, 980 );
				Eq( new Sq( 94, 98 ), SagaBoardBridge.WorldToSquare( terminal.x, terminal.z ),
					"the terminal is square (94,98)" );
			} );

			Test( "the half-space offset never rounds an entity into its neighbour", () =>
			{
				// Flooring is what makes a centre land in its own space. Any
				// rounding would put half the crates one square out.
				for ( int mx = 900; mx <= 1100; mx += 10 )
					for ( int my = 900; my <= 1100; my += 10 )
					{
						var (x, z) = CentredWorld( mx, my );
						Eq( new Sq( mx / 10, my / 10 ),
							SagaBoardBridge.WorldToSquare( x, z ),
							$"({mx},{my}) must stay in its own space" );
					}
			} );

			Test( "a door read back off the board is the door that was put on it", () =>
			{
				// The prefab shifts a door to its lattice point and BoardBuilder
				// applies that same shift itself, so reading one back without
				// undoing it lands the door one space out along BOTH axes --
				// which is a hole in one wall and a door across another.
				foreach ( int rotation in new[] { 0, 90, 180, 270 } )
					for ( int mx = 960; mx <= 1040; mx += 10 )
						for ( int my = 960; my <= 1040; my += 10 )
						{
							var (x, z) = DoorWorld( mx, my, rotation );
							var lattice = SagaBoardBridge.WorldToSquare( x, z );
							var back = SagaBoardBridge.DoorLatticeToPlacement(
								lattice.C, lattice.R, rotation );

							Eq( mx / 10, back.x,
								$"rotation {rotation}: column must round-trip" );
							Eq( my / 10, back.y,
								$"rotation {rotation}: row must round-trip" );
						}
			} );

			Test( "the lattice conversion is its own inverse", () =>
			{
				foreach ( int rotation in new[] { 0, 90, 180, 270 } )
				{
					var lattice = SagaBoardBridge.DoorPlacementToLattice( 98, 100, rotation );
					var back = SagaBoardBridge.DoorLatticeToPlacement(
						lattice.c, lattice.r, rotation );
					Eq( 98, back.x, "column, rotation " + rotation );
					Eq( 100, back.y, "row, rotation " + rotation );
				}
			} );

			Test( "the door offsets are the ones the prefab actually uses", () =>
			{
				// Taken from DoorPrefab: 180 gives (-1,-1), 270 gives (1,-1),
				// and everything else keeps the default (1,1).
				Eq( (1, 1), SagaBoardBridge.DoorOffset( 0 ), "rotation 0" );
				Eq( (-1, 1), SagaBoardBridge.DoorOffset( 90 ), "rotation 90" );
				Eq( (-1, -1), SagaBoardBridge.DoorOffset( 180 ), "rotation 180" );
				Eq( (1, -1), SagaBoardBridge.DoorOffset( 270 ), "rotation 270" );
				Eq( SagaBoardBridge.DoorOffset( 0 ), SagaBoardBridge.DoorOffset( 360 ),
					"and rotation wraps" );
			} );

			Test( "a position still in mission units is far outside any board", () =>
			{
				// The guard that turns this class of mistake from a silent
				// wrong answer into a loud one: no real board reaches column
				// 500, so a raw mission value is unmistakable.
				var raw = SagaBoardBridge.WorldToSquare( 980f, -1000f );
				True( raw.C > 500 && raw.R > 500,
					"mission units are obviously not a square: " + raw );
			} );
		}
	}
}
