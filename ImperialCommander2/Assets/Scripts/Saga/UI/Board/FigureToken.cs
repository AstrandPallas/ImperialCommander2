using System;
using System.Collections.Generic;
using DG.Tweening;
using Saga.Board;
using UnityEngine;

namespace Saga
{
	/// <summary>A figure on the board: where it stands, and how it moves when ordered.</summary>
	public class FigureToken : MonoBehaviour
	{
		public SpriteRenderer body;
		public SpriteRenderer ring;
		public TextMesh label;

		/// <summary>The card's own portrait, the same one shown in the strip.</summary>
		public SpriteRenderer portrait;

		/// <summary>Diameter the portrait is fitted to, in squares.</summary>
		public float portraitSize = 0.78f;

		/// <summary>Height above the tiles, which sit at y = 0.</summary>
		public float hover = 0.12f;

		/// <summary>Seconds per square of ordinary movement.</summary>
		public float secondsPerSquare = 0.32f;

		public string FigureId { get; private set; }
		public Sq Square { get; private set; }

		/// <summary>How many squares the base covers; the token is drawn to match.</summary>
		public Footprint Footprint { get; private set; } = Footprint.Small1x1;

		private Sequence _move;

		public void Init( string figureId, Sq square, Color colour, string caption,
			Sprite face = null, Footprint footprint = Footprint.Small1x1 )
		{
			FigureId = figureId;
			Footprint = footprint;

			// A 2x2 base drawn as a 1x1 disc hides exactly the thing that
			// matters about it -- which squares it is standing on. The token
			// is scaled to the base and centred on it.
			var (w, h) = SagaBoardBridge.FootprintSpan( footprint );
			transform.localScale = Vector3.one * Mathf.Min( w, h );
			if ( body != null ) body.color = colour;
			if ( ring != null ) ring.color = colour;

			// A token should be recognisable as the figure it stands for, the
			// same way the strip on the left is. The lettered disc is only the
			// fallback for a card with no portrait.
			bool hasFace = face != null && portrait != null;
			if ( portrait != null )
			{
				portrait.sprite = face;
				portrait.enabled = hasFace;
				if ( hasFace )
				{
					// Fit whatever pixels-per-unit the sprite was imported with.
					float width = face.bounds.size.x;
					float scale = width > 0f ? portraitSize / width : 1f;
					portrait.transform.localScale = Vector3.one * scale;
				}
			}
			if ( body != null ) body.enabled = !hasFace;

			if ( label != null )
			{
				label.text = caption ?? "";
				// With a face the caption is the figure number in the group,
				// which still matters for matching to the minis, so it moves
				// to the corner rather than covering the portrait.
				label.transform.localPosition = hasFace
					? new Vector3( 0.30f, -0.30f, -0.02f )
					: new Vector3( 0f, 0f, -0.02f );
				label.transform.localScale = Vector3.one * (hasFace ? 0.055f : 0.08f);
			}
			Place( square );
		}

		/// <summary>Put the figure on a square with no animation.</summary>
		public void Place( Sq square )
		{
			Stop();
			Square = square;
			transform.position = Centre( square );
		}

		/// <summary>Where this token sits for a given anchor square.</summary>
		private Vector3 Centre( Sq anchor )
		{
			var w = SagaBoardBridge.FootprintCenter( anchor, Footprint, hover );
			return new Vector3( w.x, w.y, w.z );
		}

		/// <summary>
		/// Walk the figure along the squares the planner chose, one at a time.
		/// </summary>
		/// <remarks>
		/// Stepping square by square rather than gliding to the destination is
		/// the point of the animation: the player is copying the move onto a
		/// physical board and needs to see which spaces were used. Each step
		/// lasts in proportion to what it cost, so difficult ground reads as
		/// slower and the route explains itself.
		/// </remarks>
		public void SlideAlong( IList<Sq> path, BoardModel board, Action onArrived = null )
		{
			Stop();

			if ( path == null || path.Count < 2 )
			{
				if ( path != null && path.Count == 1 ) Place( path[0] );
				onArrived?.Invoke();
				return;
			}

			_move = DOTween.Sequence();
			for ( int i = 1; i < path.Count; i++ )
			{
				var step = path[i];
				if ( step == path[i - 1] )
				{
					// A repeated square is a large figure rotating in place.
					_move.Append( transform.DORotate( new Vector3( 0, 90f, 0 ), secondsPerSquare )
						.SetRelative( true ) );
					continue;
				}

				int cost = board != null ? board.EnterCost( step ) : 1;
				_move.Append( transform
					.DOMove( Centre( step ), secondsPerSquare * cost )
					.SetEase( Ease.InOutSine ) );
				_move.AppendCallback( () => Square = step );
			}

			_move.OnComplete( () =>
			{
				Square = path[path.Count - 1];
				onArrived?.Invoke();
			} );
		}

		/// <summary>Draw attention to this figure before it acts.</summary>
		public void Highlight( bool on )
		{
			if ( ring == null ) return;
			ring.DOKill();
			if ( on )
				ring.DOFade( 0.35f, 0.45f ).SetLoops( -1, LoopType.Yoyo );
			else
			{
				var c = ring.color;
				ring.color = new Color( c.r, c.g, c.b, 1f );
			}
		}

		public void Stop()
		{
			if ( _move == null ) return;
			_move.Kill();
			_move = null;
		}

		private void OnDestroy()
		{
			Stop();
			if ( ring != null ) ring.DOKill();
		}
	}
}
