using System.Collections.Generic;
using System.Linq;
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
	/// <summary>
	/// The board following a card's activation text: the first line it can do,
	/// in the card's own order.
	/// </summary>
	public static class InstructionTests
	{
		private static BoardModel Open( int w = 16, int h = 8 )
		{
			var b = new BoardModel();
			for ( int c = 0; c < w; c++ )
				for ( int r = 0; r < h; r++ )
					b.SetSquare( new Sq( c, r ), SquareFlags.None );
			return b;
		}

		private static List<EnemyFigure> Melee( Sq at, int speed = 4,
			Footprint footprint = Footprint.Small1x1 )
			=> new List<EnemyFigure>
			{
				new EnemyFigure { Id = "e1", Name = "Nexu", Position = at, Speed = speed,
					AttackKind = AttackKind.Melee, Footprint = footprint },
			};

		private static List<TargetCandidate> Hero( Sq at, string id = "H1", string name = "Jyn" )
			=> new List<TargetCandidate>
			{
				new TargetCandidate { Id = id, Name = name, Position = at, MaxHealth = 10 },
			};

		// The Nexu (Elite)'s real card, verbatim from instructions.json.
		private static readonly string[] NexuLines =
		{
			"{A}{A} If this figure has suffered 6 or more {H}, recover 4 {H}. Then, move 10 to reposition 6.",
			"{Q} POUNCE: Place this figure in an empty space within 6 spaces and adjacent to 2 or more Rebels. Then attack {R1}.",
			"{Q} POUNCE: Place this figure in an empty space within 6 spaces and adjacent to {R1}. Then attack {R1}.",
			"{A} Move 3 to reposition 3.",
			"{A} Move 9 to reposition 3.",
		};

		public static void Register()
		{
			Suite( "instruction grammar" );

			Test( "each shape on the cards is read for what it asks", () =>
			{
				var a = Instructions.Parse( "{A} Move 4 to attack {R1}." );
				Eq( IntentKind.MoveAttack, a.Kind, "move to attack" );
				Eq( 4, a.Move, "with its movement" );

				var r = Instructions.Parse( "{A} Move 6 to reposition 3." );
				Eq( IntentKind.Reposition, r.Kind, "reposition" );
				Eq( 6, r.Move, "" ); Eq( 3, r.Within, "" );

				var t = Instructions.Parse( "{A} Move 5 toward the closest Rebel." );
				Eq( IntentKind.MoveToward, t.Kind, "toward" );

				var e = Instructions.Parse( "{A} Move 4 to engage as many Rebels as possible (minimum 2)." );
				Eq( IntentKind.Engage, e.Kind, "engage many" );
				Eq( 2, e.Minimum, "with a minimum" );

				var o = Instructions.Parse( "{A} Attack {R1}." );
				Eq( IntentKind.AttackOnly, o.Kind, "attack in place" );

				var p = Instructions.Parse( NexuLines[2] );
				Eq( IntentKind.PlaceAdjacentAttack, p.Kind, "pounce is a placement" );
				Eq( 6, p.Within, "within 6" );
				True( p.Attacks, "then attack" );

				var p2 = Instructions.Parse( NexuLines[1] );
				Eq( 2, p2.Minimum, "the two-Rebel pounce carries its minimum" );
			} );

			Test( "passives and conditionals are not things the board does", () =>
			{
				Eq( IntentKind.Passive, Instructions.Parse( "{-} CUNNING: While defending, apply +1 {G}." ).Kind,
					"a {-} line is a rule, not an action" );
				Eq( IntentKind.Unparsed, Instructions.Parse( NexuLines[0] ).Kind,
					"a conditional is for the players to judge" );
				False( Instructions.Parse( NexuLines[0] ).IsActionable, "and is never acted on" );
			} );

			Test( "the grammar reads the great majority of every shipped line", () =>
			{
				// Measured, not assumed. Passives and conditionals are expected
				// to be unparsed; anything else unparsed is a shape to add.
				var all = InstructionLines.All;
				int actionable = 0, passive = 0, conditional = 0, unknown = 0;
				var samples = new List<string>();
				foreach ( var l in all )
				{
					var i = Instructions.Parse( l.Text );
					if ( i.Kind == IntentKind.Passive ) passive++;
					else if ( i.IsActionable ) actionable++;
					else if ( Instructions.Plain( l.Text ).StartsWith( "If " ) ) conditional++;
					else { unknown++; if ( samples.Count < 6 ) samples.Add( Instructions.Plain( l.Text ) ); }
				}
				True( all.Length > 500, "the whole corpus is here" );
				True( actionable > all.Length * 0.6,
					$"most lines are actionable: {actionable}/{all.Length}" );
				True( unknown < all.Length * 0.08,
					$"few are unknown: {unknown} ({passive} passive, {conditional} conditional)\n  "
					+ string.Join( "\n  ", samples ) );
			} );

			Suite( "instruction-driven planning" );

			Test( "the board does the first line it can, in the card's order", () =>
			{
				// Two lines: attack within 3, else reposition 9. The hero is 6
				// away, so the first is impossible and the second is used.
				var b = Open();
				var lines = new[] { "{A} Move 3 to attack {R1}.", "{A} Move 9 to reposition 3." };
				var plan = ActivationPlanner.Plan( b, Melee( new Sq( 1, 4 ) ), Hero( new Sq( 8, 4 ) ),
					null, null, null, null, lines );
				var fp = plan.Figures[0];
				True( fp.Trace.Any( t => t.StartsWith( "line 1" ) && t.Contains( "no square" ) ),
					"line 1 is tried and found impossible: " + string.Join( " | ", fp.Trace ) );
				True( fp.Trace.Any( t => t.StartsWith( "line 2:" ) ),
					"so line 2 is what happens: " + string.Join( " | ", fp.Trace ) );
				True( fp.Moved, "and the figure moves" );
			} );

			Test( "a line the figure CAN do wins even when a later line would do more", () =>
			{
				var b = Open();
				var lines = new[] { "{A} Move 3 to attack {R1}.", "{A} Move 9 to reposition 3." };
				var plan = ActivationPlanner.Plan( b, Melee( new Sq( 1, 4 ) ), Hero( new Sq( 4, 4 ) ),
					null, null, null, null, lines );
				var fp = plan.Figures[0];
				True( fp.WillAttack, "line 1 attacks" );
				True( fp.Trace.Any( t => t.StartsWith( "line 1:" ) ), "and is the one used" );
				True( fp.MovementSpent <= 3, "spending no more than the line grants" );
			} );

			Test( "the movement comes from the line, not from the card's speed", () =>
			{
				// "Move 9" moves 9 even for a speed-4 figure: the line IS the
				// activation, and speed is what the generic plan falls back on.
				var b = Open( 20, 8 );
				var plan = ActivationPlanner.Plan( b, Melee( new Sq( 1, 4 ), speed: 4 ),
					Hero( new Sq( 18, 4 ) ), null, null, null, null,
					new[] { "{A} Move 9 to reposition 3." } );
				Eq( 9, plan.Figures[0].MovementSpent, "all nine are spent closing" );
			} );

			Test( "Pounce places the figure adjacent to its target and attacks", () =>
			{
				// The Nexu's real card. Jyn is 5 spaces off across difficult
				// ground that would cost far more than 5 to WALK -- but a
				// placement ignores movement cost entirely.
				var b = Open();
				for ( int c = 2; c < 6; c++ ) b.SetSquare( new Sq( c, 4 ), SquareFlags.Difficult );
				var plan = ActivationPlanner.Plan( b, Melee( new Sq( 1, 4 ) ), Hero( new Sq( 6, 4 ) ),
					null, null, null, null, NexuLines );
				var fp = plan.Figures[0];

				True( fp.Trace.Any( t => t.Contains( "PLACED" ) ), "it is placed: " + string.Join( " | ", fp.Trace ) );
				True( b.AreAdjacent( fp.End, new Sq( 6, 4 ) ), "adjacent to Jyn" );
				True( fp.WillAttack, "and attacks her" );
				Eq( 2, fp.Path.Count, "a placement is a jump, not a walk" );
				Eq( 0, fp.MovementSpent, "and costs no movement" );
			} );

			Test( "the two-Rebel Pounce is preferred when two Rebels stand together", () =>
			{
				var b = Open();
				var rebels = new List<TargetCandidate>
				{
					new TargetCandidate { Id = "H1", Name = "Jyn", Position = new Sq( 6, 4 ), MaxHealth = 10 },
					new TargetCandidate { Id = "H2", Name = "Gaarkhan", Position = new Sq( 6, 6 ), MaxHealth = 14 },
				};
				var plan = ActivationPlanner.Plan( b, Melee( new Sq( 1, 5 ) ), rebels,
					null, null, null, null, NexuLines );
				var fp = plan.Figures[0];
				True( fp.Trace.Any( t => t.StartsWith( "line 2:" ) ),
					"line 2, the two-Rebel pounce, is the first that works: " + string.Join( " | ", fp.Trace ) );
				True( b.AreAdjacent( fp.End, new Sq( 6, 4 ) ) && b.AreAdjacent( fp.End, new Sq( 6, 6 ) ),
					"ending adjacent to both" );
			} );

			Test( "when no Pounce is possible the Nexu falls through to reposition", () =>
			{
				var b = Open( 24, 8 );
				var plan = ActivationPlanner.Plan( b, Melee( new Sq( 1, 4 ), speed: 6 ),
					Hero( new Sq( 22, 4 ) ), null, null, null, null, NexuLines );
				var fp = plan.Figures[0];
				True( fp.Trace.Any( t => t.Contains( "place within 6" ) && t.Contains( "no empty space" ) ),
					"both pounces are tried and impossible" );
				True( fp.Trace.Any( t => t.StartsWith( "line 4:" ) ), "line 4 is used" );
				Eq( 3, fp.MovementSpent, "moving the 3 that line grants" );
			} );

			Test( "a large figure is only placed where its base fits", () =>
			{
				var b = Open( 12, 8 );
				// Wall off everything adjacent to the hero except a single
				// column of 1x1 squares, so no 2x2 can stand next to her.
				var hero = new Sq( 8, 4 );
				foreach ( var sq in b.Squares.ToList() )
					if ( Sq.Chebyshev( sq, hero ) == 1 && sq.C != 7 ) b.SetSquare( sq, SquareFlags.Blocking );
				for ( int r = 0; r < 8; r++ ) if ( r != 4 ) b.SetSquare( new Sq( 6, r ), SquareFlags.Blocking );

				var plan = ActivationPlanner.Plan( b, Melee( new Sq( 1, 4 ), footprint: Footprint.Large2x2 ),
					Hero( hero ), null, null, null, null, NexuLines );
				var fp = plan.Figures[0];
				False( fp.Trace.Any( t => t.Contains( "PLACED" ) ),
					"no pounce, because nowhere adjacent takes a 2x2: " + string.Join( " | ", fp.Trace ) );
			} );

			Test( "the target the card names is the target the board uses", () =>
			{
				// Upstream picks {R1} and prints it. The board must attack the
				// same hero, or the text and the token disagree in front of
				// everybody.
				var b = Open();
				var rebels = new List<TargetCandidate>
				{
					new TargetCandidate { Id = "H1", Name = "Jyn", Position = new Sq( 5, 4 ), MaxHealth = 10 },
					new TargetCandidate { Id = "H2", Name = "Gaarkhan", Position = new Sq( 5, 2 ), MaxHealth = 14 },
				};
				var ovrd = new PlanOverride { TargetId = "H2", Reason = "named by the card as {R1}" };
				var plan = ActivationPlanner.Plan( b, Melee( new Sq( 1, 4 ) ), rebels,
					null, null, ovrd, null, new[] { "{A} Move 6 to attack {R1}." } );
				Eq( "H2", plan.GroupTarget.Chosen.Id, "Gaarkhan, as the card says" );
				Eq( "H2", plan.Figures[0].Target.Id, "and that is who is attacked" );
			} );

			Test( "a Stunned figure cannot do a two-action line", () =>
			{
				// "A Stunned figure must spend one action to remove the
				// condition" -- so "{A}{A} ..." is out of reach, and "Move 3 to
				// attack" is only possible if the attack works from where it
				// stands. What it CAN still do is the one-action line.
				var b = Open();
				var fig = Melee( new Sq( 1, 4 ) );
				fig[0].Stunned = true;
				var lines = new[]
				{
					"{A}{A} Move 10 to reposition 1.",
					"{A} Move 3 to attack {R1}.",
					"{A} Move 3 to reposition 3.",
				};
				var plan = ActivationPlanner.Plan( b, fig, Hero( new Sq( 4, 4 ) ),
					null, null, null, null, lines );
				var fp = plan.Figures[0];
				True( fp.Trace.Any( t => t.Contains( "needs 2 actions" ) ),
					"the two-action line is skipped: " + string.Join( " | ", fp.Trace ) );
				True( fp.Trace.Any( t => t.Contains( "Stunned, and cannot attack" ) ),
					"move-to-attack is skipped, since it would need two actions" );
				True( fp.Trace.Any( t => t.StartsWith( "line 3:" ) ), "and the plain move is what happens" );
				False( fp.WillAttack, "with no attack" );
			} );

			Test( "a Stunned figure still attacks from where it stands", () =>
			{
				var b = Open();
				var fig = Melee( new Sq( 3, 4 ) );
				fig[0].Stunned = true;
				var plan = ActivationPlanner.Plan( b, fig, Hero( new Sq( 4, 4 ) ),
					null, null, null, null, new[] { "{A} Move 3 to attack {R1}." } );
				True( plan.Figures[0].WillAttack, "adjacent already, so one action suffices" );
				False( plan.Figures[0].Moved, "and it does not move" );
			} );

			Test( "with no lines the generic plan is unchanged", () =>
			{
				var b = Open();
				var a = ActivationPlanner.Plan( b, Melee( new Sq( 1, 4 ) ), Hero( new Sq( 4, 4 ) ) );
				var c = ActivationPlanner.Plan( b, Melee( new Sq( 1, 4 ) ), Hero( new Sq( 4, 4 ) ),
					null, null, null, null, new string[0] );
				Eq( a.Figures[0].End, c.Figures[0].End, "same square" );
			} );
		}
	}
}
