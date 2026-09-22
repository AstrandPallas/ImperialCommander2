using System.Collections.Generic;
using System.Linq;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>The engine running on a real shipped mission rather than a hand-written fixture.</summary>
	public static class MissionTests
	{
		private static BoardBuilder.BuildResult BuildCore1( bool doorsOpen = false,
			bool withShapes = false )
		{
			var doors = Core1Placements.Doors
				.Select( d => new DoorPlacement { X = d.X, Y = d.Y, Rotation = d.Rotation,
					Open = doorsOpen || d.Open } )
				.ToArray();
			return BoardBuilder.Build( Core1Placements.Tiles, Core1Placements.Lookup, doors,
				withShapes ? Core1Placements.Library() : null );
		}

		public static void Register()
		{
			Suite( "real mission geometry (CORE1)" );

			Test( "builds the same board the corpus harness measured", () =>
			{
				var r = BuildCore1();
				var missing = r.Warnings.Where( w => w.Contains( "no dimensions" ) ).ToList();
				Eq( 0, missing.Count, "no tile is missing dimensions: "
					+ string.Join( "; ", missing ) );
				Eq( 203, r.Board.Count, "square count matches the independent harness" );
				Eq( 3, r.Overlaps.Count, "overlap count matches the independent harness" );

				Eq( 3, r.Warnings.Count( w => w.Contains( "claim this square" )
					|| w.Contains( "within one section" ) ), "each overlap is explained" );
			} );

			Test( "every door lands on a corner shared by four occupied squares", () =>
			{
				var r = BuildCore1();
				foreach ( var d in Core1Placements.Doors )
				{
					var (cx, cy) = BoardBuilder.DoorLattice( d.X, d.Y, d.Rotation );
					var quad = new[]
					{
						new Sq( cx - 1, cy - 1 ), new Sq( cx, cy - 1 ),
						new Sq( cx - 1, cy ), new Sq( cx, cy ),
					};
					foreach ( var q in quad )
						True( r.Board.Exists( q ),
							$"door at ({d.X},{d.Y}) rot {d.Rotation}: {q} should be on the board" );
				}
			} );

			Test( "a door covers two edges between real squares", () =>
			{
				var r = BuildCore1();
				foreach ( var d in Core1Placements.Doors )
				{
					var edges = BoardBuilder.DoorEdges( d.X, d.Y, d.Rotation ).ToList();
					Eq( 2, edges.Count, "a door spans two spaces" );
					foreach ( var (owner, dir) in edges )
					{
						var other = dir == EdgeDir.N ? owner.North : owner.West;
						True( r.Board.Exists( owner ) && r.Board.Exists( other ),
							$"edge {owner}/{dir} should separate two real squares" );
					}
				}
			} );

			Test( "WITHOUT terrain data the map is one open field and doors isolate nothing", () =>
			{
				var closed = BoardBuilder.Build( Core1Placements.Tiles, Core1Placements.Lookup,
					Core1Placements.Doors.Select( d => new DoorPlacement
					{ X = d.X, Y = d.Y, Rotation = d.Rotation, Open = false } ).ToArray() );
				var open = BuildCore1( doorsOpen: true );

				Eq( Flood( open.Board ), Flood( closed.Board ),
					"no walls are authored, so doors cannot isolate anything yet" );
				Eq( closed.Board.Count, Flood( closed.Board ),
					"and the whole map is reachable from any square" );
			} );

			Test( "WITH the authored walls, CORE1's closed doors confine the Rebels", () =>
			{
				var closed = BuildCore1( doorsOpen: false, withShapes: true );
				var open = BuildCore1( doorsOpen: true, withShapes: true );
				var start = Core1Placements.Highlights[0];
				var from = new Sq( start.C, start.R );

				int shut = Pathfinder.Compute( closed.Board, from, 500 ).Cost.Count;
				int opened = Pathfinder.Compute( open.Board, from, 500 ).Cost.Count;
				True( shut < opened, $"closing the doors shrinks the Rebels' reach ({shut} < {opened})" );
				True( opened - shut >= 3 * Core1Placements.Doors.Length,
					$"each door hides at least a few squares ({opened - shut} behind {Core1Placements.Doors.Length} doors)" );
			} );

			Test( "a door does sever the map once its surrounding walls exist", () =>
			{
				var f = Fixture.Parse( @"
+-+-+-+
|A .D.|
+-+-+-+" );
				var a = f.Marker( 'A' );
				var farSide = new Sq( 2, 0 );

				True( Pathfinder.Compute( f.Board, a, 20 ).CanReach( new Sq( 1, 0 ) ),
					"the near side of the door is reachable" );
				False( Pathfinder.Compute( f.Board, a, 20 ).CanReach( farSide ),
					"the far side is not, because the door is shut" );

				f.Board.SetEdge( farSide, EdgeDir.W, EdgeType.DoorOpen );
				True( Pathfinder.Compute( f.Board, a, 20 ).CanReach( farSide ),
					"opening it lets the figure through" );
			} );

			Test( "the AI plans an activation on the real map", () =>
			{
				var r = BuildCore1( doorsOpen: true );
				var squares = r.Board.Squares.OrderBy( s => s.C ).ThenBy( s => s.R ).ToList();
				var enemyAt = squares.First();
				var heroAt = squares.Last();

				var plan = ActivationPlanner.Plan( r.Board,
					new[] { new EnemyFigure { Id = "E", Name = "Trooper", Position = enemyAt,
						Speed = 4, AttackKind = AttackKind.Ranged } },
					new[] { new TargetCandidate { Id = "H", Name = "Hero", Position = heroAt,
						MaxHealth = 10 } } );

				Eq( 1, plan.Figures.Count, "one figure planned" );
				var fp = plan.Figures[0];
				True( plan.GroupTarget.Chosen != null, "a target was chosen" );
				True( fp.Trace.Count > 0, "the decision is explained" );
				True( fp.MovementSpent <= 8, "never spends more than two actions of movement" );
				True( r.Board.Exists( fp.End ), "ends on a real square" );

				// Whatever it chose, the path it returns must be legal.
				for ( int i = 1; i < fp.Path.Count; i++ )
					True( r.Board.CanStep( fp.Path[i - 1], fp.Path[i] ),
						$"illegal step {fp.Path[i - 1]} -> {fp.Path[i]}" );
			} );

			Test( "line of sight stays symmetric on real geometry", () =>
			{
				var r = BuildCore1( doorsOpen: true );
				var squares = r.Board.Squares.OrderBy( s => s.C ).ThenBy( s => s.R ).ToList();
				int checks = 0;
				for ( int i = 0; i < squares.Count; i += 7 )
				{
					for ( int j = 0; j < squares.Count; j += 11 )
					{
						if ( i == j ) continue;
						var a = squares[i];
						var b = squares[j];
						Eq( LineOfSight.HasLos( r.Board, a, b ),
							LineOfSight.HasLos( r.Board, b, a ),
							$"symmetry between {a} and {b}" );
						checks++;
					}
				}
				True( checks > 200, $"sampled enough pairs ({checks})" );
			} );

			Test( "tile shapes remove the apparent overlaps", () =>
			{
				var bbox = BuildCore1();
				var shaped = BuildCore1( withShapes: true );

				Eq( 3, bbox.Overlaps.Count, "bounding boxes appear to collide" );
				Eq( 0, shaped.Overlaps.Count, "the real shapes do not" );
				True( shaped.Board.Count < bbox.Board.Count,
					"and the board has fewer phantom squares "
					+ $"({bbox.Board.Count} -> {shaped.Board.Count})" );
			} );

			Test( "void squares never join the board", () =>
			{
				var shaped = BuildCore1( withShapes: true, doorsOpen: true );
				foreach ( var sq in shaped.Board.Squares )
					Eq( SquareFlags.None, shaped.Board.Flags( sq ) & SquareFlags.Void,
						$"{sq} is a void square and should not be on the board" );
				True( shaped.Board.Count > 0, "the board is not empty" );
			} );

			Test( "the AI never routes a figure through a square outside the tiles", () =>
			{
				// The end-to-end point of the shape work: orders stay inside the
				// playable area, because squares outside it no longer exist.
				var r = BuildCore1( withShapes: true, doorsOpen: true );
				var playable = r.Board.Squares.OrderBy( s => s.C ).ThenBy( s => s.R ).ToList();

				var plan = ActivationPlanner.Plan( r.Board,
					new[] { new EnemyFigure { Id = "E", Name = "Trooper",
						Position = playable.First(), Speed = 4, AttackKind = AttackKind.Ranged } },
					new[] { new TargetCandidate { Id = "H", Name = "Hero",
						Position = playable.Last(), MaxHealth = 10 } } );

				var fp = plan.Figures[0];
				foreach ( var step in fp.Path )
					True( r.Board.Exists( step ), $"path left the playable area at {step}" );
				for ( int i = 1; i < fp.Path.Count; i++ )
					True( r.Board.CanStep( fp.Path[i - 1], fp.Path[i] ),
						$"illegal step {fp.Path[i - 1]} -> {fp.Path[i]}" );
			} );

			Test( "map sections are built independently, not unioned", () =>
			{
				var dims = new Dictionary<string, (int w, int h)>
				{
					{ "Core_1", (2, 2) },
				};
				(int w, int h)? Lookup( string e, string i )
					=> dims.TryGetValue( e + "_" + i, out var d ) ? d : ((int, int)?)null;

				// Two tiles occupying exactly the same squares, in different sections.
				var tiles = new[]
				{
					new TilePlacement { Expansion = "Core", TileId = "1", Side = "A",
						X = 0, Y = 0, Rotation = 0, SectionGuid = "SECTION-A" },
					new TilePlacement { Expansion = "Core", TileId = "1", Side = "B",
						X = 0, Y = 0, Rotation = 0, SectionGuid = "SECTION-B" },
				};

				var unioned = BoardBuilder.Build( tiles, Lookup );
				Eq( 4, unioned.Overlaps.Count, "unioning both sections collides on every square" );
				True( unioned.Warnings.Any( w => w.Contains( "SECTION-A" ) && w.Contains( "SECTION-B" ) ),
					"and the warning names both sections" );

				var onlyA = BoardBuilder.Build( tiles, Lookup, null, null, g => g == "SECTION-A" );
				Eq( 0, onlyA.Overlaps.Count, "building one section at a time is clean" );
				Eq( 4, onlyA.Board.Count, "and still produces a board" );
			} );

			Test( "a within-section overlap is reported differently from a cross-section one", () =>
			{
				var dims = new Dictionary<string, (int w, int h)> { { "Core_1", (2, 2) } };
				(int w, int h)? Lookup( string e, string i )
					=> dims.TryGetValue( e + "_" + i, out var d ) ? d : ((int, int)?)null;

				var sameSection = new[]
				{
					new TilePlacement { Expansion = "Core", TileId = "1", Side = "A",
						X = 0, Y = 0, Rotation = 0, SectionGuid = "S" },
					new TilePlacement { Expansion = "Core", TileId = "1", Side = "B",
						X = 0, Y = 0, Rotation = 0, SectionGuid = "S" },
				};
				var r = BoardBuilder.Build( sameSection, Lookup );
				True( r.Warnings.All( w => w.Contains( "within one section" ) ),
					"reported as a data error, not a section swap" );
			} );

			Test( "authored terrain reaches the real board", () =>
			{
				var r = BuildCore1( withShapes: true, doorsOpen: true );

				int difficult = r.Board.Squares.Count(
					s => (r.Board.Flags( s ) & SquareFlags.Difficult) != 0 );
				int blocking = r.Board.Squares.Count(
					s => (r.Board.Flags( s ) & SquareFlags.Blocking) != 0 );

				True( difficult > 0, $"CORE1 carries reviewed difficult terrain ({difficult})" );
				True( blocking > 0, $"and reviewed blocking terrain ({blocking})" );

				foreach ( var s in r.Board.Squares )
				{
					if ( (r.Board.Flags( s ) & SquareFlags.Difficult) != 0 )
						Eq( 2, r.Board.EnterCost( s ), $"{s} is difficult and must cost 2" );
					if ( (r.Board.Flags( s ) & SquareFlags.Blocking) != 0 )
						False( r.Board.IsEnterable( s ), $"{s} is blocking and must be unenterable" );
				}
			} );

			Test( "the AI never routes through blocking terrain on the real map", () =>
			{
				var r = BuildCore1( withShapes: true, doorsOpen: true );
				var open = r.Board.Squares.Where( r.Board.IsEnterable )
					.OrderBy( s => s.C ).ThenBy( s => s.R ).ToList();
				True( open.Count > 50, "plenty of open board" );

				// Plan from several starts so the path crosses varied terrain.
				for ( int i = 0; i < open.Count; i += 37 )
				{
					var plan = ActivationPlanner.Plan( r.Board,
						new[] { new EnemyFigure { Id = "E", Name = "Trooper",
							Position = open[i], Speed = 4, AttackKind = AttackKind.Ranged } },
						new[] { new TargetCandidate { Id = "H", Name = "Hero",
							Position = open[(i + open.Count / 2) % open.Count], MaxHealth = 10 } } );

					var fp = plan.Figures[0];
					foreach ( var step in fp.Path )
					{
						True( r.Board.IsEnterable( step ),
							$"path entered an unenterable square at {step}" );
						Eq( SquareFlags.None, r.Board.Flags( step ) & SquareFlags.Blocking,
							$"path crossed blocking terrain at {step}" );
					}
					for ( int k = 1; k < fp.Path.Count; k++ )
						True( r.Board.CanStep( fp.Path[k - 1], fp.Path[k] ),
							$"illegal step {fp.Path[k - 1]} -> {fp.Path[k]}" );
				}
			} );

			Test( "difficult terrain makes the AI pay for it on the real map", () =>
			{
				// Walking across authored difficult terrain must cost more than
				// the same distance over open ground.
				var r = BuildCore1( withShapes: true, doorsOpen: true );
				var difficult = r.Board.Squares
					.Where( s => (r.Board.Flags( s ) & SquareFlags.Difficult) != 0 )
					.OrderBy( s => s.C ).ThenBy( s => s.R ).ToList();
				True( difficult.Count > 0, "there is difficult terrain to test" );

				var d = difficult.First();
				var from = r.Board.Neighbours( d ).FirstOrDefault(
					n => r.Board.IsEnterable( n ) && r.Board.CanStep( n, d ) );
				True( r.Board.Exists( from ), "found an adjacent open square" );

				var reach = Pathfinder.Compute( r.Board, from, 6 );
				Eq( 2, reach.CostTo( d ), "entering difficult terrain costs 2 movement points" );
			} );

			Test( "authored edges rotate with the tile", () =>
			{
				var terrain = new TerrainLibrary();
				terrain.Add( new TileTerrain
				{
					Expansion = "T", TileId = "1", Side = "A", Width = 2, Height = 2,
					Rows = new[] { "..", ".." },
					Edges = new[]
					{
						new TileEdge { C = 0, R = 0, Dir = "W", Type = EdgeType.Impassable },
					},
				} );
				(int w, int h)? Lookup( string e, string i ) => (2, 2);

				var expected = new (int rot, Sq owner, EdgeDir dir)[]
				{
					(0,   new Sq( 10, 10 ), EdgeDir.W),   // W stays W
					(90,  new Sq(  9, 10 ), EdgeDir.N),   // W becomes N
					(180, new Sq( 10,  9 ), EdgeDir.W),   // W becomes E, owned by the east neighbour
					(270, new Sq( 10, 10 ), EdgeDir.N),   // W becomes S, owned by the south neighbour
				};

				foreach ( var (rot, owner, dir) in expected )
				{
					var placed = new[]
					{
						new TilePlacement { Expansion = "T", TileId = "1", Side = "A",
							X = 10, Y = 10, Rotation = rot, SectionGuid = "S" },
					};
					var r = BoardBuilder.Build( placed, Lookup, null, terrain );

					Eq( EdgeType.Impassable, r.Board.Edge( owner, dir ),
						$"rotation {rot}: edge should sit at {owner}/{dir}" );

					int found = 0;
					for ( int c = 7; c <= 13; c++ )
					{
						for ( int rr = 7; rr <= 13; rr++ )
						{
							var sq = new Sq( c, rr );
							if ( r.Board.Edge( sq, EdgeDir.N ) == EdgeType.Impassable ) found++;
							if ( r.Board.Edge( sq, EdgeDir.W ) == EdgeType.Impassable ) found++;
						}
					}
					Eq( 1, found, $"rotation {rot}: exactly one impassable edge on the board" );
				}
			} );

			Test( "a direction rotates the same way the tile does", () =>
			{
				// The table above depends on this, so it is pinned separately:
				// clockwise rotation advances N -> E -> S -> W.
				Eq( "W", BoardBuilder.RotateDir( "W", 0 ), "no rotation" );
				Eq( "N", BoardBuilder.RotateDir( "W", 90 ), "W becomes N" );
				Eq( "E", BoardBuilder.RotateDir( "W", 180 ), "W becomes E" );
				Eq( "S", BoardBuilder.RotateDir( "W", 270 ), "W becomes S" );
				Eq( "N", BoardBuilder.RotateDir( "N", 360 ), "full turn is identity" );
			} );

			Test( "south and east edges normalise onto the neighbouring square", () =>
			{
				// Every edge is stored once, owned by a square via its N or W side.
				Eq( (new Sq( 5, 6 ), EdgeDir.N), BoardBuilder.Canonical( new Sq( 5, 5 ), "S" ),
					"south edge is the neighbour's north" );
				Eq( (new Sq( 6, 5 ), EdgeDir.W), BoardBuilder.Canonical( new Sq( 5, 5 ), "E" ),
					"east edge is the neighbour's west" );
			} );

			Test( "CORE1 is fully authored, and the terrain is all reachable by the AI", () =>
			{
				var r = BuildCore1( withShapes: true, doorsOpen: true );

				int difficult = 0, blocking = 0, impassable = 0;
				foreach ( var s in r.Board.Squares )
				{
					var f = r.Board.Flags( s );
					if ( (f & SquareFlags.Difficult) != 0 ) difficult++;
					if ( (f & SquareFlags.Blocking) != 0 ) blocking++;
					if ( (f & SquareFlags.Impassable) != 0 ) impassable++;
				}

				True( difficult > 0, $"reviewed difficult terrain is present ({difficult})" );
				True( blocking > 0, $"reviewed blocking terrain is present ({blocking})" );
				Eq( 0, r.Overlaps.Count, "and the board still has no overlaps" );

				var enterable = r.Board.Squares.Where( r.Board.IsEnterable ).ToList();
				var reach = Pathfinder.Compute( r.Board, enterable.First(), 500 );
				int reached = enterable.Count( s => reach.CanReach( s ) );
				True( reached > enterable.Count * 0.9,
					$"authored terrain must not island the map ({reached}/{enterable.Count})" );
			} );

			Test( "blocking edges stop movement but leave spaces adjacent", () =>
			{
				var r = BuildCore1( withShapes: true, doorsOpen: true );

				int blockingEdges = 0;
				foreach ( var s in r.Board.Squares )
				{
					foreach ( var dir in new[] { EdgeDir.N, EdgeDir.W } )
					{
						if ( r.Board.Edge( s, dir ) != EdgeType.Blocking ) continue;
						blockingEdges++;
						var other = dir == EdgeDir.N ? s.North : s.West;
						if ( !r.Board.Exists( other ) ) continue;
						False( r.Board.CanStep( s, other ), $"movement across {s}/{dir}" );
						True( r.Board.AreAdjacent( s, other ),
							$"a blocking edge does not sever adjacency at {s}/{dir}" );
					}
				}
				True( blockingEdges > 0, $"CORE1 carries authored blocking edges ({blockingEdges})" );
			} );

			Test( "every Core tile face CORE1 uses is reviewed, not shape-only", () =>
			{
				var lib = Core1Placements.Library();
				foreach ( var t in Core1Placements.Shapes )
				{
					True( t.Rows != null && t.Rows.Length == t.Height,
						$"{t.Expansion}_{t.TileId}{t.Side} has {t.Rows?.Length} rows, "
						+ $"expected {t.Height}" );
					foreach ( var row in t.Rows )
						Eq( t.Width, row.Length,
							$"{t.Expansion}_{t.TileId}{t.Side} row width" );
					True( lib.For( t.Expansion, t.TileId, t.Side ) != null,
						$"{t.Expansion}_{t.TileId}{t.Side} is in the library" );
				}
			} );

			Test( "authored terrain survives the round trip into the board", () =>
			{
				var r = BuildCore1( withShapes: true, doorsOpen: true );

				int claimed = 0;
				foreach ( var t in Core1Placements.Shapes )
					foreach ( var row in t.Rows )
						foreach ( var g in row )
							if ( g == 'd' || g == 'X' || g == 'I' ) claimed++;
				True( claimed > 0, "the fixture carries authored terrain" );

				int onBoard = r.Board.Squares.Count( s =>
					(r.Board.Flags( s ) & (SquareFlags.Difficult | SquareFlags.Blocking
						| SquareFlags.Impassable)) != 0 );
				True( onBoard > 0, $"and it reaches the board ({onBoard} squares)" );

				True( onBoard >= 3,
					$"a meaningful amount of terrain survived ({onBoard} of {claimed} claimed)" );
			} );

			Suite( "a second mission and expansion (TWIN1)" );

			Test( "TWIN1 builds clean with shapes applied", () =>
			{
				var doors = Twin1Placements.Doors.Select( d => new DoorPlacement
				{ X = d.X, Y = d.Y, Rotation = d.Rotation, Open = true } ).ToArray();

				var bbox = BoardBuilder.Build( Twin1Placements.Tiles, Twin1Placements.Lookup, doors );
				var shaped = BoardBuilder.Build( Twin1Placements.Tiles, Twin1Placements.Lookup,
					doors, Twin1Placements.Library() );

				True( bbox.Overlaps.Count > 0,
					$"bounding boxes collide on this mission ({bbox.Overlaps.Count})" );
				Eq( 0, shaped.Overlaps.Count, "real tile shapes do not collide" );
				True( shaped.Board.Count > 100, $"a real board was built ({shaped.Board.Count})" );

				var missing = shaped.Warnings.Where( w => w.Contains( "no dimensions" ) ).ToList();
				Eq( 0, missing.Count, "every tile resolved: " + string.Join( "; ", missing ) );
			} );

			Test( "the AI plans legally on TWIN1", () =>
			{
				var doors = Twin1Placements.Doors.Select( d => new DoorPlacement
				{ X = d.X, Y = d.Y, Rotation = d.Rotation, Open = true } ).ToArray();
				var r = BoardBuilder.Build( Twin1Placements.Tiles, Twin1Placements.Lookup,
					doors, Twin1Placements.Library() );

				var open = r.Board.Squares.Where( r.Board.IsEnterable )
					.OrderBy( s => s.C ).ThenBy( s => s.R ).ToList();
				True( open.Count > 50, "enough open board to plan on" );

				for ( int i = 0; i < open.Count; i += 29 )
				{
					var plan = ActivationPlanner.Plan( r.Board,
						new[] { new EnemyFigure { Id = "E", Name = "Trooper", Position = open[i],
							Speed = 4, AttackKind = AttackKind.Melee } },
						new[] { new TargetCandidate { Id = "H", Name = "Hero",
							Position = open[(i + open.Count / 3) % open.Count], MaxHealth = 10 } } );

					var fp = plan.Figures[0];
					True( r.Board.IsEnterable( fp.End ), $"ends somewhere legal ({fp.End})" );
					for ( int k = 1; k < fp.Path.Count; k++ )
						True( r.Board.CanStep( fp.Path[k - 1], fp.Path[k] ),
							$"illegal step {fp.Path[k - 1]} -> {fp.Path[k]}" );
					if ( fp.WillAttack )
						True( r.Board.AreAdjacent( fp.End, fp.Target.Position ),
							"a declared melee attack is from an adjacent square" );
				}
			} );

		}

		/// <summary>Squares reachable from the first square, doors as given.</summary>
		private static int Flood( BoardModel board )
		{
			var start = board.Squares.OrderBy( s => s.C ).ThenBy( s => s.R ).First();
			var seen = new HashSet<Sq> { start };
			var stack = new Stack<Sq>();
			stack.Push( start );
			while ( stack.Count > 0 )
			{
				var cur = stack.Pop();
				foreach ( var n in board.Neighbours( cur ) )
				{
					if ( seen.Contains( n ) ) continue;
					if ( !board.CanStep( cur, n ) ) continue;
					seen.Add( n );
					stack.Push( n );
				}
			}
			return seen.Count;
		}
	}
}
