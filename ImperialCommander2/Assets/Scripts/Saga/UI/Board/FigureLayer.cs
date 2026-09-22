using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Saga.Board;
using UnityEngine;

namespace Saga
{
	/// <summary>The figures drawn on the map, and the playback of an activation.</summary>
	public class FigureLayer : MonoBehaviour
	{
		/// <summary>Leave unassigned to load the generated prefab from Resources.</summary>
		public FigureToken tokenPrefab;

		/// <summary>Built by Assets/Editor/BoardSetup.cs.</summary>
		private const string DefaultPrefab = "BoardView/FigureToken";
		public Color imperialColour = new Color( 0.78f, 0.16f, 0.16f );
		public Color rebelColour = new Color( 0.20f, 0.45f, 0.82f );

		/// <summary>Seconds to hold on a figure before it moves, so the eye can find it.</summary>
		public float focusPause = 0.55f;

		private readonly Dictionary<string, FigureToken> _tokens =
			new Dictionary<string, FigureToken>();

		public IEnumerable<FigureToken> Tokens => _tokens.Values;

		/// <summary>True while an activation is being played back.</summary>
		/// <remarks>
		/// Anything that rebuilds the tokens while this is true destroys the
		/// figure mid-slide and respawns it at its destination -- which is
		/// exactly what a teleport looks like. Callers defer until it is false.
		/// </remarks>
		public bool IsPlaying { get; private set; }

		/// <summary>Ids of every token currently on the layer.</summary>
		public IEnumerable<string> Ids => _tokens.Keys;

		public FigureToken Get( string figureId )
			=> figureId != null && _tokens.TryGetValue( figureId, out var t ) ? t : null;

		/// <summary>
		/// Bring a token to the given state, reusing it if it already exists.
		/// </summary>
		/// <remarks>
		/// Spawn destroys and recreates, which is right for a new figure and
		/// wrong for one that is merely being refreshed: recreating it stops any
		/// slide it was in the middle of. A token already on its square is
		/// left exactly as it is.
		/// </remarks>
		public FigureToken Ensure( string figureId, Sq square, bool imperial, string caption,
			Sprite face = null, Footprint footprint = Footprint.Small1x1 )
		{
			var existing = Get( figureId );
			if ( existing == null )
				return Spawn( figureId, square, imperial, caption, face, footprint );

			existing.Init( figureId, square, imperial ? imperialColour : rebelColour, caption,
				face, footprint );
			return existing;
		}

		/// <summary>Remove every token whose id is not in the set.</summary>
		public void Prune( ICollection<string> keep )
		{
			foreach ( var id in _tokens.Keys.Where( k => !keep.Contains( k ) ).ToList() )
				Despawn( id );
		}

		public FigureToken Spawn( string figureId, Sq square, bool imperial, string caption,
			Sprite face = null, Footprint footprint = Footprint.Small1x1 )
		{
			Despawn( figureId );
			// Falling back to Resources means the layer works from code alone,
			// without anyone having to drag the prefab into an inspector slot.
			if ( tokenPrefab == null )
				tokenPrefab = Resources.Load<FigureToken>( DefaultPrefab );

			if ( tokenPrefab == null )
			{
				Utils.LogWarning( "FigureLayer::no token prefab, and " + DefaultPrefab
					+ " is missing from Resources" );
				return null;
			}

			var token = Instantiate( tokenPrefab, transform );
			token.name = "Figure_" + figureId;
			token.Init( figureId, square, imperial ? imperialColour : rebelColour, caption, face, footprint );
			_tokens[figureId] = token;
			return token;
		}

		public void Despawn( string figureId )
		{
			if ( figureId == null || !_tokens.TryGetValue( figureId, out var token ) ) return;
			_tokens.Remove( figureId );
			if ( token != null ) Destroy( token.gameObject );
		}

		public void Clear()
		{
			foreach ( var token in _tokens.Values.Where( t => t != null ) )
				Destroy( token.gameObject );
			_tokens.Clear();
		}

		/// <summary>Move a figure without animating, for a correction at the table.</summary>
		public void Place( string figureId, Sq square ) => Get( figureId )?.Place( square );

		/// <summary>
		/// Play an activation: each figure in turn is highlighted, walks its
		/// route, and hands over to the next.
		/// </summary>
		/// <remarks>
		/// The order is the planner's own, which matters. It commits each
		/// figure's destination before planning the next, so replaying in any
		/// other order can show a figure walking through a square that is about
		/// to be occupied.
		/// </remarks>
		public Coroutine Play( ActivationPlan plan, BoardModel board, Action onFinished = null )
		{
			StopAllCoroutines();
			IsPlaying = true;
			return StartCoroutine( PlayRoutine( plan, board, () =>
			{
				IsPlaying = false;
				onFinished?.Invoke();
			} ) );
		}

		private IEnumerator PlayRoutine( ActivationPlan plan, BoardModel board, Action onFinished )
		{
			if ( plan != null )
			{
				foreach ( var fp in plan.Figures )
				{
					var token = Get( fp.Figure?.Id );
					if ( token == null ) continue;

					token.Highlight( true );
					yield return new WaitForSeconds( focusPause );

					if ( fp.Moved && fp.Path != null && fp.Path.Count > 1 )
					{
						bool arrived = false;
						token.SlideAlong( fp.Path, board, () => arrived = true );
						while ( !arrived ) yield return null;
					}
					else
					{
						token.Place( fp.End );
					}

					token.Highlight( false );
				}
			}
			onFinished?.Invoke();
		}

		/// <summary>Stop playback and leave every figure on its ordered square.</summary>
		public void SkipToEnd( ActivationPlan plan )
		{
			StopAllCoroutines();
			IsPlaying = false;
			if ( plan == null ) return;
			foreach ( var fp in plan.Figures )
			{
				var token = Get( fp.Figure?.Id );
				if ( token == null ) continue;
				token.Highlight( false );
				token.Place( fp.End );
			}
		}
	}
}
