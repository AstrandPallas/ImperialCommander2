using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Saga.Board
{
	/// <summary>One square of a disputed board, in a form a test can rebuild.</summary>
	public sealed class DisputeSquare
	{
		public int c;
		public int r;
		public string flags;
	}

	/// <summary>An edge that mattered to the argument.</summary>
	public sealed class DisputeEdge
	{
		public int c;
		public int r;
		public string dir;
		public string type;
	}

	/// <summary>A figure as it stood when the dispute was raised.</summary>
	public sealed class DisputeFigure
	{
		public string id;
		public string name;
		public int c;
		public int r;
		public bool hostile;
	}

	/// <summary>
	/// Everything needed to argue about one order after the fact, and to turn
	/// that argument into a regression test.
	/// </summary>
	/// <remarks>
	/// This is the mechanism by which the engine becomes correct IN PLAY.
	/// Simulation cannot see the cardboard: it is perfectly self-consistent
	/// with whatever terrain it was told about, so terrain that was read wrong
	/// off a picture stays wrong forever. The people at the table are the only
	/// ones who can see that, and they will see it exactly once, mid-mission,
	/// while trying to get on with the game.
	///
	/// So the capture has to be one tap and has to be complete. A snapshot that
	/// needs a follow-up question is a snapshot that gets thrown away.
	/// </remarks>
	public sealed class DisputeSnapshot
	{
		public int schemaVersion = 1;
		public string capturedUtc;
		public string missionId;
		public int round;

		/// <summary>What the players said was wrong, in their words.</summary>
		public string reason;

		public List<DisputeSquare> squares = new List<DisputeSquare>();
		public List<DisputeEdge> edges = new List<DisputeEdge>();
		public List<DisputeFigure> figures = new List<DisputeFigure>();

		/// <summary>The order that was given, and the reasoning behind it.</summary>
		public List<string> orders = new List<string>();
		public List<string> trace = new List<string>();

		/// <summary>The board as text, so the file is readable without the app.</summary>
		public string render;

		/// <summary>
		/// Capture a board, the figures on it and the plan that was disputed.
		/// </summary>
		/// <remarks>
		/// The whole board is written, not a window around the argument.
		/// Line of sight and pathing depend on geometry far outside the part
		/// anybody is looking at, so a cropped board reproduces a different
		/// question from the one that was asked.
		/// </remarks>
		public static DisputeSnapshot Capture(
			BoardModel board,
			ActivationPlan plan,
			IEnumerable<EnemyFigure> enemies = null,
			IEnumerable<TargetCandidate> rebels = null,
			string missionId = null,
			int round = 0,
			string reason = null )
		{
			var snap = new DisputeSnapshot
			{
				capturedUtc = DateTime.UtcNow.ToString( "o" ),
				missionId = missionId ?? "",
				round = round,
				reason = reason ?? "",
			};

			if ( board != null )
			{
				foreach ( var sq in board.Squares )
					snap.squares.Add( new DisputeSquare
					{ c = sq.C, r = sq.R, flags = board.Flags( sq ).ToString() } );

				foreach ( var sq in board.Squares )
					foreach ( var dir in new[] { EdgeDir.N, EdgeDir.W } )
					{
						var type = board.Edge( sq, dir );
						if ( type == EdgeType.Open ) continue;
						snap.edges.Add( new DisputeEdge
						{ c = sq.C, r = sq.R, dir = dir.ToString(), type = type.ToString() } );
					}
			}

			foreach ( var e in enemies ?? Enumerable.Empty<EnemyFigure>() )
			{
				if ( e == null ) continue;
				snap.figures.Add( new DisputeFigure
				{
					id = e.Id, name = e.Name,
					c = e.Position.C, r = e.Position.R, hostile = false,
				} );
			}

			foreach ( var t in rebels ?? Enumerable.Empty<TargetCandidate>() )
			{
				if ( t == null ) continue;
				snap.figures.Add( new DisputeFigure
				{
					id = t.Id, name = t.Name,
					c = t.Position.C, r = t.Position.R, hostile = true,
				} );
			}

			if ( plan != null )
			{
				foreach ( var fp in plan.Figures )
				{
					snap.orders.Add( fp.ToString() );
					snap.trace.Add( (fp.Figure?.Name ?? "figure") + ": "
						+ string.Join( " | ", fp.Trace ) );
				}
				if ( plan.GroupTarget != null )
					snap.trace.Insert( 0, "target: "
						+ string.Join( " | ", plan.GroupTarget.Trace ) );
			}

			try
			{
				if ( board != null && plan != null )
				{
					var glyphs = new Dictionary<Sq, char>();
					foreach ( var f in snap.figures )
						glyphs[new Sq( f.c, f.r )] = f.hostile ? 'R' : 'e';
					snap.render = BoardRenderer.RenderPlan( board, plan, glyphs );
				}
			}
			catch ( Exception e )
			{
				// A snapshot that fails because its own pretty-printer threw
				// would lose the report the players stopped the game to make.
				snap.render = "(could not render: " + e.Message + ")";
			}

			return snap;
		}

		/// <summary>A readable summary, for the top of the file and for a log.</summary>
		public string Describe()
		{
			var sb = new StringBuilder();
			sb.AppendLine( "RULES DISPUTE" );
			sb.AppendLine( "mission " + missionId + ", round " + round + ", " + capturedUtc );
			if ( !string.IsNullOrEmpty( reason ) ) sb.AppendLine( "reason: " + reason );
			sb.AppendLine( squares.Count + " squares, " + edges.Count + " non-open edges, "
				+ figures.Count + " figures" );
			sb.AppendLine();

			sb.AppendLine( "ORDERS" );
			foreach ( var o in orders ) sb.AppendLine( "  " + o );
			sb.AppendLine();

			sb.AppendLine( "WHY" );
			foreach ( var t in trace ) sb.AppendLine( "  " + t );

			if ( !string.IsNullOrEmpty( render ) )
			{
				sb.AppendLine();
				sb.AppendLine( "BOARD" );
				sb.AppendLine( render );
			}
			return sb.ToString();
		}

		/// <summary>Rebuild the board this snapshot recorded.</summary>
		/// <remarks>
		/// The other half of the round trip. Without it a snapshot is a bug
		/// report; with it, it is a test.
		/// </remarks>
		public BoardModel ToBoard()
		{
			var board = new BoardModel();
			foreach ( var s in squares )
			{
				var flags = SquareFlags.None;
				foreach ( var part in (s.flags ?? "").Split( ',' ) )
					if ( Enum.TryParse( part.Trim(), true, out SquareFlags f ) ) flags |= f;
				board.SetSquare( new Sq( s.c, s.r ), flags );
			}

			foreach ( var e in edges )
			{
				if ( !Enum.TryParse( e.dir, true, out EdgeDir dir ) ) continue;
				if ( !Enum.TryParse( e.type, true, out EdgeType type ) ) continue;
				board.SetEdge( new Sq( e.c, e.r ), dir, type );
			}
			return board;
		}
	}
}
