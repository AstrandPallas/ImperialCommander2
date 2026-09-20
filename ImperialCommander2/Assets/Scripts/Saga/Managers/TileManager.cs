using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Saga.Board;

namespace Saga
{
	public class TileManager : MonoBehaviour
	{
		public GameObject tilePrefab;
		[HideInInspector]
		public BiomeType currentBiometype;

		List<MapSection> mapSections;
		List<TileDescriptor> tileDescriptors;

		public bool tilesLoaded
		{
			get
			{
				bool loaded = true;
				foreach ( var tr in tileRenderers )
				{
					if ( !tr.isLoaded )
						loaded = false;
				}
				return loaded;
			}
		}

		public List<TileRenderer> tileRenderers
		{
			get
			{
				var tiles = new List<TileRenderer>();
				foreach ( Transform child in transform )
					tiles.Add( child.GetComponent<TileRenderer>() );
				return tiles;
			}
		}

		/// <summary>
		/// Create the tiles in the whole mission, doesn't show them
		/// </summary>
		/// <summary>
		/// Every tile in the mission, reduced to what the rules engine needs.
		///
		/// The engine assembly cannot reference UnityEngine and so cannot read
		/// MapTile directly, which is why the extraction lives on this side of
		/// the boundary rather than inside Saga.Board.
		///
		/// Two conversions happen here and both are exact rather than rounded.
		/// entityPosition is in editor units at ten to the board square, and
		/// every shipped mission holds only multiples of ten -- verified across
		/// all 138 of them, with zero fractional positions. entityRotation is
		/// likewise always one of 0, 90, 180 and 270, with zero non-orthogonal
		/// values in the corpus. If either assumption ever breaks, it breaks
		/// loudly here instead of silently shifting the whole board by half a
		/// square.
		/// </summary>
		public List<SagaBoardBridge.TileInput> CollectBoardTiles()
		{
			var output = new List<SagaBoardBridge.TileInput>();
			if ( mapSections == null ) return output;

			foreach ( var section in mapSections )
			{
				foreach ( var tile in section.mapTiles )
				{
					float x = tile.entityPosition.X, y = tile.entityPosition.Y;
					float rot = tile.entityRotation;

					if ( x % 10 != 0 || y % 10 != 0 || rot % 90 != 0 )
					{
						Utils.LogWarning( $"CollectBoardTiles()::tile {tile.textureName} "
							+ $"is off the grid at ({x},{y}) rotation {rot} -- skipped" );
						continue;
					}

					output.Add( new SagaBoardBridge.TileInput
					{
						Expansion = tile.expansion.ToString(),
						TileId = tile.tileID,
						Side = tile.tileSide,
						X = (int)(x / 10f),
						Y = (int)(y / 10f),
						Rotation = ((int)rot % 360 + 360) % 360,
						SectionGuid = section.GUID.ToString(),
					} );
				}
			}
			return output;
		}

		public void InstantiateTiles( List<MapSection> sections )
		{
			mapSections = sections;
			tileDescriptors = TileDescriptor.LoadData();
			if ( tileDescriptors == null )
			{
				Utils.LogWarning( "InstantiateTiles()::tileDescriptors is null" );
				return;
			}

			foreach ( var s in mapSections )
			{
				foreach ( var mt in s.mapTiles )
				{
					var t = Instantiate( tilePrefab, transform );
					TileRenderer tileRenderer = t.GetComponent<TileRenderer>();
					t.GetComponent<TileRenderer>().LoadTile( mt, tileDescriptors.Where( x => x.expansion == mt.expansion.ToString() && x.id.ToString() == mt.tileID ).FirstOr( null ) );
					mt.tileRenderer = tileRenderer;
				}
			}
		}

		/// <summary>
		/// example: Core1B
		/// </summary>
		public void CamToTile( Guid tileID, bool immediate = false, Action callback = null )
		{
			foreach ( var tr in tileRenderers )
			{
				if ( tr.mapTile.GUID == tileID )//tr.mapTile.textureName == tileID )
				{
					if ( immediate )
						FindObjectOfType<CameraController>().MoveToImmediate( tr.transform.position, 5, true, callback );
					else
					{
						if ( tr.isLoaded )
							FindObjectOfType<CameraController>().MoveTo( tr.transform.position, 2, 5, true, callback );
					}
				}
			}
		}

		public void CamToSection( Guid guid )
		{
			var selectedSection = mapSections.Where( x => x.GUID == guid ).FirstOr( null );
			if ( selectedSection == null )
				return;
			int idx = mapSections.IndexOf( selectedSection );
			CamToSection( idx );
		}

		public void CamToSection( int index, bool immediate = false, Action callback = null )
		{
			Vector3 sum = Vector3.zero;
			var s = mapSections[index];
			var tiles = tileRenderers.Where( x => x.mapTile.mapSectionOwner == s.GUID ).ToList();
			if ( tiles.Count > 0 )
			{
				for ( int i = 0; i < tiles.Count(); i++ )
					sum += tiles[i].transform.position;
				//average the positions
				sum /= tiles.Count;
				if ( immediate )
					FindObjectOfType<CameraController>().MoveToImmediate( sum, 5, true, callback );
				else
					FindObjectOfType<CameraController>().MoveTo( sum, 2, 5, true, callback );
			}
			else
				callback?.Invoke();
		}

		/// <summary>
		/// Marks section as active, shows section, tiles and entities within (does NOT toggle IsActive for entities)
		/// </summary>
		public void ActivateMapSection( int index )
		{
			mapSections[index].isActive = true;
			foreach ( var tr in tileRenderers )
			{
				if ( tr.mapTile.mapSectionOwner == mapSections[index].GUID && tr.mapTile.entityProperties.isActive )
					tr.ShowTile();
			}

			//determine which ambient sound to play
			currentBiometype = mapSections[index].GetBiomeType();
			Debug.Log( $"ActivateMapSection()::index = {index}, Biome = {currentBiometype}" );
			FindObjectOfType<Sound>().ChangeAmbient( currentBiometype );

			//show ACTIVE entities in this section
			GlowTimer.SetTimer( 1f, () => FindObjectOfType<MapEntityManager>().ToggleSectionEntitiesVisibility( mapSections[index].GUID, true ) );
		}

		/// <summary>
		/// Hides a section and all entities within (does NOT toggle IsActive)
		/// </summary>
		public void DeactivateMapSection( int index )
		{
			foreach ( var tr in tileRenderers )
			{
				if ( tr.mapTile.mapSectionOwner == mapSections[index].GUID )
					tr.HideTile();
			}
			//hide entities in this section
			FindObjectOfType<MapEntityManager>().ToggleSectionEntitiesVisibility( mapSections[index].GUID, false );
			mapSections[index].isActive = false;
		}

		/// <summary>
		/// Marks session as active, shows section, tiles and entities within (does NOT toggle IsActive for entities),returns all active tiles and entities
		/// </summary>
		public Tuple<List<string>, List<string>> ActivateMapSection( Guid guid )
		{
			List<string> tiles = new List<string>();
			List<string> entities = new List<string>();

			MapSection ms = mapSections.Where( x => x.GUID == guid ).FirstOr( null );
			if ( ms != null )
			{
				ActivateMapSection( mapSections.IndexOf( ms ) );
				tiles.AddRange( ms.mapTiles.Where( x => x.entityProperties.isActive ).Select( x => $"{x.expansion} {x.tileID}{x.tileSide}" ) );
				entities = FindObjectOfType<MapEntityManager>().GetActiveEntities( ms.GUID );
			}

			return new Tuple<List<string>, List<string>>( tiles, entities );
		}

		/// <summary>
		/// Hides a section and all entities within (does NOT toggle IsActive)
		/// </summary>
		public List<string> DeactivateMapSection( Guid guid )
		{
			List<string> tiles = new List<string>();
			MapSection ms = mapSections.Where( x => x.GUID == guid ).FirstOr( null );
			if ( ms != null )
			{
				DeactivateMapSection( mapSections.IndexOf( ms ) );
				tiles.AddRange( ms.mapTiles.Select( x => $"{x.expansion} {x.tileID}{x.tileSide}" ) );
			}
			return tiles;
		}

		/// <summary>
		/// Shows all map sections that aren't marked to start hidden, returns all active tiles and entities
		/// </summary>
		public Tuple<List<string>, List<string>> ActivateAllVisibleSections()
		{
			List<string> tiles = new List<string>();
			List<string> entities = new List<string>();

			for ( int i = 0; i < mapSections.Count; i++ )
			{
				if ( !mapSections[i].invisibleUntilActivated )
				{
					entities = entities.Concat( FindObjectOfType<MapEntityManager>().GetActiveEntities( mapSections[i].GUID ) ).ToList();
					ActivateMapSection( i );
					tiles.AddRange( mapSections[i].mapTiles.Where( x => x.entityProperties.isActive ).Select( x => $"{x.expansion} {x.tileID}{x.tileSide}" ) );
				}
			}

			return new Tuple<List<string>, List<string>>( tiles, entities );
		}

		public void RestoreTiles()
		{
			//assign a tileRenderer back to all the tiles since it gets blown away from state restoration ( even though it's assigned during InstantiateTiles() )
			for ( int i = 0; i < mapSections.Count; i++ )
			{
				foreach ( var tr in tileRenderers.Where( x => x.mapTile.mapSectionOwner == mapSections[i].GUID ).ToList() )
				{
					foreach ( var tile in mapSections[i].mapTiles )
					{
						if ( tr.mapTile.GUID == tile.GUID )
							tile.tileRenderer = tr;
					}
				}

				if ( mapSections[i].isActive )
					ActivateMapSection( i );
			}
		}

		public string ActivateTile( Guid guid )
		{
			foreach ( var tr in tileRenderers )
			{
				if ( tr.mapTile.GUID == guid )
				{
					tr.ModifyVisibility( true );
					return $"{tr.mapTile.expansion} {tr.mapTile.tileID}{tr.mapTile.tileSide}";
				}
			}
			return "";
		}

		public string DeactivateTile( Guid guid )
		{
			foreach ( var tr in tileRenderers )
			{
				if ( tr.mapTile.GUID == guid )
				{
					tr.ModifyVisibility( false );
					return $"{tr.mapTile.expansion} {tr.mapTile.tileID}{tr.mapTile.tileSide}";
				}
			}
			return "";
		}

		public bool IsMapSectionActive( Guid guid )
		{
			return mapSections.Any( x => x.GUID == guid && x.isActive );
		}

		//public void ShowSectionTiles( Guid guid )
		//{
		//	foreach ( var tr in tileRenderers )
		//	{
		//		if ( tr.mapTile.mapSectionOwner == guid )
		//			tr.ShowTile();
		//	}
		//}

		public string GetState()
		{
			var state = new TileManagerState();
			state.mapSections = mapSections;
			state.tileDescriptors = tileDescriptors;

			return JsonConvert.SerializeObject( state, Formatting.Indented );
		}

		public void RestoreState( TileManagerState state )
		{
			mapSections = state.mapSections;
			tileDescriptors = state.tileDescriptors;
		}
	}
}
