using System;
using System.Collections.Generic;
using System.Linq;
using Saga.Board;
using Saga.Tracking;
using UnityEngine;

namespace Saga
{
	/// <summary>Builds the board for a mission and drives the figures on it.</summary>
	/// <remarks>
	/// Everything beneath this is tested headlessly; this is the layer that
	/// finds the scene's managers and calls it in the right order. Keeping it
	/// thin is deliberate, because it is the part that cannot be tested without
	/// running the game.
	/// </remarks>
	public class SagaBoardController : MonoBehaviour
	{
		public TileManager tileManager;
		public MapEntityManager mapEntityManager;
		public FigureLayer figureLayer;

		/// <summary>Null until a mission has been set up.</summary>
		public BoardModel Board => SagaBoardBridge.Current;

		public bool IsReady => Board != null && Board.Count > 0;

		private readonly List<GroupCombatState> _groups = new List<GroupCombatState>();
		private readonly List<HeroCombatState> _heroes = new List<HeroCombatState>();

		public IReadOnlyList<GroupCombatState> Groups => _groups;
		public IReadOnlyList<HeroCombatState> Heroes => _heroes;

		private void Awake()
		{
			if ( tileManager == null ) tileManager = FindObjectOfType<TileManager>();
			if ( mapEntityManager == null ) mapEntityManager = FindObjectOfType<MapEntityManager>();
			if ( figureLayer == null ) figureLayer = FindObjectOfType<FigureLayer>();
		}

		/// <summary>
		/// Build the board from the mission that is already loaded.
		/// </summary>
		/// <param name="isSectionActive">
		/// Hidden map sections must not be pathable, or the AI routes figures
		/// through rooms the players cannot see. Sections also reuse squares,
		/// so unioning them all invents collisions that never occur in play.
		/// </param>
		public BoardModel BuildBoard( Func<string, bool> isSectionActive = null )
		{
			if ( tileManager == null )
			{
				Utils.LogWarning( "SagaBoardController::no TileManager, cannot build a board" );
				return null;
			}

			var tiles = tileManager.CollectBoardTiles();
			var doors = mapEntityManager != null
				? mapEntityManager.CollectBoardDoors()
				: new List<SagaBoardBridge.DoorInput>();

			var board = SagaBoardBridge.Rebuild(
				tiles, doors, LookupDimensions, TerrainLoader.Library,
				isSectionActive ?? (_ => true) );

			var build = SagaBoardBridge.LastBuild;
			if ( build != null && build.Warnings.Count > 0 )
			{
				foreach ( var w in build.Warnings.Take( 8 ) )
					Utils.LogWarning( "SagaBoardController::" + w );
			}

			Utils.LogWarning( $"SagaBoardController::board is {board?.Count ?? 0} squares from "
				+ $"{tiles.Count} tiles and {doors.Count} doors" );
			return board;
		}

		private static (int w, int h)? LookupDimensions( string expansion, string tileId )
		{
			var descriptors = TileDescriptor.LoadData();
			var match = descriptors?.FirstOrDefault(
				d => d.expansion == expansion && d.id.ToString() == tileId );
			return match == null ? ((int, int)?)null : (match.width, match.height);
		}

		/// <summary>
		/// Called once the mission's tiles and entities are in place.
		/// </summary>
		/// <remarks>
		/// Heroes come from the session's party. Their stats are not in
		/// heroes.json, so health falls back to the deployment card's value and
		/// endurance to a default until herostats.json exists; neither affects
		/// movement or line of sight, only how long a hero lasts.
		/// </remarks>
		public void OnMissionReady( IEnumerable<DeploymentCard> party,
			Func<string, bool> isSectionActive = null )
		{
			if ( BuildBoard( isSectionActive ) == null ) return;

			var heroes = new List<HeroCombatState>();
			foreach ( var card in party ?? Enumerable.Empty<DeploymentCard>() )
			{
				if ( card == null ) continue;
				heroes.Add( new HeroCombatState
				{
					CardId = card.id,
					Name = card.name,
					MaxHealth = card.health > 0 ? card.health : 10,
				} );
			}

			SeedHeroes( heroes );
		}

		/// <summary>Seat the party at the mission entrance and show them.</summary>
		public List<Sq> SeedHeroes( IEnumerable<HeroCombatState> heroes )
		{
			_heroes.Clear();
			_heroes.AddRange( heroes ?? Enumerable.Empty<HeroCombatState>() );
			if ( !IsReady || _heroes.Count == 0 ) return new List<Sq>();

			var highlights = mapEntityManager != null
				? mapEntityManager.CollectHighlights()
				: new List<(string, int, int)>();

			var starts = HeroPlacement.SuggestStarts( Board, highlights, _heroes.Count );
			if ( starts.Count == 0 )
			{
				// Two of the shipped missions carry no entrance. The players
				// place the party themselves rather than the app guessing.
				Utils.LogWarning( "SagaBoardController::no entrance highlight, "
					+ "the party must be placed by hand" );
				return starts;
			}

			for ( int i = 0; i < starts.Count && i < _heroes.Count; i++ )
				TrackerBridge.SetHeroPosition( _heroes[i], starts[i], 0 );

			RefreshTokens();
			return starts;
		}

		public void Track( GroupCombatState group )
		{
			if ( group == null || _groups.Any( g => g.InstanceId == group.InstanceId ) ) return;
			_groups.Add( group );
			RefreshTokens();
		}

		public void Forget( GroupCombatState group )
		{
			if ( group == null ) return;
			_groups.RemoveAll( g => g.InstanceId == group.InstanceId );
			RefreshTokens();
		}

		/// <summary>Make the tokens on screen match what is tracked.</summary>
		public void RefreshTokens()
		{
			if ( figureLayer == null ) return;
			figureLayer.Clear();

			foreach ( var hero in _heroes )
			{
				if ( !hero.InPlay || hero.PosC == null || hero.PosR == null ) continue;
				figureLayer.Spawn( hero.CardId, new Sq( hero.PosC.Value, hero.PosR.Value ),
					false, Initial( hero.Name ) );
			}

			foreach ( var group in _groups )
			{
				foreach ( var slot in group.Figures )
				{
					if ( !slot.Alive || !slot.HasPosition ) continue;
					figureLayer.Spawn(
						TrackerBridge.FigureId( group.InstanceId, slot.Index ),
						new Sq( slot.PosC.Value, slot.PosR.Value ),
						true, (slot.Index + 1).ToString() );
				}
			}
		}

		private static string Initial( string name )
			=> string.IsNullOrEmpty( name ) ? "?" : name.Substring( 0, 1 ).ToUpperInvariant();

		/// <summary>
		/// Work out what a group should do, show it moving, and record where it
		/// ended up.
		/// </summary>
		/// <remarks>
		/// The destinations are committed as soon as the plan is made, not when
		/// the animation finishes, so a player who skips the playback is not
		/// left with a tracker that disagrees with the orders on screen. If they
		/// move a figure somewhere else, the correction path overrides it.
		/// </remarks>
		public ActivationPlan PlanActivation( GroupCombatState group, int round,
			PlanOverride overrides = null, Action onPlayed = null )
		{
			if ( !IsReady || group == null ) return null;

			var snapshot = TrackerBridge.Snapshot( group, _heroes, _groups, round );
			foreach ( var gap in snapshot.Gaps )
				Utils.LogWarning( "SagaBoardController::" + gap );

			if ( !snapshot.CanPlan )
			{
				Utils.LogWarning( "SagaBoardController::nothing to plan: "
					+ snapshot.Enemies.Count + " figures, " + snapshot.Rebels.Count + " targets" );
				return null;
			}

			var plan = ActivationPlanner.Plan( Board, snapshot.Enemies, snapshot.Rebels,
				null, snapshot.Visibility, overrides );

			TrackerBridge.Commit( plan, group );
			figureLayer?.Play( plan, Board, () =>
			{
				RefreshTokens();
				onPlayed?.Invoke();
			} );
			return plan;
		}
	}
}
