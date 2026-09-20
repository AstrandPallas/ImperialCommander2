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

		/// <summary>Height above the tiles, which sit at y = 0.</summary>
		public float hover = 0.12f;

		/// <summary>Seconds per square of ordinary movement.</summary>
		public float secondsPerSquare = 0.32f;

		public string FigureId { get; private set; }
		public Sq Square { get; private set; }

		private Sequence _move;

		public void Init( string figureId, Sq square, Color colour, string caption )
		{
			FigureId = figureId;
			if ( body != null ) body.color = colour;
			if ( ring != null ) ring.color = colour;
			if ( label != null ) label.text = caption;
			Place( square );
		}

		/// <summary>Put the figure on a square with no animation.</summary>
		public void Place( Sq square )
		{
			Stop();
			Square = square;
			var w = SagaBoardBridge.SquareToWorld( square, hover );
			transform.position = new Vector3( w.x, w.y, w.z );
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
				var w = SagaBoardBridge.SquareToWorld( step, hover );
				_move.Append( transform
					.DOMove( new Vector3( w.x, w.y, w.z ), secondsPerSquare * cost )
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
