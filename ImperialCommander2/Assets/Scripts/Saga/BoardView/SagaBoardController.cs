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

		/// <summary>Where the Strict/Classic choice is remembered.</summary>
		public const string AiModePref = "Saga.BoardAiMode";

		/// <summary>
		/// Switch between board-aware orders and upstream's abstract text.
		/// </summary>
		/// <remarks>
		/// Kept permanently rather than as a migration aid. A named square
		/// derived from terrain read off a picture can be specifically wrong in
		/// a way "Move 4 to attack Gaarkhan" never is, so there has to be a way
		/// back that costs a sentence of vagueness rather than the evening.
		/// </remarks>
		public void SetAiMode( AiMode mode )
		{
			BoardAiSettings.Mode = mode;
			if ( mode == AiMode.StrictRules ) BoardAiSettings.UseStrictRules();
			PlayerPrefs.SetInt( AiModePref, mode == AiMode.Classic ? 1 : 0 );
			PlayerPrefs.Save();
			Utils.LogWarning( "SagaBoardController::AI mode is now " + mode );
		}

		/// <summary>Null until a mission has been set up.</summary>
		public BoardModel Board => SagaBoardBridge.Current;

		public bool IsReady => Board != null && Board.Count > 0;

		/// <summary>
		/// All tracked state. Owning a TrackerManager rather than loose lists
		/// is what lets undo and the save file work on the same objects the
		/// board is drawn from -- a restore replaces the contents of these
		/// lists in place, so nothing holds a stale reference afterwards.
		/// </summary>
		public readonly TrackerManager Tracker = new TrackerManager();

		/// <summary>Takes back a mis-tap on the tracker panel.</summary>
		public UndoStack Undo { get; private set; }

		private List<GroupCombatState> _groups => Tracker.Groups;
		private List<HeroCombatState> _heroes => Tracker.Heroes;

		public IReadOnlyList<GroupCombatState> Groups => _groups;
		public IReadOnlyList<HeroCombatState> Heroes => _heroes;

		private void Awake()
		{
			if ( tileManager == null ) tileManager = FindObjectOfType<TileManager>();
			if ( mapEntityManager == null ) mapEntityManager = FindObjectOfType<MapEntityManager>();
			if ( figureLayer == null ) figureLayer = FindObjectOfType<FigureLayer>();
			if ( Undo == null ) Undo = new UndoStack( Tracker );

			// The toggle is the players', so it outlives a session.
			BoardAiSettings.Mode = PlayerPrefs.GetInt( AiModePref, 0 ) == 1
				? AiMode.Classic : AiMode.StrictRules;
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

			RefreshObjectives();

			Utils.LogWarning( $"SagaBoardController::board is {board?.Count ?? 0} squares from "
				+ $"{tiles.Count} tiles and {doors.Count} doors, "
				+ $"{Tracker.Tokens.Count} objective token(s)" );
			return board;
		}

		/// <summary>
		/// Take the mission's crates, terminals and tokens into the tracker.
		/// </summary>
		/// <remarks>
		/// Without this the objective map is always empty and the planner's
		/// positioning preference never fires -- the feature would be live in
		/// the tests and dead in the game, which is the failure this project
		/// has hit before by checking that code exists rather than that
		/// something calls it.
		///
		/// Re-read rather than merged, because a mission may reveal or remove
		/// tokens as it runs. State the players have already recorded against
		/// a token is preserved by guid.
		/// </remarks>
		public void RefreshObjectives()
		{
			if ( mapEntityManager == null ) return;

			var previous = Tracker.Tokens
				.Where( t => !string.IsNullOrEmpty( t.EntityGuid ) )
				.GroupBy( t => t.EntityGuid )
				.ToDictionary( g => g.Key, g => g.First() );

			Tracker.Tokens.Clear();
			foreach ( var (name, kind, guid, c, r) in mapEntityManager.CollectObjectiveTokens() )
			{
				previous.TryGetValue( guid, out var was );
				Tracker.Tokens.Add( new MissionTokenState
				{
					EntityGuid = guid,
					Name = name,
					Kind = Enum.TryParse( kind, true, out TokenKind k ) ? k : TokenKind.Other,
					State = was?.State ?? "unopened",
					Counter = was?.Counter ?? 0,
					ClaimedBy = was?.ClaimedBy,
					PosC = c,
					PosR = r,
				} );
			}
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
		/// Heroes come from the session's party, and their sheets from
		/// herostats.json, because heroes.json carries no combat numbers. This
		/// is not only cosmetic: the Imperial priority chain ranks Rebels by
		/// health remaining and by total health, so tracking every hero on the
		/// same numbers changes which one the AI attacks.
		/// </remarks>
		public void OnMissionReady( IEnumerable<DeploymentCard> party,
			Func<string, bool> isSectionActive = null,
			IEnumerable<CampaignHero> campaignHeroes = null )
		{
			if ( BuildBoard( isSectionActive ) == null ) return;

			var sheets = HeroStatsLoader.Stats;
			var carry = CarryOverFrom( campaignHeroes );
			var heroes = new List<HeroCombatState>();
			foreach ( var card in party ?? Enumerable.Empty<DeploymentCard>() )
			{
				if ( card == null ) continue;
				var hero = new HeroCombatState { CardId = card.id, Name = card.name };

				// The printed sheet wins where we have it. The deployment card's
				// health is the ALLY version of that character and does not
				// match the hero sheet, so it is only a last resort.
				if ( sheets.Knows( card.id ) )
					carry.Seat( hero, sheets );
				else
				{
					hero.MaxHealth = card.health > 0 ? card.health : 10;
					carry.For( card.id ).ApplyTo( hero );
					Utils.LogWarning( "SagaBoardController::no hero sheet for " + card.name
						+ " (" + card.id + "), tracking it on defaults" );
				}

				var gains = carry.For( card.id );
				if ( !gains.IsEmpty )
					Utils.LogWarning( "SagaBoardController::" + card.name
						+ " carries campaign gains, " + gains );

				heroes.Add( hero );
			}

			SeedHeroes( heroes );
		}

		/// <summary>
		/// The party's campaign gains, taken from the campaign record.
		/// </summary>
		/// <remarks>
		/// A campaign this fork has never touched has these at zero, which
		/// means the printed sheet, so an existing save loads as a party with
		/// no gains rather than as a party with no health.
		/// </remarks>
		private static CampaignCarryOver CarryOverFrom( IEnumerable<CampaignHero> campaignHeroes )
		{
			var carry = new CampaignCarryOver();
			foreach ( var ch in campaignHeroes ?? Enumerable.Empty<CampaignHero>() )
			{
				if ( ch == null || string.IsNullOrEmpty( ch.heroID ) ) continue;
				carry.Set( new CampaignAdjustment
				{
					CardId = ch.heroID,
					BonusHealth = ch.bonusHealth,
					BonusEndurance = ch.bonusEndurance,
					BonusSpeed = ch.bonusSpeed,
					Reason = ch.bonusReason ?? "",
				} );
			}
			return carry;
		}

		/// <summary>Seat the party at the mission entrance and show them.</summary>
		public List<Sq> SeedHeroes( IEnumerable<HeroCombatState> heroes )
		{
			_heroes.Clear();
			_heroes.AddRange( heroes ?? Enumerable.Empty<HeroCombatState>() );
			if ( !IsReady || _heroes.Count == 0 ) return new List<Sq>();

			var highlights = mapEntityManager != null
				? mapEntityManager.CollectHighlights()
				: new List<(string Name, int C, int R)>();

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

		/// <summary>
		/// Start tracking a deployed group, with no positions yet.
		/// </summary>
		/// <remarks>
		/// Deployment happens in two steps upstream: the card is added to the
		/// game, and then -- separately, and sometimes only after a text box --
		/// a deployment point is chosen and shown to the players. The tracker
		/// mirrors that. Registering here means the group exists for undo and
		/// the panel from the moment the card does; where it stands is decided
		/// by PlaceGroupAt, from the SAME point the players are told to use.
		/// </remarks>
		public GroupCombatState Register( DeploymentCard card )
		{
			if ( card == null ) return null;
			var existing = FindGroup( card );
			if ( existing != null ) return existing;

			int figures = card.currentSize > 0 ? card.currentSize : Math.Max( 1, card.size );
			var group = GroupCombatState.Create(
				card.id + ":" + Guid.NewGuid().ToString( "N" ).Substring( 0, 6 ),
				card.id, card.name, figures,
				card.health > 0 ? card.health : 3, card.isElite );

			// The board engine is only as right as the card it is given. Speed,
			// attack type and footprint all reach the planner from here, and
			// until they did every figure was planned as a ranged 1x1 moving 4.
			group.Profile = UnitProfile.From(
				card.attackType.ToString(), card.miniSize.ToString(), card.speed, card.keywords );

			Track( group );
			return group;
		}

		/// <summary>The live group for a card, if it is on the board.</summary>
		private GroupCombatState FindGroup( DeploymentCard card )
			=> card == null ? null
				: _groups.FirstOrDefault( g => g.CardId == card.id && !g.IsDefeated );

		/// <summary>
		/// Put a group's figures on the board at a deployment point.
		/// </summary>
		/// <param name="points">
		/// The deployment squares the mission allows for this group -- the
		/// same ones the players are shown. Several means the mission spreads
		/// the choice, so the group takes the least crowded of them.
		/// </param>
		/// <remarks>
		/// This is called from the moment upstream resolves WHICH point it is
		/// about to highlight, so the tokens are already standing there while
		/// the text box says "deploy here". Using a point of our own would put
		/// the token somewhere the players were not told about, which is worse
		/// than no token at all.
		/// </remarks>
		public GroupCombatState PlaceGroupAt( DeploymentCard card,
			IEnumerable<(string Name, Sq Square)> points )
		{
			var group = Register( card );
			if ( group == null || !IsReady ) return group;

			var candidates = (points ?? Enumerable.Empty<(string, Sq)>())
				.Where( p => Board.Exists( p.Square ) )
				.ToList();
			if ( candidates.Count == 0 )
			{
				Utils.LogWarning( "SagaBoardController::" + card.name
					+ " has no deployment point on the board, so it has no position yet" );
				return group;
			}

			var occupied = HeroPlacement.OccupiedSquares( _heroes, _groups );

			// Spread the arrivals: a point already crowded by the last group
			// is the worst place to put the next one. Deterministic on ties.
			var chosen = candidates
				.OrderBy( p => occupied.Count( o => Sq.Chebyshev( o, p.Square ) <= 2 ) )
				.ThenBy( p => p.Square.C ).ThenBy( p => p.Square.R )
				.First();

			int figures = group.MaxFigures;
			var squares = DeploymentPlanner.PlaceGroup( Board, chosen.Square, figures,
				occupied, _heroes, group.Profile.Footprint );

			for ( int i = 0; i < squares.Count && i < figures; i++ )
				TrackerBridge.SetFigurePosition( group, i, squares[i] );

			Utils.LogWarning( "SagaBoardController::" + card.name + " deploys at "
				+ chosen.Name + " " + chosen.Square
				+ (candidates.Count > 1 ? " (chosen from " + candidates.Count + ")" : "")
				+ ", " + squares.Count + " of " + figures + " figure(s) seated" );

			RefreshTokens();
			return group;
		}

		/// <summary>Serialised tracker state, in the shape the other managers use.</summary>
		public string GetState()
			=> Newtonsoft.Json.JsonConvert.SerializeObject(
				Tracker.Capture(), Newtonsoft.Json.Formatting.Indented );

		/// <summary>
		/// Rebuild the board and put the tracked figures back on it.
		/// </summary>
		/// <remarks>
		/// The board itself is derived from the tiles and is rebuilt rather
		/// than saved, so only the things that cannot be derived -- who is
		/// where, how hurt they are, what conditions they carry -- come out of
		/// the file.
		///
		/// A null state is a session saved before tracking existed. Those load
		/// as a fresh board with the party seated at the entrance, which is
		/// wrong about where the party currently stands but is correctable by
		/// dragging, and is a great deal better than refusing to load.
		/// </remarks>
		public void RestoreTrackerState( TrackerStateData state,
			Func<string, bool> isSectionActive = null )
		{
			if ( BuildBoard( isSectionActive ) == null ) return;

			if ( state == null )
			{
				Utils.LogWarning( "SagaBoardController::this session predates figure "
					+ "tracking, so the board starts empty and positions must be set by hand" );
				RefreshTokens();
				return;
			}

			Tracker.Restore( state );
			Undo?.Clear();
			RefreshTokens();
			Utils.LogWarning( "SagaBoardController::restored " + Tracker.Groups.Count
				+ " group(s) and " + Tracker.Heroes.Count + " hero/heroes from the save" );
		}

		/// <summary>
		/// The landmarks a plan can be described against: the mission's own
		/// tokens, which are the things physically on the table.
		/// </summary>
		public List<Landmark> Landmarks()
		{
			var marks = new List<Landmark>();
			foreach ( var t in Tracker.Tokens )
			{
				if ( t == null || t.PosC == null || t.PosR == null ) continue;
				marks.Add( new Landmark(
					string.IsNullOrEmpty( t.Name ) ? t.Kind.ToString().ToLowerInvariant() : t.Name,
					new Sq( t.PosC.Value, t.PosR.Value ) ) );
			}
			return marks;
		}

		/// <summary>
		/// The last orders, said in terms of things visible on the table.
		/// </summary>
		/// <remarks>
		/// The sliding token is the real instruction; this is the caption, and
		/// the fallback when the animation cannot speak -- a dispute, a stale
		/// position, or somebody who looked away.
		/// </remarks>
		public List<string> NarrateLastPlan()
			=> PlanNarrator.Narrate( LastPlan, Landmarks(),
				SagaBoardBridge.LastBuild?.TileOf );

		/// <summary>The last plan given, for a dispute raised about it.</summary>
		public ActivationPlan LastPlan { get; private set; }

		private BoardSnapshot _lastSnapshot;

		/// <summary>
		/// Write out everything needed to argue about the last order.
		/// </summary>
		/// <remarks>
		/// Simulation cannot see the cardboard, so terrain read wrong off a
		/// picture stays wrong until somebody at the table notices -- once,
		/// mid-mission, while trying to get on with the game. One tap has to
		/// capture the whole thing or it will not be captured at all.
		/// </remarks>
		public string CaptureDispute( string reason, string folder = null )
		{
			if ( Board == null ) return null;

			var snap = DisputeSnapshot.Capture(
				Board, LastPlan,
				_lastSnapshot?.Enemies, _lastSnapshot?.Rebels,
				DataStore.mission?.missionProperties?.missionID,
				Tracker.Round, reason );

			try
			{
				string dir = folder ?? System.IO.Path.Combine(
					Application.persistentDataPath, "Disputes" );
				System.IO.Directory.CreateDirectory( dir );
				string file = System.IO.Path.Combine( dir,
					"dispute-" + DateTime.UtcNow.ToString( "yyyyMMdd-HHmmss" ) + ".json" );

				var json = Newtonsoft.Json.JsonConvert.SerializeObject(
					snap, Newtonsoft.Json.Formatting.Indented );
				System.IO.File.WriteAllText( file, json );

				Utils.LogWarning( "SagaBoardController::dispute written to " + file );
				Utils.LogWarning( snap.Describe() );
				return file;
			}
			catch ( Exception e )
			{
				// Losing the report the players stopped the game to make is
				// worse than any write error, so the reasoning still reaches
				// the log even when the file cannot be created.
				Utils.LogWarning( "SagaBoardController::could not write the dispute file ("
					+ e.Message + "), so it goes to the log instead" );
				Utils.LogWarning( snap.Describe() );
				return null;
			}
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
				var face = FaceFor( hero.CardId );
				figureLayer.Spawn( hero.CardId, new Sq( hero.PosC.Value, hero.PosR.Value ),
					false, face != null ? "" : Initial( hero.Name ), face );
			}

			foreach ( var group in _groups )
			{
				var face = FaceFor( group.CardId );
				foreach ( var slot in group.Figures )
				{
					if ( !slot.Alive || !slot.HasPosition ) continue;
					figureLayer.Spawn(
						TrackerBridge.FigureId( group.InstanceId, slot.Index ),
						new Sq( slot.PosC.Value, slot.PosR.Value ),
						true,
						// The number still matters for a group: it is how a
						// token maps onto one of three identical minis.
						group.MaxFigures > 1 ? (slot.Index + 1).ToString() : "",
						face, group.Profile.Footprint );
				}
			}
		}

		private static string Initial( string name )
			=> string.IsNullOrEmpty( name ) ? "?" : name.Substring( 0, 1 ).ToUpperInvariant();

		private static readonly Dictionary<string, Sprite> _faces = new Dictionary<string, Sprite>();

		/// <summary>
		/// The card's portrait -- the same image the strip on the left uses.
		/// </summary>
		/// <remarks>
		/// A token that looks like the figure it stands for needs no legend.
		/// The path is the one DataStore derives for every card, so the token
		/// and the strip can never show different faces for the same card.
		/// </remarks>
		private static Sprite FaceFor( string cardId )
		{
			if ( string.IsNullOrEmpty( cardId ) ) return null;
			if ( _faces.TryGetValue( cardId, out var cached ) ) return cached;

			DeploymentCard card = null;
			foreach ( var list in new[]
			{
				DataStore.heroCards, DataStore.deploymentCards,
				DataStore.villainCards, DataStore.allyCards,
			} )
			{
				card = list?.FirstOrDefault( c => c != null && c.id == cardId );
				if ( card != null ) break;
			}

			var sprite = card != null && !string.IsNullOrEmpty( card.mugShotPath )
				? Resources.Load<Sprite>( card.mugShotPath )
				: null;
			_faces[cardId] = sprite;
			return sprite;
		}

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

			// The way back. A named square derived from terrain read off a
			// picture can be confidently, specifically wrong, in a way
			// upstream's "Move 4 to attack Gaarkhan" never is -- so returning
			// null here hands the popup back to that text rather than arguing
			// with the table.
			if ( !BoardAiSettings.UseBoardAi )
			{
				Utils.LogWarning( "SagaBoardController::board AI is off"
					+ (string.IsNullOrEmpty( BoardAiSettings.ClassicReason )
						? "" : " -- " + BoardAiSettings.ClassicReason) );
				return null;
			}

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
				null, snapshot.Visibility, overrides,
				TrackerBridge.ObjectivesFrom( Tracker.Tokens ) );

			LastPlan = plan;
			_lastSnapshot = snapshot;
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
