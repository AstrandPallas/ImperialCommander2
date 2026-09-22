using System;
using System.Collections.Generic;
using Saga.Tracking;
using UnityEngine;
using UnityEngine.UI;

namespace Saga
{
	/// <summary>
	/// One figure's health, strain and conditions, for tapping a token or for
	/// entering the damage an attack just did.
	/// </summary>
	/// <remarks>
	/// Built from code like the tracker panel, so it needs no scene authoring.
	/// It exists because the panel is a list: finding Diala's row in it after
	/// every attack is the kind of small friction that stops a tracker being
	/// used at all. Tapping the figure that was hit, or having the card open
	/// itself the moment an attack is ordered, is the difference.
	///
	/// Prompts queue. Three troopers attacking three heroes produce three cards
	/// in turn, each dismissed with one tap, rather than one card the players
	/// have to remember to switch.
	/// </remarks>
	public class FigureCardPopup : MonoBehaviour
	{
		public SagaBoardController boardController;

		private readonly Queue<Action> _queued = new Queue<Action>();
		private GameObject _card;
		private Action _current;

		private void Awake()
		{
			if ( boardController == null ) boardController = FindObjectOfType<SagaBoardController>();
		}

		public bool IsOpen => _card != null;

		/// <summary>Show a hero. The prompt, if any, says why the card opened.</summary>
		public void Show( HeroCombatState hero, string prompt = null )
		{
			if ( hero == null ) return;
			Enqueue( () => BuildHeroCard( hero, prompt ) );
		}

		/// <summary>Show a group, with the engaged figure's damage.</summary>
		public void Show( GroupCombatState group, string prompt = null )
		{
			if ( group == null ) return;
			Enqueue( () => BuildGroupCard( group, prompt ) );
		}

		/// <summary>
		/// Ask for the damage every attack in a plan did, one target at a time.
		/// </summary>
		/// <remarks>
		/// The app cannot know what the dice said. What it can do is put the
		/// right hero's card in front of the players at the right moment with
		/// the attacker named, so the number goes in while everyone still
		/// remembers it.
		/// </remarks>
		public void PromptForAttacks( Board.ActivationPlan plan )
		{
			if ( plan == null || boardController == null ) return;
			foreach ( var fp in plan.Figures )
			{
				if ( !fp.WillAttack || fp.Target == null ) continue;
				var target = fp.Target;
				HeroCombatState hero = null;
				foreach ( var h in boardController.Heroes )
					if ( h.CardId == target.Id ) { hero = h; break; }
				if ( hero == null ) continue;

				string who = fp.Figure?.Name ?? "an enemy";
				string need = fp.Attack != null && fp.Attack.RequiredAccuracy > 0
					? " (needs accuracy " + fp.Attack.RequiredAccuracy + ")" : "";
				Show( hero, who + " attacks " + hero.Name + need + " -- enter the damage taken" );
			}
		}

		private void Enqueue( Action build )
		{
			_queued.Enqueue( build );
			if ( _card == null ) Next();
		}

		private void Next()
		{
			Close();
			if ( _queued.Count == 0 ) { _current = null; return; }
			_current = _queued.Dequeue();
			_current();
		}

		public void Close()
		{
			if ( _card != null ) Destroy( _card );
			_card = null;
		}

		private void BuildHeroCard( HeroCombatState hero, string prompt )
		{
			var list = Card( hero.Name, prompt, new Color( 0.35f, 0.55f, 0.9f ) );

			var hp = Label( list, HealthText( hero ), Color.white, 26 );
			var strain = Label( list, "strain " + hero.Strain + " / " + hero.Endurance,
				new Color( 0.75f, 0.85f, 1f ), 20 );

			var row = Row( list );
			Button( row, "-1", () => { Before( hero.Name + " damage -1" ); hero.Heal( 1 ); hp.text = HealthText( hero ); Redraw(); }, 70 );
			Button( row, "+1", () => { Before( hero.Name + " damage +1" ); hero.ApplyDamage( 1 ); hp.text = HealthText( hero ); Redraw(); }, 70, new Color( 0.6f, 0.25f, 0.2f ) );
			Button( row, "+2", () => { Before( hero.Name + " damage +2" ); hero.ApplyDamage( 2 ); hp.text = HealthText( hero ); Redraw(); }, 70, new Color( 0.6f, 0.25f, 0.2f ) );
			Button( row, "+3", () => { Before( hero.Name + " damage +3" ); hero.ApplyDamage( 3 ); hp.text = HealthText( hero ); Redraw(); }, 70, new Color( 0.6f, 0.25f, 0.2f ) );

			var row2 = Row( list );
			Label( row2, "strain", new Color( 0.75f, 0.85f, 1f ), 18, 90 );
			Button( row2, "-1", () => { Before( hero.Name + " strain -1" ); hero.Strain = Math.Max( 0, hero.Strain - 1 ); strain.text = "strain " + hero.Strain + " / " + hero.Endurance; }, 70 );
			Button( row2, "+1", () => { Before( hero.Name + " strain +1" ); hero.Strain = Math.Min( hero.Endurance, hero.Strain + 1 ); strain.text = "strain " + hero.Strain + " / " + hero.Endurance; }, 70 );

			Conditions( list, hero.Conditions, hero.Name );
			Footer( list );
		}

		private void BuildGroupCard( GroupCombatState group, string prompt )
		{
			var list = Card( group.CardName + (group.IsElite ? " (Elite)" : ""), prompt,
				new Color( 0.85f, 0.3f, 0.3f ) );

			var alive = Label( list, group.FiguresAlive + " of " + group.MaxFigures + " figures", Color.white, 22 );
			var dmg = Label( list, GroupText( group ), Color.white, 26 );

			var row = Row( list );
			Button( row, "-1", () => { Before( group.CardName + " damage -1" ); group.Heal( 1 ); dmg.text = GroupText( group ); alive.text = group.FiguresAlive + " of " + group.MaxFigures + " figures"; Redraw(); }, 70 );
			foreach ( int n in new[] { 1, 2, 3 } )
			{
				int amount = n;
				Button( row, "+" + n, () =>
				{
					Before( group.CardName + " damage +" + amount );
					group.ApplyDamage( amount );
					dmg.text = GroupText( group );
					alive.text = group.FiguresAlive + " of " + group.MaxFigures + " figures";
					Redraw();
				}, 70, new Color( 0.6f, 0.25f, 0.2f ) );
			}

			if ( group.MaxFigures > 1 )
			{
				var row2 = Row( list );
				Button( row2, "next figure takes fire", () =>
				{
					Before( group.CardName + " next figure" );
					group.SetEngaged( (group.EngagedFigureIndex + 1) % Math.Max( 1, group.MaxFigures ) );
					dmg.text = GroupText( group );
				}, 240 );
			}

			Conditions( list, group.Conditions, group.CardName );
			Footer( list );
		}

		private static string HealthText( HeroCombatState h )
			=> (h.IsWounded ? "WOUNDED  " : "") + "damage " + h.Damage + " / " + h.MaxHealth
			   + "   (" + h.RemainingHealth + " left)";

		private static string GroupText( GroupCombatState g )
			=> "figure " + (g.EngagedFigureIndex + 1) + ": damage " + g.CurrentFigureDamage
			   + " / " + g.PerFigureHealth;

		private void Before( string what ) => boardController?.Undo?.Record( what );

		private void Redraw()
		{
			boardController?.RefreshTokens();
			FindObjectOfType<TrackerPanel>()?.Refresh();
		}

		// ---- layout ----

		private GameObject Card( string title, string prompt, Color accent )
		{
			Close();
			var canvas = GetComponentInParent<Canvas>();
			_card = new GameObject( "FigureCard", typeof( RectTransform ) );
			_card.transform.SetParent( canvas != null ? canvas.transform : transform, false );
			_card.transform.SetAsLastSibling();

			var rect = _card.GetComponent<RectTransform>();
			rect.anchorMin = new Vector2( 0.5f, 0f );
			rect.anchorMax = new Vector2( 0.5f, 0f );
			rect.pivot = new Vector2( 0.5f, 0f );
			rect.anchoredPosition = new Vector2( 0f, 40f );
			rect.sizeDelta = new Vector2( 520f, 100f );

			_card.AddComponent<Image>().color = new Color( 0.06f, 0.07f, 0.09f, 0.96f );
			var layout = _card.AddComponent<VerticalLayoutGroup>();
			layout.padding = new RectOffset( 14, 14, 12, 12 );
			layout.spacing = 8;
			layout.childForceExpandHeight = false;
			layout.childControlHeight = true;
			layout.childControlWidth = true;
			var fitter = _card.AddComponent<ContentSizeFitter>();
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

			Label( _card, title, accent, 24 );
			if ( !string.IsNullOrEmpty( prompt ) )
				Label( _card, prompt, new Color( 1f, 0.85f, 0.4f ), 18 );
			return _card;
		}

		private void Conditions( GameObject list, HashSet<Condition> conditions, string who )
		{
			var row = Row( list );
			foreach ( Condition c in Enum.GetValues( typeof( Condition ) ) )
			{
				var value = c;
				bool on = conditions.Contains( value );
				Button( row, value.ToString(), () =>
				{
					Before( (conditions.Contains( value ) ? "remove " : "apply ") + value + " on " + who );
					if ( conditions.Contains( value ) ) conditions.Remove( value );
					else conditions.Add( value );
					Redraw();
					// Rebuild the same card so the chip colour follows the state.
					_current?.Invoke();
				}, 92, on ? new Color( 0.85f, 0.65f, 0.15f ) : new Color( 0.22f, 0.24f, 0.28f ) );
			}
		}

		private void Footer( GameObject list )
		{
			var row = Row( list );
			Button( row, _queued.Count > 0 ? "next (" + _queued.Count + " more)" : "done",
				Next, 180, new Color( 0.2f, 0.45f, 0.3f ) );
			if ( _queued.Count > 0 )
				Button( row, "skip all", () => { _queued.Clear(); Close(); }, 120 );
		}

		private static GameObject Row( GameObject parent )
		{
			var go = new GameObject( "Row", typeof( RectTransform ) );
			go.transform.SetParent( parent.transform, false );
			var layout = go.AddComponent<HorizontalLayoutGroup>();
			layout.spacing = 6;
			layout.childForceExpandWidth = false;
			layout.childControlWidth = true;
			layout.childControlHeight = true;
			go.AddComponent<LayoutElement>().minHeight = 40;
			return go;
		}

		private static Text Label( GameObject parent, string text, Color colour, int size, float width = 0f )
		{
			var go = new GameObject( "Label", typeof( RectTransform ) );
			go.transform.SetParent( parent.transform, false );
			var label = go.AddComponent<Text>();
			label.text = text;
			label.color = colour;
			label.fontSize = size;
			label.alignment = TextAnchor.MiddleLeft;
			label.font = Resources.GetBuiltinResource<Font>( "Arial.ttf" );
			var le = go.AddComponent<LayoutElement>();
			le.minHeight = size + 10;
			if ( width > 0 ) le.preferredWidth = width;
			return label;
		}

		private static void Button( GameObject parent, string caption, Action onClick,
			float width = 60, Color? colour = null )
		{
			var go = new GameObject( "Button", typeof( RectTransform ) );
			go.transform.SetParent( parent.transform, false );
			go.AddComponent<Image>().color = colour ?? new Color( 0.22f, 0.24f, 0.28f );
			go.AddComponent<Button>().onClick.AddListener( () => onClick() );
			go.AddComponent<LayoutElement>().preferredWidth = width;

			var t = new GameObject( "Text", typeof( RectTransform ) );
			t.transform.SetParent( go.transform, false );
			var text = t.AddComponent<Text>();
			text.text = caption;
			text.color = Color.white;
			text.fontSize = 18;
			text.alignment = TextAnchor.MiddleCenter;
			text.font = Resources.GetBuiltinResource<Font>( "Arial.ttf" );
			var r = t.GetComponent<RectTransform>();
			r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
			r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
		}
	}
}
