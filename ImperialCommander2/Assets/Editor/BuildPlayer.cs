using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Saga.EditorTools
{
	/// <summary>
	/// Produces a standalone Windows build, from the menu or from the command
	/// line.
	/// </summary>
	/// <remarks>
	/// The Addressables content build has to run FIRST and is the part that is
	/// easy to forget: the tile art is served through Addressables, so a player
	/// built without it starts, reaches a mission, and then renders an empty
	/// board. That failure looks like a bug in the board code rather than a
	/// missing build step, which is exactly why it is done here rather than
	/// left to whoever runs the build.
	/// </remarks>
	public static class BuildPlayer
	{
		private const string DefaultOutput = "Build/Windows";
		private const string ExeName = "ImperialCommander2.exe";

		[MenuItem( "Imperial Commander/Build Windows Player" )]
		public static void BuildFromMenu()
		{
			var path = Path.GetFullPath( Path.Combine(
				Path.GetDirectoryName( Application.dataPath ) ?? ".", "..", DefaultOutput ) );
			if ( Run( path, out string error ) )
				EditorUtility.DisplayDialog( "Build finished", path, "OK" );
			else
				EditorUtility.DisplayDialog( "Build failed", error, "OK" );
		}

		/// <summary>Entry point for -executeMethod. Exits non-zero on failure.</summary>
		public static void BuildFromCommandLine()
		{
			string output = Argument( "-buildOutput" ) ?? Path.GetFullPath( Path.Combine(
				Path.GetDirectoryName( Application.dataPath ) ?? ".", "..", DefaultOutput ) );

			bool ok = Run( output, out string error );
			if ( !ok ) Debug.LogError( "BUILD FAILED: " + error );

			// Without an explicit exit code a batchmode build reports success
			// however badly it went, and a broken build then ships quietly.
			EditorApplication.Exit( ok ? 0 : 1 );
		}

		private static bool Run( string output, out string error )
		{
			error = null;
			try
			{
				Directory.CreateDirectory( output );

				Debug.Log( "BUILD: addressable content first, or the board renders empty" );
				AddressableAssetSettings.BuildPlayerContent( out AddressablesPlayerBuildResult addr );
				if ( !string.IsNullOrEmpty( addr?.Error ) )
				{
					error = "addressables: " + addr.Error;
					return false;
				}
				Debug.Log( "BUILD: addressable content done in " + addr?.Duration + "s" );

				var scenes = EditorBuildSettings.scenes
					.Where( s => s.enabled && !string.IsNullOrEmpty( s.path ) )
					.Select( s => s.path )
					.ToArray();
				if ( scenes.Length == 0 )
				{
					error = "no enabled scenes in Build Settings";
					return false;
				}
				Debug.Log( "BUILD: " + scenes.Length + " scenes -> " + output );

				var options = new BuildPlayerOptions
				{
					scenes = scenes,
					locationPathName = Path.Combine( output, ExeName ),
					target = BuildTarget.StandaloneWindows64,
					targetGroup = BuildTargetGroup.Standalone,
					options = BuildOptions.None,
				};

				var report = BuildPipeline.BuildPlayer( options );
				var summary = report.summary;
				Debug.Log( "BUILD: " + summary.result + ", " + summary.totalSize
					+ " bytes, " + summary.totalTime );

				if ( summary.result == BuildResult.Succeeded ) return true;

				error = summary.result + " with " + summary.totalErrors + " error(s)";
				return false;
			}
			catch ( Exception e )
			{
				error = e.Message;
				return false;
			}
		}

		private static string Argument( string name )
		{
			var args = Environment.GetCommandLineArgs();
			for ( int i = 0; i < args.Length - 1; i++ )
				if ( string.Equals( args[i], name, StringComparison.OrdinalIgnoreCase ) )
					return args[i + 1];
			return null;
		}
	}
}
