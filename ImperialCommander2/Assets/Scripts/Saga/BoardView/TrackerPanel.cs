using System;
using System.Collections.Generic;
using System.Linq;
using Saga.Board;
using Saga.Tracking;
using UnityEngine;
using UnityEngine.UI;

namespace Saga
{
	/// <summary>Reads and edits the tracked state during a mission.</summary>
	/// <remarks>
	/// Built from code rather than a prefab so it needs no scene authoring and
	/// can be dropped onto any canvas. The layout is deliberately plain: this
	/// is read across a table at arm's length, between turns, so rows are tall
	/// and the controls are the ones that get used every round.
	/// </remarks>
	public class TrackerPanel : MonoBehaviour
	{
		public SagaBoardController boardController;

		public int Round { get; set; } = 1;

		private RectTransform _list;
		private readonly List<GameObject> _rows = new List<GameObject>();
		private bool _built;

		private void Awake()
		{
			if ( boardController == null ) boardController = FindObjectOfType<SagaBoardController>();
		}

		private void OnEnable() => Refresh();

		/// <summary>
		/// Record the state before an edit, so it can be taken back.
		/// </summary>
		/// <remarks>
		/// Every control on this panel goes through here. It is one call per
		/// control rather than a mechanism each one has to implement, which is
		/// why a control added later is undoable without anybody remembering
		/// to make it so.
		/// </remarks>
		private void Before( string what ) => boardController?.Undo?.Record( what );

		/// <summary>Rebuild the rows from what is currently tracked.</summary>
		public void Refresh()
		{
			if ( !_built ) Build();
			foreach ( var row in _rows ) Destroy( row );
			_rows.Clear();
			if ( boardController == null ) return;

			_rows.Add( HeaderRow() );

			foreach ( var hero in boardController.Heroes )
				_rows.Add( HeroRow( hero ) );

			foreach ( var group in boardController.Groups )
				_rows.Add( GroupRow( group ) );
		}

		/// <summary>The undo control, and what it would take back.</summary>
		private GameObject HeaderRow()
		{
			var row = Row();
			var undo = boardController?.Undo;
			string next = undo?.NextUndo;

			Label( row, "TRACKER", 120, new Color( 0.75f, 0.78f, 0.85f ) );

			// Naming the change is what makes the button safe to press at a
			// table: you can see what is about to come back before you do it.
			Label( row, next == null ? "nothing to undo" : "undo: " + next, 330,
				next == null ? new Color( 0.5f, 0.52f, 0.56f ) : Color.white );

			Button( row, "UNDO", () =>
			{
				if ( boardController?.Undo?.Undo() == null ) return;
				boardController.RefreshTokens();
				Refresh();
			}, 90, next == null
				? new Color( 0.18f, 0.19f, 0.22f ) : new Color( 0.55f, 0.35f, 0.12f ) );

			Button( row, "REDO", () =>
			{
				if ( boardController?.Undo?.Redo() == null ) return;
				boardController.RefreshTokens();
				Refresh();
			}, 90, undo != null && undo.CanRedo
				? new Color( 0.22f, 0.36f, 0.5f ) : new Color( 0.18f, 0.19f, 0.22f ) );

			return row;
		}

		private void Build()
		{
			_built = true;
			var panel = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
			panel.color = new Color( 0.08f, 0.09f, 0.11f, 0.92f );

			var layout = gameObject.GetComponent<VerticalLayoutGroup>()
				?? gameObject.AddComponent<VerticalLayoutGroup>();
			layout.padding = new RectOffset( 10, 10, 10, 10 );
			layout.spacing = 6;
			layout.childForceExpandHeight = false;
			layout.childControlHeight = true;
			layout.childControlWidth = true;

			var fitter = gameObject.GetComponent<ContentSizeFitter>()
				?? gameObject.AddComponent<ContentSizeFitter>();
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			// The widest row is a hero who has not been placed yet: name, damage,
			// strain, the staleness note and five condition chips. Fitting the
			// panel to that rather than to a chosen number is what stops the rows
			// being squeezed, because the columns carry no minimum width and so
			// compress silently instead of overflowing where it would be noticed.
			fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

			_list = GetComponent<RectTransform>();
		}

		private GameObject HeroRow( HeroCombatState hero )
		{
			var row = Row();
			var stale = Round - hero.PosRound;

			Label( row, hero.Name, 190, hero.IsWounded
				? new Color( 1f, 0.55f, 0.55f ) : Color.white );

			// Damage is the number that changes most, so it gets the widest
			// controls and sits first.
			var damage = Label( row, DamageText( hero ), 120, Color.white );
			Button( row, "-", () =>
			{
				Before( hero.Name + " damage -1" );
				hero.Heal( 1 );
				damage.text = DamageText( hero );
			} );
			Button( row, "+", () =>
			{
				Before( hero.Name + " damage +1" );
				hero.ApplyDamage( 1 );
				damage.text = DamageText( hero );
				Refresh();
			} );

			Label( row, "strain", 70, new Color( 0.7f, 0.8f, 1f ) );
			var strain = Label( row, hero.Strain + "/" + hero.Endurance, 70, Color.white );
			Button( row, "-", () =>
			{
				Before( hero.Name + " strain -1" );
				hero.Strain = Math.Max( 0, hero.Strain - 1 );
				strain.text = hero.Strain + "/" + hero.Endurance;
			} );
			Button( row, "+", () =>
			{
				Before( hero.Name + " strain +1" );
				hero.Strain = Math.Min( hero.Endurance, hero.Strain + 1 );
				strain.text = hero.Strain + "/" + hero.Endurance;
			} );

			// A position nobody has confirmed for two rounds is the most
			// likely cause of a wrong order, so it is said out loud.
			if ( hero.PosC == null )
				Label( row, "not placed", 120, new Color( 1f, 0.7f, 0.2f ) );
			else if ( stale >= TrackerBridge.StaleAfterRounds )
				Label( row, stale + " rounds old", 120, new Color( 1f, 0.7f, 0.2f ) );

			ConditionChips( row, hero.Conditions );
			return row;
		}

		private static string DamageText( HeroCombatState hero )
			=> (hero.IsWounded ? "WOUNDED " : "") + hero.Damage + "/" + hero.MaxHealth;

		private GameObject GroupRow( GroupCombatState group )
		{
			var row = Row();
			Label( row, group.CardName + (group.IsElite ? " [E]" : ""), 190,
				new Color( 1f, 0.6f, 0.6f ) );

			var alive = Label( row, group.FiguresAlive + "/" + group.MaxFigures + " alive",
				120, Color.white );

			// Count and current-figure damage are separate on purpose: one dial
			// for the figure taking fire, one pip count for the group.
			var damage = Label( row,
				"fig " + (group.EngagedFigureIndex + 1) + ": " + group.CurrentFigureDamage
				+ "/" + group.PerFigureHealth, 150, Color.white );

			Button( row, "-", () =>
			{
				Before( group.CardName + " damage -1" );
				group.Heal( 1 );
				damage.text = "fig " + (group.EngagedFigureIndex + 1) + ": "
					+ group.CurrentFigureDamage + "/" + group.PerFigureHealth;
			} );
			Button( row, "+", () =>
			{
				Before( group.CardName + " damage +1" );
				group.ApplyDamage( 1 );
				alive.text = group.FiguresAlive + "/" + group.MaxFigures + " alive";
				damage.text = "fig " + (group.EngagedFigureIndex + 1) + ": "
					+ group.CurrentFigureDamage + "/" + group.PerFigureHealth;
				boardController?.RefreshTokens();
				Refresh();
			} );

			Button( row, "next figure", () =>
			{
				Before( group.CardName + " next figure" );
				group.SetEngaged( (group.EngagedFigureIndex + 1) % Math.Max( 1, group.MaxFigures ) );
				Refresh();
			}, 120 );

			ConditionChips( row, group.Conditions );
			return row;
		}

		private void ConditionChips( GameObject row, HashSet<Condition> conditions )
		{
			foreach ( Condition c in Enum.GetValues( typeof( Condition ) ) )
			{
				var value = c;
				bool on = conditions.Contains( value );
				Button( row, value.ToString().Substring( 0, 2 ).ToUpperInvariant(), () =>
				{
					Before( (conditions.Contains( value ) ? "remove " : "apply ") + value );
					if ( conditions.Contains( value ) ) conditions.Remove( value );
					else conditions.Add( value );
					Refresh();
				}, 44, on ? new Color( 0.85f, 0.65f, 0.15f ) : new Color( 0.22f, 0.24f, 0.28f ) );
			}
		}

		private GameObject Row()
		{
			var go = new GameObject( "Row", typeof( RectTransform ) );
			go.transform.SetParent( _list, false );
			var layout = go.AddComponent<HorizontalLayoutGroup>();
			layout.spacing = 6;
			layout.childForceExpandWidth = false;
			layout.childControlWidth = true;
			layout.childControlHeight = true;
			go.AddComponent<LayoutElement>().minHeight = 38;
			return go;
		}

		private static Text Label( GameObject parent, string text, float width, Color colour )
		{
			var go = new GameObject( "Label", typeof( RectTransform ) );
			go.transform.SetParent( parent.transform, false );
			var label = go.AddComponent<Text>();
			label.text = text;
			label.color = colour;
			label.fontSize = 18;
			label.alignment = TextAnchor.MiddleLeft;
			label.font = Resources.GetBuiltinResource<Font>( "Arial.ttf" );
			go.AddComponent<LayoutElement>().preferredWidth = width;
			return label;
		}

		private static void Button( GameObject parent, string caption, Action onClick,
			float width = 40, Color? colour = null )
		{
			var go = new GameObject( "Button", typeof( RectTransform ) );
			go.transform.SetParent( parent.transform, false );
			var image = go.AddComponent<Image>();
			image.color = colour ?? new Color( 0.22f, 0.24f, 0.28f );
			var button = go.AddComponent<Button>();
			button.onClick.AddListener( () => onClick() );
			go.AddComponent<LayoutElement>().preferredWidth = width;

			var textGo = new GameObject( "Text", typeof( RectTransform ) );
			textGo.transform.SetParent( go.transform, false );
			var text = textGo.AddComponent<Text>();
			text.text = caption;
			text.color = Color.white;
			text.fontSize = 17;
			text.alignment = TextAnchor.MiddleCenter;
			text.font = Resources.GetBuiltinResource<Font>( "Arial.ttf" );
			var rect = textGo.GetComponent<RectTransform>();
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
		}
	}
}
