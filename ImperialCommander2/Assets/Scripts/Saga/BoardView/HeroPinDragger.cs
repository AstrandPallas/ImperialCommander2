using System;
using System.Collections.Generic;
using System.Linq;
using Saga.Board;
using Saga.Tracking;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Saga
{
	/// <summary>Drag a hero's token to the square the players moved it to.</summary>
	/// <remarks>
	/// This is the one input the players give every round, so it is a drag
	/// rather than a form: pick the figure up, put it down, done. The square is
	/// worked out from where the pointer meets the board plane, then snapped,
	/// so a finger landing between squares or on a wall still lands somewhere
	/// sensible instead of being rejected.
	/// </remarks>
	public class HeroPinDragger : MonoBehaviour
	{
		public SagaBoardController boardController;
		public Camera boardCamera;

		/// <summary>Lifted height while dragging, so the token reads as picked up.</summary>
		public float liftHeight = 0.45f;

		public Color draggingColour = new Color( 1f, 0.72f, 0.15f );

		/// <summary>Round number stamped on a position when it is set.</summary>
		public int CurrentRound { get; set; }

		public event Action<HeroCombatState, Sq> HeroMoved;

		private FigureToken _dragging;
		private HeroCombatState _hero;
		private Color _originalColour;
		private Sq _origin;

		private void Awake()
		{
			if ( boardController == null ) boardController = FindObjectOfType<SagaBoardController>();
			if ( boardCamera == null ) boardCamera = Camera.main;
		}

		private void Update()
		{
			if ( boardController == null || !boardController.IsReady ) return;

			if ( Input.GetMouseButtonDown( 0 ) && !IsOverUI() ) TryPickUp();
			else if ( _dragging != null && Input.GetMouseButton( 0 ) ) DragTo();
			else if ( _dragging != null && Input.GetMouseButtonUp( 0 ) ) Drop();
		}

		/// <summary>A click on a panel must not reach through to the board.</summary>
		private static bool IsOverUI()
			=> EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

		private void TryPickUp()
		{
			if ( !PointerSquare( out var square ) ) return;

			var layer = boardController.figureLayer;
			if ( layer == null ) return;

			// Only heroes are dragged. Enemy positions come from the planner,
			// and a correction to one of those goes through the override path
			// so the rest of the group re-plans around it.
			foreach ( var hero in boardController.Heroes )
			{
				if ( hero.PosC == null || hero.PosR == null ) continue;
				if ( new Sq( hero.PosC.Value, hero.PosR.Value ) != square ) continue;

				var token = layer.Get( hero.CardId );
				if ( token == null ) continue;

				_hero = hero;
				_dragging = token;
				_origin = square;
				if ( token.body != null )
				{
					_originalColour = token.body.color;
					token.body.color = draggingColour;
				}
				token.Stop();
				return;
			}
		}

		private void DragTo()
		{
			if ( !PointerWorld( out var world ) ) return;
			_dragging.transform.position =
				new Vector3( world.x, _dragging.hover + liftHeight, world.z );
		}

		private void Drop()
		{
			var token = _dragging;
			var hero = _hero;
			_dragging = null;
			_hero = null;

			if ( token == null || hero == null ) return;
			if ( token.body != null ) token.body.color = _originalColour;

			if ( !PointerSquare( out var wanted ) )
			{
				token.Place( _origin );
				return;
			}

			var occupied = HeroPlacement.OccupiedSquares(
				boardController.Heroes.Where( h => h != hero ), boardController.Groups );

			var landed = HeroPlacement.Snap( boardController.Board, wanted, occupied );
			if ( landed == null )
			{
				// Nothing free within reach, so the figure goes back rather
				// than being dropped somewhere it cannot stand.
				token.Place( _origin );
				return;
			}

			TrackerBridge.SetHeroPosition( hero, landed.Value, CurrentRound );
			token.Place( landed.Value );
			HeroMoved?.Invoke( hero, landed.Value );
		}

		/// <summary>Where the pointer meets the board plane.</summary>
		/// <remarks>
		/// Tiles lie flat at y = 0, so the board is a plane rather than
		/// geometry to raycast against. That also means a drop works over empty
		/// space beyond the tiles, which the snap then pulls back onto the map.
		/// </remarks>
		private bool PointerWorld( out Vector3 world )
		{
			world = Vector3.zero;
			if ( boardCamera == null ) return false;

			var ray = boardCamera.ScreenPointToRay( Input.mousePosition );
			var plane = new Plane( Vector3.up, Vector3.zero );
			if ( !plane.Raycast( ray, out float distance ) ) return false;

			world = ray.GetPoint( distance );
			return true;
		}

		private bool PointerSquare( out Sq square )
		{
			square = default;
			if ( !PointerWorld( out var world ) ) return false;
			square = SagaBoardBridge.WorldToSquare( world.x, world.z );
			return true;
		}
	}
}
