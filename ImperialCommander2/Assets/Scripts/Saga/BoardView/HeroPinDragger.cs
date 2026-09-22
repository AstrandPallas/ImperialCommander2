using System;
using System.Collections.Generic;
using System.Linq;
using Saga.Board;
using Saga.Tracking;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Saga
{
	/// <summary>Drag any figure's token to the square the players moved it to.</summary>
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

		/// <summary>Raised when an enemy figure is put down somewhere by hand.</summary>
		public event Action<GroupCombatState, int, Sq> FigureMoved;

		private FigureToken _dragging;
		private HeroCombatState _hero;
		private GroupCombatState _group;
		private int _figureIndex = -1;
		private Color _originalColour;
		private Sq _origin;
		private CameraController _camera;
		private bool _heldCamera;
		private Vector3 _downAt;

		/// <summary>Pointer travel below this is a tap, not a drag.</summary>
		public float tapPixels = 8f;

		private void Awake()
		{
			if ( boardController == null ) boardController = FindObjectOfType<SagaBoardController>();
			if ( boardCamera == null ) boardCamera = Camera.main;
			if ( _camera == null ) _camera = FindObjectOfType<CameraController>();
		}

		// The camera pans on left-drag with the same guard this does, so without
		// this the board slides under the token and the square you drop on is not
		// the one you aimed at. ToggleNavigation is the seam the game already uses
		// to hold the camera still during an interaction.
		private void HoldCamera( bool held )
		{
			if ( _camera == null || held == _heldCamera ) return;
			_heldCamera = held;
			_camera.ToggleNavigation( !held );
		}

		private void OnDisable() => HoldCamera( false );

		private void Update()
		{
			if ( boardController == null || !boardController.IsReady ) return;

			if ( Input.GetMouseButtonDown( 0 ) && !IsOverUI() ) { _downAt = Input.mousePosition; TryPickUp(); }
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

			foreach ( var hero in boardController.Heroes )
			{
				if ( hero.PosC == null || hero.PosR == null ) continue;
				if ( new Sq( hero.PosC.Value, hero.PosR.Value ) != square ) continue;

				var token = layer.Get( hero.CardId );
				if ( token == null ) continue;

				_hero = hero;
				PickUp( token, square );
				return;
			}

			// Enemy figures too. The planner puts them down, but the table is
			// the authority: a mission with no deployment point, a figure the
			// players moved by hand, or an order they disagreed with all end
			// with a token that has to go where the mini actually is.
			foreach ( var group in boardController.Groups )
			{
				var footprint = group.Profile.Footprint;
				foreach ( var slot in group.Figures )
				{
					if ( !slot.Alive || !slot.HasPosition ) continue;
					var anchor = new Sq( slot.PosC.Value, slot.PosR.Value );
					// Any square of a large base picks the figure up.
					if ( !HeroPlacement.Cells( anchor, footprint ).Contains( square ) ) continue;

					var token = layer.Get( TrackerBridge.FigureId( group.InstanceId, slot.Index ) );
					if ( token == null ) continue;

					_group = group;
					_figureIndex = slot.Index;
					PickUp( token, anchor );
					return;
				}
			}
		}

		private void PickUp( FigureToken token, Sq square )
		{
			_dragging = token;
			_origin = square;
			HoldCamera( true );
			if ( token.ring != null )
			{
				_originalColour = token.ring.color;
				token.ring.color = draggingColour;
			}
			token.Stop();
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
			var group = _group;
			int index = _figureIndex;
			_dragging = null;
			_hero = null;
			_group = null;
			_figureIndex = -1;
			HoldCamera( false );

			if ( token == null || (hero == null && group == null) ) return;
			if ( token.ring != null ) token.ring.color = _originalColour;

			// A tap on a token opens its card -- health, strain, conditions --
			// rather than moving it. Finding a figure's row in the tracker
			// list after every hit is the friction that stops a tracker being
			// used; tapping the figure that was hit is not.
			if ( Vector3.Distance( _downAt, Input.mousePosition ) < tapPixels )
			{
				token.Place( _origin );
				var card = boardController.FigureCard;
				if ( card != null )
				{
					if ( hero != null ) card.Show( hero );
					else card.Show( group );
				}
				return;
			}

			if ( !PointerSquare( out var wanted ) )
			{
				token.Place( _origin );
				return;
			}

			// Everything except the figure being carried counts as occupied.
			var footprint = group != null ? group.Profile.Footprint : Footprint.Small1x1;
			var occupied = HeroPlacement.OccupiedSquares(
				boardController.Heroes.Where( h => h != hero ), boardController.Groups );
			if ( group != null )
				foreach ( var cell in HeroPlacement.Cells( _origin, footprint ) ) occupied.Remove( cell );

			var landed = HeroPlacement.Snap( boardController.Board, wanted, occupied, 3, footprint );
			if ( landed == null )
			{
				// Nothing free within reach, so the figure goes back rather
				// than being dropped somewhere it cannot stand.
				token.Place( _origin );
				return;
			}

			// Recorded only once the drop is known to be legal, so an abandoned
			// drag leaves no step behind.
			if ( hero != null )
			{
				boardController.Undo?.Record( hero.Name + " moved to " + landed.Value );
				TrackerBridge.SetHeroPosition( hero, landed.Value, CurrentRound );
				token.Place( landed.Value );
				HeroMoved?.Invoke( hero, landed.Value );
			}
			else
			{
				boardController.Undo?.Record( group.CardName + " #" + (index + 1)
					+ " moved to " + landed.Value );
				TrackerBridge.SetFigurePosition( group, index, landed.Value );
				token.Place( landed.Value );
				FigureMoved?.Invoke( group, index, landed.Value );
			}
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
