using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Saga.EditorTools
{
	/// <summary>Builds the board view assets that cannot be authored as text.</summary>
	public static class BoardSetup
	{
		private const string Folder = "Assets/Resources/BoardView";
		private const string PrefabPath = Folder + "/FigureToken.prefab";
		private const string SpritePath = Folder + "/FigureDisc.png";

		[MenuItem( "Imperial Commander/Board/Build Figure Token Prefab" )]
		public static void BuildFigureToken()
		{
			Directory.CreateDirectory( Folder );
			var sprite = LoadOrCreateDisc();

			var root = new GameObject( "FigureToken" );
			// Tiles lie flat with the camera looking down, so the token's
			// sprites are rotated onto the ground plane to match.
			root.transform.rotation = Quaternion.Euler( 90f, 0f, 0f );

			var ring = MakeSprite( "Ring", root.transform, sprite, 1.00f, 0 );
			var bodyGo = MakeSprite( "Body", root.transform, sprite, 0.74f, 1 );

			var labelGo = new GameObject( "Label" );
			labelGo.transform.SetParent( root.transform, false );
			labelGo.transform.localPosition = new Vector3( 0f, 0f, -0.02f );
			labelGo.transform.localScale = Vector3.one * 0.08f;
			var label = labelGo.AddComponent<TextMesh>();
			label.anchor = TextAnchor.MiddleCenter;
			label.alignment = TextAlignment.Center;
			label.fontSize = 64;
			label.characterSize = 0.5f;
			label.color = Color.white;
			label.text = "1";
			var labelRenderer = labelGo.GetComponent<MeshRenderer>();
			labelRenderer.sortingOrder = 2;

			var token = root.AddComponent<FigureToken>();
			token.body = bodyGo.GetComponent<SpriteRenderer>();
			token.ring = ring.GetComponent<SpriteRenderer>();
			token.label = label;

			PrefabUtility.SaveAsPrefabAsset( root, PrefabPath );
			Object.DestroyImmediate( root );
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log( "BoardSetup: wrote " + PrefabPath );
		}

		/// <summary>Check the authored terrain actually loads at runtime.</summary>
		/// <remarks>
		/// Terrain that fails to load does not throw: every tile simply reads
		/// as open floor, and the AI confidently walks through walls the
		/// players can see. The totals are checked against the authoring tool's
		/// own count so a silent loss shows up here instead of at the table.
		/// </remarks>
		[MenuItem( "Imperial Commander/Board/Verify Terrain Loads" )]
		public static void VerifyTerrainLoads()
		{
			const int expectedFaces = 276;
			const int expectedTerrainSquares = 332;

			TerrainLoader.Reload();
			var library = TerrainLoader.Library;

			int terrain = 0;
			int edges = 0;
			foreach ( var face in library.All )
			{
				if ( face.Rows != null )
					foreach ( var row in face.Rows )
						foreach ( var ch in row )
							if ( ch == 'd' || ch == 'X' || ch == 'I' || ch == 'P' ) terrain++;
				if ( face.Edges != null ) edges += face.Edges.Length;
			}

			string summary = "TerrainLoader: " + library.Count + " faces, " + terrain
				+ " terrain squares, " + edges + " edges";

			if ( library.Count != expectedFaces || terrain != expectedTerrainSquares )
				Debug.LogError( summary + " -- EXPECTED " + expectedFaces + " faces and "
					+ expectedTerrainSquares + " terrain squares. Terrain has been lost "
					+ "between authoring and runtime." );
			else
				Debug.Log( summary + " -- matches the authored data" );
		}

		private const string ScenePath = "Assets/Scenes/Saga.unity";
		private const string LayerName = "FigureLayer";

		/// <summary>Put a FigureLayer in the Saga scene, beside the tiles.</summary>
		/// <remarks>
		/// The layer has to share the tiles' coordinate space, because a figure
		/// on square (c, r) is placed at (c + 0.5, y, -(r + 0.5)) in the same
		/// world the tiles are laid out in. Parenting it anywhere else would
		/// need a second transform nobody would remember to keep in step.
		/// </remarks>
		[MenuItem( "Imperial Commander/Board/Add Board View To Scene" )]
		public static void AddFigureLayerToScene()
		{
			var scene = EditorSceneManager.OpenScene( ScenePath, OpenSceneMode.Single );

			var tileManager = Object.FindObjectOfType<TileManager>();
			if ( tileManager == null )
			{
				Debug.LogError( "BoardSetup: no TileManager in " + ScenePath
					+ ", so there is nothing to sit the figures beside" );
				return;
			}

			// Idempotent on purpose: this gets re-run whenever a component is
			// added, so it tops up what is missing rather than refusing once
			// anything exists.
			var layer = Object.FindObjectOfType<FigureLayer>();
			GameObject host;
			if ( layer != null )
			{
				host = layer.gameObject;
			}
			else
			{
				host = new GameObject( LayerName );
				host.transform.SetParent( tileManager.transform.parent, false );
				host.transform.localPosition = Vector3.zero;
				host.transform.localRotation = Quaternion.identity;
				host.transform.localScale = Vector3.one;
				host.AddComponent<FigureLayer>();
			}

			// The controller and the dragger ride on the same object: they are
			// one feature, and splitting them only creates references somebody
			// has to reconnect by hand.
			Ensure<SagaBoardController>( host );
			Ensure<HeroPinDragger>( host );

			AddTrackerPanel();

			EditorSceneManager.MarkSceneDirty( scene );
			EditorSceneManager.SaveScene( scene );
			Debug.Log( "BoardSetup: board view ready on " + host.name + " in " + ScenePath );
		}

		private static T Ensure<T>( GameObject go ) where T : Component
		{
			var existing = go.GetComponent<T>();
			if ( existing != null ) return existing;
			Debug.Log( "BoardSetup: added " + typeof( T ).Name + " to " + go.name );
			return go.AddComponent<T>();
		}

		/// <summary>Put the tracker panel on a canvas, anchored to the right edge.</summary>
		private static void AddTrackerPanel()
		{
			if ( Object.FindObjectOfType<TrackerPanel>() != null ) return;

			var canvas = Object.FindObjectsOfType<Canvas>()
				.FirstOrDefault( c => c.renderMode != RenderMode.WorldSpace );
			if ( canvas == null )
			{
				Debug.LogWarning( "BoardSetup: no screen canvas, tracker panel not added" );
				return;
			}

			var go = new GameObject( "TrackerPanel", typeof( RectTransform ) );
			go.transform.SetParent( canvas.transform, false );

			var rect = go.GetComponent<RectTransform>();
			rect.anchorMin = new Vector2( 1f, 0.5f );
			rect.anchorMax = new Vector2( 1f, 0.5f );
			rect.pivot = new Vector2( 1f, 0.5f );
			rect.anchoredPosition = new Vector2( -12f, 0f );
			rect.sizeDelta = new Vector2( 900f, 500f );

			go.AddComponent<TrackerPanel>();
			Debug.Log( "BoardSetup: added TrackerPanel to " + canvas.name );
		}

		private static GameObject MakeSprite( string name, Transform parent, Sprite sprite,
			float size, int order )
		{
			var go = new GameObject( name );
			go.transform.SetParent( parent, false );
			go.transform.localScale = Vector3.one * size;
			var sr = go.AddComponent<SpriteRenderer>();
			sr.sprite = sprite;
			sr.sortingOrder = order;
			return go;
		}

		/// <summary>
		/// A plain white disc, generated rather than shipped so the prefab has
		/// no external art dependency. Colour is applied per figure at runtime.
		/// </summary>
		private static Sprite LoadOrCreateDisc()
		{
			var existing = AssetDatabase.LoadAssetAtPath<Sprite>( SpritePath );
			if ( existing != null ) return existing;

			const int size = 128;
			var tex = new Texture2D( size, size, TextureFormat.RGBA32, false );
			float r = size / 2f - 1f;
			var centre = new Vector2( size / 2f, size / 2f );
			for ( int y = 0; y < size; y++ )
			{
				for ( int x = 0; x < size; x++ )
				{
					float d = Vector2.Distance( new Vector2( x + 0.5f, y + 0.5f ), centre );
					float a = Mathf.Clamp01( r - d );
					tex.SetPixel( x, y, new Color( 1f, 1f, 1f, a ) );
				}
			}
			tex.Apply();
			File.WriteAllBytes( SpritePath, tex.EncodeToPNG() );
			Object.DestroyImmediate( tex );
			AssetDatabase.ImportAsset( SpritePath, ImportAssetOptions.ForceUpdate );

			var importer = (TextureImporter)AssetImporter.GetAtPath( SpritePath );
			importer.textureType = TextureImporterType.Sprite;
			importer.spritePixelsPerUnit = 128;
			importer.alphaIsTransparency = true;
			importer.SaveAndReimport();

			return AssetDatabase.LoadAssetAtPath<Sprite>( SpritePath );
		}
	}
}
