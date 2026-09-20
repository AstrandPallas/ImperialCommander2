using System;
using System.Collections.Generic;

namespace Saga.Board.Tests
{
	/// <summary>Minimal assert harness.</summary>
	public static class Harness
	{
		private sealed class Case
		{
			public string Suite;
			public string Name;
			public Action Body;
		}

		private static readonly List<Case> _cases = new List<Case>();
		private static string _suite = "misc";
		public static int Failures { get; private set; }
		public static int Passed { get; private set; }

		public static void Suite( string name ) => _suite = name;

		public static void Test( string name, Action body )
			=> _cases.Add( new Case { Suite = _suite, Name = name, Body = body } );

		public static int Run( string filter )
		{
			string current = null;
			foreach ( var c in _cases )
			{
				if ( filter != null && c.Name.IndexOf( filter, StringComparison.OrdinalIgnoreCase ) < 0
					 && c.Suite.IndexOf( filter, StringComparison.OrdinalIgnoreCase ) < 0 )
					continue;
				if ( c.Suite != current )
				{
					current = c.Suite;
					Console.WriteLine();
					Console.WriteLine( "== " + current );
				}
				try
				{
					c.Body();
					Passed++;
					Console.WriteLine( "  PASS  " + c.Name );
				}
				catch ( Exception ex )
				{
					Failures++;
					Console.WriteLine( "  FAIL  " + c.Name );
					foreach ( var line in ex.Message.Split( '\n' ) )
						Console.WriteLine( "        " + line );
				}
			}
			Console.WriteLine();
			Console.WriteLine( $"{Passed} passed, {Failures} failed" );
			return Failures == 0 ? 0 : 1;
		}

		// ---- assertions ----

		public static void True( bool actual, string what )
		{
			if ( !actual ) throw new Exception( $"expected TRUE but was FALSE: {what}" );
		}

		public static void False( bool actual, string what )
		{
			if ( actual ) throw new Exception( $"expected FALSE but was TRUE: {what}" );
		}

		public static void Eq<T>( T expected, T actual, string what )
		{
			if ( !EqualityComparer<T>.Default.Equals( expected, actual ) )
				throw new Exception( $"{what}\n  expected: {expected}\n  actual:   {actual}" );
		}

		public static void Throws<T>( Action a, string what ) where T : Exception
		{
			try { a(); }
			catch ( T ) { return; }
			catch ( Exception ex ) { throw new Exception( $"{what}: wrong exception {ex.GetType().Name}" ); }
			throw new Exception( $"{what}: expected {typeof( T ).Name}, nothing thrown" );
		}
	}
}
