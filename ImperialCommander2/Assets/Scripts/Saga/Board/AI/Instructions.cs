using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Saga.Board
{
	/// <summary>What one line of a deployment card's activation text asks for.</summary>
	public enum IntentKind
	{
		/// <summary>"Move N to attack {R1}."</summary>
		MoveAttack,

		/// <summary>"Move N toward {R1}" / "toward the closest Rebel".</summary>
		MoveToward,

		/// <summary>"Move N to reposition M": move, trying to end within M of the target.</summary>
		Reposition,

		/// <summary>"Move N to engage as many Rebels as possible (minimum M)".</summary>
		Engage,

		/// <summary>"Attack {R1}." with no movement.</summary>
		AttackOnly,

		/// <summary>"POUNCE: Place this figure in an empty space within N spaces and adjacent to ... Then attack."</summary>
		PlaceAdjacentAttack,

		/// <summary>A "{-}" line: a passive or a rule, not a thing the board can do.</summary>
		Passive,

		/// <summary>A conditional or something the parser does not understand.</summary>
		Unparsed,
	}

	/// <summary>One instruction line, read into something the planner can try.</summary>
	public sealed class ActivationIntent
	{
		public IntentKind Kind = IntentKind.Unparsed;

		/// <summary>Movement points the line grants, where it grants any.</summary>
		public int Move;

		/// <summary>"within N spaces" / "reposition N".</summary>
		public int Within;

		/// <summary>"minimum N" Rebels to engage, or "adjacent to N or more".</summary>
		public int Minimum = 1;

		/// <summary>Whether the line ends in an attack.</summary>
		public bool Attacks;

		/// <summary>Zero-based position on the card.</summary>
		public int Line;

		/// <summary>
		/// Actions the line costs: the count of leading {A} glyphs, with {Q}
		/// counting as one. A line that moves AND attacks costs two regardless
		/// of its glyphs, since those are two actions under the rules.
		/// </summary>
		public int Actions = 1;

		public string Raw = "";

		public bool IsActionable
			=> Kind != IntentKind.Passive && Kind != IntentKind.Unparsed;

		public override string ToString()
			=> $"line {Line + 1}: {Kind}"
			   + (Move > 0 ? $" move {Move}" : "")
			   + (Within > 0 ? $" within {Within}" : "")
			   + (Minimum > 1 ? $" min {Minimum}" : "")
			   + (Attacks ? " then attack" : "");
	}

	/// <summary>
	/// Reads a card's activation text into intents the board can try in order.
	/// </summary>
	/// <remarks>
	/// This is how the app's text and the board's orders stop being two
	/// unrelated answers. A card's lines are a fallthrough: the group does the
	/// FIRST one it can, and only if that is impossible does it try the next.
	/// The board used to plan a generic move-and-attack no matter what the
	/// card said, so the text could read "Pounce 6" while the token walked 4
	/// -- or, when its base did not fit, did nothing.
	///
	/// The grammar is small on purpose. Five shapes cover the great majority of
	/// the 521 shipped lines; a "{-}" line is a passive that the board cannot
	/// act on, and a conditional ("If this figure ...") is left to the players
	/// because the board cannot evaluate the condition. Anything it does not
	/// understand is reported as such and skipped, never guessed at.
	/// </remarks>
	public static class Instructions
	{
		private static readonly Regex Glyph = new Regex( @"\{[^}]*\}", RegexOptions.Compiled );
		private static readonly Regex Tag = new Regex( @"<[^>]+>", RegexOptions.Compiled );

		private static readonly Regex Place = new Regex(
			@"place this figure in an empty space within (\d+) spaces?(?: and adjacent to (?:(\d+) or more )?)?",
			RegexOptions.IgnoreCase | RegexOptions.Compiled );
		private static readonly Regex MoveAttack = new Regex(
			@"\bmove (\d+) to attack", RegexOptions.IgnoreCase | RegexOptions.Compiled );
		private static readonly Regex Reposition = new Regex(
			@"\bmove (\d+) to reposition (\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled );
		private static readonly Regex Toward = new Regex(
			@"\bmove (\d+) toward", RegexOptions.IgnoreCase | RegexOptions.Compiled );
		private static readonly Regex EngageMany = new Regex(
			@"\bmove (\d+) to engage as many rebels as possible \(minimum (\d+)\)",
			RegexOptions.IgnoreCase | RegexOptions.Compiled );
		private static readonly Regex EngageOne = new Regex(
			@"\bmove (\d+) to engage", RegexOptions.IgnoreCase | RegexOptions.Compiled );
		private static readonly Regex AttackOnly = new Regex(
			@"^\s*attack\b", RegexOptions.IgnoreCase | RegexOptions.Compiled );

		/// <summary>Strip the glyph codes and colour tags the text is decorated with.</summary>
		public static string Plain( string line )
		{
			if ( string.IsNullOrEmpty( line ) ) return "";
			var s = Tag.Replace( line, "" );
			s = Glyph.Replace( s, " " );
			return Regex.Replace( s, @"\s+", " " ).Trim();
		}

		public static ActivationIntent Parse( string line, int index = 0 )
		{
			var intent = new ActivationIntent { Line = index, Raw = line ?? "" };
			if ( string.IsNullOrEmpty( line ) ) return intent;

			// "{-}" marks a passive: a rule that applies during the activation,
			// not an action the figure takes.
			if ( line.TrimStart().StartsWith( "{-}", StringComparison.Ordinal ) )
			{
				intent.Kind = IntentKind.Passive;
				return intent;
			}

			var text = Plain( line );

			// Leading glyphs say what the line costs in actions.
			int glyphs = Regex.Matches( line.TrimStart(), @"^(\{A\}\s*)+" ).Count > 0
				? Regex.Matches( Regex.Match( line.TrimStart(), @"^(\{A\}\s*)+" ).Value, @"\{A\}" ).Count
				: 1;
			intent.Actions = Math.Max( 1, glyphs );

			// A condition the board cannot read decides whether the rest even
			// applies. The players resolve those; the board does not pretend.
			if ( text.StartsWith( "If ", StringComparison.OrdinalIgnoreCase ) )
				return intent;

			Match m;
			if ( (m = Place.Match( text )).Success )
			{
				intent.Kind = IntentKind.PlaceAdjacentAttack;
				intent.Within = int.Parse( m.Groups[1].Value );
				intent.Minimum = m.Groups[2].Success ? int.Parse( m.Groups[2].Value ) : 1;
				intent.Attacks = text.IndexOf( "then attack", StringComparison.OrdinalIgnoreCase ) >= 0;
				return intent;
			}
			if ( (m = Reposition.Match( text )).Success )
			{
				intent.Kind = IntentKind.Reposition;
				intent.Move = int.Parse( m.Groups[1].Value );
				intent.Within = int.Parse( m.Groups[2].Value );
				return intent;
			}
			if ( (m = MoveAttack.Match( text )).Success )
			{
				intent.Kind = IntentKind.MoveAttack;
				intent.Move = int.Parse( m.Groups[1].Value );
				intent.Attacks = true;
				return intent;
			}
			if ( (m = EngageMany.Match( text )).Success )
			{
				intent.Kind = IntentKind.Engage;
				intent.Move = int.Parse( m.Groups[1].Value );
				intent.Minimum = int.Parse( m.Groups[2].Value );
				intent.Attacks = true;
				return intent;
			}
			if ( (m = EngageOne.Match( text )).Success )
			{
				intent.Kind = IntentKind.Engage;
				intent.Move = int.Parse( m.Groups[1].Value );
				intent.Minimum = 1;
				intent.Attacks = true;
				return intent;
			}
			if ( (m = Toward.Match( text )).Success )
			{
				intent.Kind = IntentKind.MoveToward;
				intent.Move = int.Parse( m.Groups[1].Value );
				return intent;
			}
			if ( AttackOnly.IsMatch( text ) )
			{
				intent.Kind = IntentKind.AttackOnly;
				intent.Attacks = true;
				return intent;
			}
			return intent;
		}

		public static List<ActivationIntent> ParseAll( IEnumerable<string> lines )
		{
			var list = new List<ActivationIntent>();
			if ( lines == null ) return list;
			int i = 0;
			foreach ( var l in lines ) list.Add( Parse( l, i++ ) );
			return list;
		}
	}
}
