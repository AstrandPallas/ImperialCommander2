using Saga.Board;
using Saga.Tracking;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Saga
{
	/// <summary>
	/// Drag a hero from the strip on the left onto a square of the board.
	/// </summary>
	/// <remarks>
	/// Heroes are seated automatically at the mission's entrance, but that only
	/// works when the mission HAS an entrance marker -- two of the shipped
	/// missions do not -- and it says nothing about where the party actually
	/// went once play starts. Dragging the portrait is the obvious gesture and
	/// the one people reach for, so it is worth having even though dragging the
	/// token already on the board does the same job.
	///
	/// Added at runtime by SagaHGPrefab rather than baked into the prefab, so
	/// the scene asset does not have to be edited and the feature survives an
	/// upstream change to that prefab.
	/// </remarks>
	public class HeroTileDragger : MonoBehaviour,
		IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		/// <summary>Round stamped on a position when one is set this way.</summary>
		public int CurrentRound { get; set; }

		private SagaBoardController _board;
		private Camera _camera;
		private string _cardId;
		private string _cardName;
		private GameObject _ghost;

		private void Awake()
		{
			_board = FindObjectOfType<SagaBoardController>();
			_camera = Camera.main;
		}

		public void OnBeginDrag( PointerEventData e )
		{
			var prefab = GetComponent<SagaHGPrefab>();
			var card = prefab != null ? prefab.Card : null;
			if ( card == null || card.isDummy ) return;

			_cardId = card.id;
			_cardName = card.name;
			if ( _board == null ) _board = FindObjectOfType<SagaBoardController>();
			if ( _camera == null ) _camera = Camera.main;

			MakeGhost( e );
		}

		public void OnDrag( PointerEventData e )
		{
			if ( _ghost != null ) _ghost.transform.position = e.position;
		}

		public void OnEndDrag( PointerEventData e )
		{
			if ( _ghost != null ) Destroy( _ghost );
			_ghost = null;

			if ( string.IsNullOrEmpty( _cardId ) ) return;
			string id = _cardId;
			_cardId = null;

			if ( _board == null || !_board.IsReady || _camera == null ) return;

			// Dropped away from the board rather than onto it: a cancelled
			// drag, not a placement at the edge of the map.
			if ( !PointerSquare( e.position, out var wanted ) ) return;

			var hero = FindHero( id );
			if ( hero == null )
			{
				Utils.LogWarning( "HeroTileDragger::" + _cardName
					+ " is not tracked on this board, so it cannot be placed" );
				return;
			}

			var occupied = HeroPlacement.OccupiedSquares(
				OtherHeroes( hero ), _board.Groups );
			var landed = HeroPlacement.Snap( _board.Board, wanted, occupied );
			if ( landed == null )
			{
				Utils.LogWarning( "HeroTileDragger::nothing free near " + wanted
					+ ", so " + _cardName + " was not placed" );
				return;
			}

			_board.Undo?.Record( _cardName + " placed at " + landed.Value );
			TrackerBridge.SetHeroPosition( hero, landed.Value, CurrentRound );
			_board.RefreshTokens();
			Utils.LogWarning( "HeroTileDragger::" + _cardName + " placed at " + landed.Value );
		}

		private HeroCombatState FindHero( string cardId )
		{
			foreach ( var h in _board.Heroes )
				if ( h != null && h.CardId == cardId ) return h;
			return null;
		}

		private System.Collections.Generic.List<HeroCombatState> OtherHeroes( HeroCombatState self )
		{
			var others = new System.Collections.Generic.List<HeroCombatState>();
			foreach ( var h in _board.Heroes )
				if ( h != self ) others.Add( h );
			return others;
		}

		/// <summary>A portrait that follows the pointer, so the drag reads as one.</summary>
		private void MakeGhost( PointerEventData e )
		{
			var source = GetComponentInChildren<Image>();
			var canvas = GetComponentInParent<Canvas>();
			if ( source == null || canvas == null ) return;

			_ghost = new GameObject( "HeroDragGhost", typeof( RectTransform ) );
			_ghost.transform.SetParent( canvas.transform, false );
			_ghost.transform.SetAsLastSibling();

			var image = _ghost.AddComponent<Image>();
			image.sprite = source.sprite;
			image.color = new Color( 1f, 1f, 1f, 0.75f );
			image.raycastTarget = false;          // must not eat its own drop

			var rect = _ghost.GetComponent<RectTransform>();
			rect.sizeDelta = new Vector2( 90f, 90f );
			rect.position = e.position;
		}

		/// <summary>The square under a screen point, if the pointer is over the board.</summary>
		private bool PointerSquare( Vector2 screenPoint, out Sq square )
		{
			square = default;
			var ray = _camera.ScreenPointToRay( screenPoint );

			// Tiles lie flat at y = 0, so the board is a plane rather than
			// geometry to raycast against -- the same trick HeroPinDragger uses,
			// and the reason a drop just past the last tile still resolves to a
			// square that the snap can pull back onto the map.
			var plane = new Plane( Vector3.up, Vector3.zero );
			if ( !plane.Raycast( ray, out float distance ) ) return false;

			var world = ray.GetPoint( distance );
			square = SagaBoardBridge.WorldToSquare( world.x, world.z );
			return _board.Board != null && _board.Board.Count > 0;
		}
	}
}
