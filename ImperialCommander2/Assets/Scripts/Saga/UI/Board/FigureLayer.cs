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

		public FigureToken Get( string figureId )
			=> figureId != null && _tokens.TryGetValue( figureId, out var t ) ? t : null;

		public FigureToken Spawn( string figureId, Sq square, bool imperial, string caption )
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
			token.Init( figureId, square, imperial ? imperialColour : rebelColour, caption );
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
			return StartCoroutine( PlayRoutine( plan, board, onFinished ) );
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
