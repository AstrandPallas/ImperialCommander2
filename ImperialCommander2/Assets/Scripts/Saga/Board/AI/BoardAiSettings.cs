namespace Saga.Board
{
	/// <summary>How much of the board the Imperial AI is trusted with.</summary>
	public enum AiMode
	{
		/// <summary>Square-level orders from the board model.</summary>
		StrictRules = 0,

		/// <summary>Upstream's abstract text, with no position awareness.</summary>
		Classic = 1,
	}

	/// <summary>
	/// The switch between board-aware orders and upstream's behaviour.
	/// </summary>
	/// <remarks>
	/// This exists because the board AI can be wrong in a way that upstream's
	/// cannot. Upstream says "Move 4 to attack Gaarkhan", which is vague enough
	/// to always be survivable. This fork names a square, and a named square
	/// derived from terrain that was read off a picture can be confidently,
	/// specifically wrong.
	///
	/// So there has to be a way back. A group that is arguing with the table,
	/// a tile whose terrain is mis-painted, a mission whose rules text the app
	/// cannot read -- any of these should cost a sentence of vagueness, not the
	/// evening.
	///
	/// The toggle is deliberately permanent rather than a migration aid. It is
	/// the reason a mis-painted wall is an annoyance instead of a session
	/// ending in an argument with a piece of software.
	/// </remarks>
	public static class BoardAiSettings
	{
		/// <summary>Board-aware orders unless the players say otherwise.</summary>
		public static AiMode Mode = AiMode.StrictRules;

		public static bool UseBoardAi => Mode == AiMode.StrictRules;

		/// <summary>Why the board AI is not being used, for the trace.</summary>
		public static string ClassicReason = "";

		/// <summary>
		/// Fall back to Classic, saying why.
		/// </summary>
		/// <remarks>
		/// Called when something makes the board untrustworthy rather than
		/// merely inconvenient. The reason travels with it, because a silent
		/// downgrade is indistinguishable from the feature never having worked.
		/// </remarks>
		public static void FallBackToClassic( string reason )
		{
			Mode = AiMode.Classic;
			ClassicReason = reason ?? "";
		}

		public static void UseStrictRules()
		{
			Mode = AiMode.StrictRules;
			ClassicReason = "";
		}
	}
}
