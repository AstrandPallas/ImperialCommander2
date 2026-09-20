using System;
using System.Collections.Generic;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>Runs simulated play as a regression gate.</summary>
	public static class SimTests
	{
		/// <summary>Kept modest so the suite still runs in about a second.</summary>
		private const int Seeds = 12;
		private const int Rounds = 8;

		public static void Register()
		{
			Suite( "simulated play on a real mission" );

			foreach ( var mission in Sim.Missions )
			{
				var m = mission;
				Test( "no illegal order on " + m + " in " + Seeds + " seeded runs of "
					+ Rounds + " rounds", () =>
				{
					var broken = new List<string>();
					for ( uint seed = 1; seed <= Seeds; seed++ )
					{
						int violations = Sim.Run( seed, Rounds, quiet: true, mission: m );
						if ( violations > 0 )
							broken.Add( "seed " + seed + ": " + violations );
					}
					True( broken.Count == 0,
						"illegal orders on " + m + ": " + string.Join( ", ", broken )
						+ " -- rerun with: dotnet run -- --sim <seed> " + Rounds + " " + m );
				} );
			}

			Test( "a run is reproducible from its seed", () =>
			{
				// Without this the harness is useless for debugging: a finding
				// that cannot be replayed cannot be turned into a fixture.
				int a = Sim.Run( 7, Rounds, quiet: true );
				int b = Sim.Run( 7, Rounds, quiet: true );
				Eq( a, b, "same seed gave a different result" );
			} );
		}
	}
}
