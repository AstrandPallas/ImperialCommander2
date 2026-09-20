using System.IO;
using UnityEditor;
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
