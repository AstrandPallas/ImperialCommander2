using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using Saga.Board;
using Saga.Tracking;
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

			// The card portrait sits over the body disc. Its sprite and scale
			// are set at runtime by FigureToken, since the mugshots are not
			// all imported at the same pixels-per-unit.
			var faceGo = new GameObject( "Portrait" );
			faceGo.transform.SetParent( root.transform, false );
			faceGo.transform.localPosition = new Vector3( 0f, 0f, -0.01f );
			var face = faceGo.AddComponent<SpriteRenderer>();
			face.sortingOrder = 2;
			face.enabled = false;

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
			labelRenderer.sortingOrder = 3;

			var token = root.AddComponent<FigureToken>();
			token.body = bodyGo.GetComponent<SpriteRenderer>();
			token.ring = ring.GetComponent<SpriteRenderer>();
			token.label = label;
			token.portrait = face;

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
			// 195 blocking and impassable edges plus 2,124 printed walls.
			const int expectedEdges = 2319;

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

			if ( library.Count != expectedFaces || terrain != expectedTerrainSquares
				|| edges != expectedEdges )
				Debug.LogError( summary + " -- EXPECTED " + expectedFaces + " faces, "
					+ expectedTerrainSquares + " terrain squares and " + expectedEdges
					+ " edges. Terrain has been lost between authoring and runtime." );
			else
				Debug.Log( summary + " -- matches the authored data" );
		}

		/// <summary>Build a real mission's board in the editor and report it.</summary>
		/// <remarks>
		/// The headless suite proves the engine against generated fixtures; this
		/// proves the RUNTIME path -- the game's own mission loader, tile
		/// descriptors and terrain loader -- produces the same board. A mistake
		/// in that seam would leave every test green and the game wrong.
		/// </remarks>
		[MenuItem( "Imperial Commander/Board/Verify Board Builds (CORE1)" )]
		public static void VerifyBoardBuilds()
		{
			const string missionPath = "Assets/Resources/SagaMissions/Core/CORE1.json";
			var asset = AssetDatabase.LoadAssetAtPath<TextAsset>( missionPath );
			if ( asset == null )
			{
				Debug.LogError( "BoardSetup: could not load " + missionPath );
				return;
			}

			var mission = FileManager.LoadMissionFromString( asset.text );
			if ( mission == null )
			{
				Debug.LogError( "BoardSetup: mission did not parse" );
				return;
			}

			var tiles = new List<SagaBoardBridge.TileInput>();
			foreach ( var section in mission.mapSections )
			{
				foreach ( var tile in section.mapTiles )
				{
					tiles.Add( new SagaBoardBridge.TileInput
					{
						Expansion = tile.expansion.ToString(),
						TileId = tile.tileID,
						Side = tile.tileSide,
						X = (int)(tile.entityPosition.X / 10f),
						Y = (int)(tile.entityPosition.Y / 10f),
						Rotation = ((int)tile.entityRotation % 360 + 360) % 360,
						SectionGuid = section.GUID.ToString(),
					} );
				}
			}

			var doors = new List<SagaBoardBridge.DoorInput>();
			var highlights = new List<(string Name, int C, int R)>();
			foreach ( var e in mission.mapEntities )
			{
				int c = (int)(e.entityPosition.X / 10f);
				int r = (int)(e.entityPosition.Y / 10f);
				if ( e.entityType == EntityType.Door )
					doors.Add( new SagaBoardBridge.DoorInput
					{
						X = c, Y = r,
						Rotation = ((int)e.entityRotation % 360 + 360) % 360,
						Open = e.entityProperties == null || e.entityProperties.isActive,
					} );
				else if ( e.entityType == EntityType.Highlight )
					highlights.Add( (e.name ?? "", c, r) );
			}

			var descriptors = TileDescriptor.LoadData();
			var board = SagaBoardBridge.Rebuild( tiles, doors,
				( exp, id ) =>
				{
					var d = descriptors.FirstOrDefault(
						x => x.expansion == exp && x.id.ToString() == id );
					return d == null ? ((int, int)?)null : (d.width, d.height);
				},
				TerrainLoader.Library, _ => true );

			int difficult = 0, blocking = 0, impassable = 0;
			foreach ( var sq in board.Squares )
			{
				var f = board.Flags( sq );
				if ( (f & SquareFlags.Difficult) != 0 ) difficult++;
				if ( (f & SquareFlags.Blocking) != 0 ) blocking++;
				if ( (f & SquareFlags.Impassable) != 0 ) impassable++;
			}

			var starts = Saga.Tracking.HeroPlacement.SuggestStarts( board, highlights, 4 );
			var build = SagaBoardBridge.LastBuild;

			Debug.Log( "BoardSetup: CORE1 -> " + tiles.Count + " tiles, " + doors.Count
				+ " doors, " + board.Count + " squares | difficult " + difficult
				+ ", blocking " + blocking + ", impassable " + impassable
				+ " | overlaps " + build.Overlaps.Count
				+ " | hero starts " + string.Join( ", ", starts ) );

			foreach ( var w in build.Warnings.Take( 5 ) )
				Debug.LogWarning( "BoardSetup: " + w );
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
		/// <summary>
		/// Run CORE1's entities through the real prefabs and ask the collectors
		/// what they see.
		/// </summary>
		/// <remarks>
		/// The headless suite cannot reach this. Its fixtures read the mission
		/// JSON directly, so they only ever see entityPosition in mission
		/// units -- while at runtime every prefab's Init overwrites it with
		/// world coordinates. That gap let every collector return nothing in
		/// the built game while 300-odd tests stayed green.
		///
		/// So this opens the real scene, instantiates the real entities and
		/// checks the answers, which is the only place that disagreement can
		/// actually be observed.
		/// </remarks>
		[MenuItem( "Imperial Commander/Board/Verify Runtime Entities (CORE1)" )]
		public static void VerifyRuntimeEntities()
		{
			var scene = EditorSceneManager.OpenScene( ScenePath, OpenSceneMode.Single );

			var manager = Object.FindObjectOfType<MapEntityManager>();
			if ( manager == null )
			{
				Debug.LogError( "RUNTIME ENTITIES: no MapEntityManager in " + ScenePath );
				return;
			}

			var asset = Resources.Load<TextAsset>( "SagaMissions/Core/CORE1" );
			if ( asset == null )
			{
				Debug.LogError( "RUNTIME ENTITIES: CORE1 could not be loaded" );
				return;
			}

			var mission = FileManager.LoadMissionFromString( asset.text );
			if ( mission == null )
			{
				Debug.LogError( "RUNTIME ENTITIES: CORE1 did not parse" );
				return;
			}

			DataStore.mission = mission;
			manager.InstantiateEntities( mission.mapEntities, false );

			var highlights = manager.CollectHighlights();
			var points = manager.CollectDeploymentPoints();
			var tokens = manager.CollectObjectiveTokens();
			var doors = manager.CollectBoardDoors();

			Debug.Log( $"RUNTIME ENTITIES: {highlights.Count} highlight(s), "
				+ $"{points.Count} deployment point(s), {tokens.Count} objective token(s), "
				+ $"{doors.Count} door(s)" );

			foreach ( var h in highlights )
				Debug.Log( $"RUNTIME ENTITIES:   highlight '{h.Name}' at ({h.C},{h.R})" );
			foreach ( var t in tokens )
				Debug.Log( $"RUNTIME ENTITIES:   {t.Kind} '{t.Name}' at ({t.C},{t.R})" );
			foreach ( var d in doors )
				Debug.Log( $"RUNTIME ENTITIES:   door at ({d.X},{d.Y}) rot {d.Rotation} "
					+ (d.Open ? "open" : "CLOSED") );

			// CORE1 is the mission every other check is anchored to, so its
			// numbers are known and stated rather than merely printed.
			bool ok = true;
			if ( highlights.Count != 1 ) { Debug.LogError( "RUNTIME ENTITIES: expected 1 highlight" ); ok = false; }
			if ( tokens.Count != 7 ) { Debug.LogError( "RUNTIME ENTITIES: expected 7 objective tokens (4 crates + 3 terminals)" ); ok = false; }
			if ( doors.Count != 3 ) { Debug.LogError( "RUNTIME ENTITIES: expected 3 doors" ); ok = false; }
			if ( points.Count == 0 ) { Debug.LogError( "RUNTIME ENTITIES: expected deployment points" ); ok = false; }

			var entrance = highlights.FirstOrDefault( h => HeroPlacement.IsEntrance( h.Name ) );
			if ( entrance.Name == null || entrance.C != 98 || entrance.R != 100 )
			{
				Debug.LogError( "RUNTIME ENTITIES: the entrance must be at (98,100), got "
					+ $"'{entrance.Name}' ({entrance.C},{entrance.R})" );
				ok = false;
			}

			Debug.Log( ok ? "RUNTIME ENTITIES: OK" : "RUNTIME ENTITIES: FAILED" );
		}

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
