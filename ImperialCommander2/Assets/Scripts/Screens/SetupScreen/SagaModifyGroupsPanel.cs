using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Saga
{
	public class SagaModifyGroupsPanel : MonoBehaviour
	{
		public PopupBase popupBase;
		public GameObject groupMugPrefab;
		public Toggle[] expansionToggles;
		public Transform mugContainer;
		public TextMeshProUGUI nameText;
		public DynamicCardPrefab cardPrefab;
		public HelpPanel helpPanel;
		public GameObject closeButton, helpButton, coreToggle, twinToggle, hothToggle, bespinToggle, jabbaToggle, empireToggle, lothalToggle, otherToggle, importsToggle;

		Action callback;
		int prevExp;
		bool updating;
		List<DeploymentCard> disabledGroups = new List<DeploymentCard>();
		GroupSelectionMode dataMode;
		List<Selectable> mugIconToggle = new List<Selectable>();//for navigation with controller/keyboard

		public void Show( GroupSelectionMode mode, List<DeploymentCard> disabledG = null, Action cb = null )
		{
			InputManager.Instance.PushFocus( gameObject );
			EventSystem.current.SetSelectedGameObject( closeButton );

			disabledGroups = disabledG ?? new List<DeploymentCard>();
			callback = cb;
			dataMode = mode;
			popupBase.Show();

			prevExp = -1;
			updating = false;
			ResetExpansionUI();

			OnChangeExpansion( 0 );

			//wire up navigation for the expansion toggles, based on owned expansions
			WireUpExpansionNavigation();
		}

		void WireUpExpansionNavigation()
		{
			for ( int i = 0; i < expansionToggles.Length; i++ )
			{
				expansionToggles[i].navigation = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnLeft = i > 0 ? FirstInteractibleExpansionToggleLeft( i ) : null,
					selectOnRight = i < expansionToggles.Length - 1 ? FirstInteractibleExpansionToggleRight( i ) : null,
					selectOnUp = null,
					selectOnDown = mugIconToggle.Count > 0 ? mugIconToggle[0] : closeButton.GetComponent<Button>()
				};
			}
		}

		Selectable FirstInteractibleExpansionToggleRight( int index )
		{
			for ( int i = index + 1; i < expansionToggles.Length; i++ )
			{
				if ( expansionToggles[i].interactable )
					return expansionToggles[i];
			}
			return null;
		}

		Selectable FirstInteractibleExpansionToggleLeft( int index )
		{
			for ( int i = index - 1; i >= 0; i-- )
			{
				if ( expansionToggles[i].interactable )
					return expansionToggles[i];
			}
			return null;
		}

		void ResetExpansionUI()
		{
			updating = true;
			for ( int i = 0; i < expansionToggles.Length; i++ )
			{
				expansionToggles[i].interactable = false;
				expansionToggles[i].transform.GetChild( 1 ).GetComponent<Image>().color = new Color( 1, 1, 1, .2f );
			}
			//only enable buttons for owned expansions
			for ( int i = 0; i < DataStore.ownedExpansions.Count; i++ )
			{
				expansionToggles[(int)DataStore.ownedExpansions[i]].interactable = true;
				expansionToggles[(int)DataStore.ownedExpansions[i]].transform.GetChild( 1 ).GetComponent<Image>().color = Color.white;
			}
			//core/other/imports always true
			expansionToggles[0].interactable = true;
			expansionToggles[0].isOn = true;
			expansionToggles[7].interactable = true;
			expansionToggles[7].transform.GetChild( 1 ).GetComponent<Image>().color = Color.white;
			expansionToggles[8].interactable = true;
			expansionToggles[8].transform.GetChild( 1 ).GetComponent<Image>().color = Color.white;
			updating = false;

			UpdateExpansionCounts();
		}

		//change expansion
		public void OnChangeExpansion( int idx )
		{
			if ( prevExp == idx || updating )
				return;

			prevExp = idx;
			nameText.text = "";

			foreach ( Transform item in mugContainer )
			{
				Destroy( item.gameObject );
			}

			if ( dataMode == GroupSelectionMode.Ignored )//ignored
			{
				if ( idx == 8 )//imports tab
					UpdateIgnoredImported();
				else//everything else
					UpdateIgnored( idx );
			}
			else//villains
			{
				UpdateVillains( idx );
			}

			WireUpExpansionNavigation();
		}

		void UpdateIgnored( int idx )
		{
			InputManager.Instance.uiAnimationsPlaying = true;
			mugIconToggle.Clear();

			updating = true;//avoid tripping toggle callback

			string expansion = ((Expansion)idx).ToString();
			List<DeploymentCard> cards = DataStore.deploymentCards.Where( x => x.expansion == expansion ).ToList();

			//if showing the OTHER tab, show only owned Figure Packs
			if ( expansion == "Other" )
				cards = cards.Where( x => DataStore.ownedFigurePacks.ContainsCard( x ) ).ToList();

			for ( int i = 0; i < cards.Count; i++ )
			{
				var mug = Instantiate( groupMugPrefab, mugContainer );
				mug.GetComponent<GroupMugshotToggle>().Init( cards[i], dataMode );

				//if the card is in the mission ignored list toggle it ON (ignored)
				if ( DataStore.sagaSessionData.MissionIgnored.Contains( cards[i] ) )
				{
					mug.GetComponent<GroupMugshotToggle>().isOn = true;
					mug.GetComponent<GroupMugshotToggle>().UpdateToggle();
				}

				mugIconToggle.Add( mug.transform.GetComponentInChildren<Button>().GetComponent<Selectable>() );
				//add navigation to the toggles for controller/keyboard support
				for ( int mugidx = 0; mugidx < mugIconToggle.Count; mugidx++ )
				{
					mugIconToggle[mugidx].navigation = new Navigation
					{
						mode = Navigation.Mode.Explicit,
						selectOnLeft = mugidx > 0 ? mugIconToggle[mugidx - 1] : null,
						selectOnRight = mugidx < mugIconToggle.Count - 1 ? mugIconToggle[mugidx + 1] : null,
						//set selectOnUp and down based on a row of 7 items
						selectOnUp = mugidx >= 7 ? mugIconToggle[mugidx - 7] : expansionToggles[0],
						selectOnDown = mugidx < mugIconToggle.Count - 7 ? mugIconToggle[mugidx + 7] : closeButton.GetComponent<Button>(),
					};
				}

				//disable the toggle if it's on the mission/preset ignore list
				if ( disabledGroups.ContainsCard( cards[i] ) )
					mug.GetComponent<GroupMugshotToggle>().DisableMug();
			}

			updating = false;
			UpdateExpansionCounts();
			if ( cards.Count > 0 )
				cardPrefab.InitCard( cards[0] );

			InputManager.Instance.uiAnimationsPlaying = false;
			if ( mugIconToggle.Count > 0 )
			{
				closeButton.GetComponent<Button>().navigation = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnLeft = null,
					selectOnRight = helpButton.GetComponent<Button>(),
					selectOnUp = mugIconToggle[mugIconToggle.Count - 1],//last item in the list
					selectOnDown = null
				};
			}
		}

		void UpdateIgnoredImported()
		{
			InputManager.Instance.uiAnimationsPlaying = true;
			mugIconToggle.Clear();

			updating = true;//avoid tripping toggle callback

			//add imported Imperial cards
			List<DeploymentCard> cards = DataStore.globalImportedCharacters.Where( x => x.deploymentCard.characterType == CharacterType.Imperial ).Select( x => x.deploymentCard ).ToList();

			for ( int i = 0; i < cards.Count; i++ )
			{
				var mug = Instantiate( groupMugPrefab, mugContainer );
				mug.GetComponent<GroupMugshotToggle>().Init( cards[i], dataMode );

				//set the ignore toggle (ON) based on priority (low first)
				bool ignore = false;
				//if it's excluded from Expansions, DO ignore it
				ignore = DataStore.IgnoredPrefsImports.Contains( cards[i].customCharacterGUID.ToString() );
				//if it's excluded from Expansions but INCLUDED in the session imports, do NOT ignore it
				if ( ignore && DataStore.sagaSessionData.globalImportedCharacters.ContainsCard( cards[i] ) )
					ignore = false;
				//if it's NOT excluded from Expansions but INCLUDED in the session imports, do NOT ignore
				else if ( !ignore && DataStore.sagaSessionData.globalImportedCharacters.ContainsCard( cards[i] ) )
					ignore = false;
				//if it's excluded from Expansions AND NOT in the session imports, DO ignore it
				else if ( ignore && !DataStore.sagaSessionData.globalImportedCharacters.ContainsCard( cards[i] ) )
					ignore = true;
				//if it's NOT excluded from Expansions AND NOT in the session imports, DO ignore it
				else if ( !ignore && !DataStore.sagaSessionData.globalImportedCharacters.ContainsCard( cards[i] ) )
					ignore = true;

				if ( ignore )
				{
					mug.GetComponent<GroupMugshotToggle>().isOn = true;
					mug.GetComponent<GroupMugshotToggle>().UpdateToggle();
				}
				//disable the toggle if it's on the mission/preset ignore list
				if ( disabledGroups.ContainsCard( cards[i] ) )
					mug.GetComponent<GroupMugshotToggle>().DisableMug();

				mugIconToggle.Add( mug.transform.GetComponentInChildren<Button>().GetComponent<Selectable>() );
				//add navigation to the toggles for controller/keyboard support
				for ( int mugidx = 0; mugidx < mugIconToggle.Count; mugidx++ )
				{
					mugIconToggle[mugidx].navigation = new Navigation
					{
						mode = Navigation.Mode.Explicit,
						selectOnLeft = mugidx > 0 ? mugIconToggle[mugidx - 1] : null,
						selectOnRight = mugidx < mugIconToggle.Count - 1 ? mugIconToggle[mugidx + 1] : null,
						//set selectOnUp and down based on a row of 7 items
						selectOnUp = mugidx >= 7 ? mugIconToggle[mugidx - 7] : expansionToggles[0],
						selectOnDown = mugidx < mugIconToggle.Count - 7 ? mugIconToggle[mugidx + 7] : closeButton.GetComponent<Button>(),
					};
				}
			}

			updating = false;
			UpdateExpansionCounts();
			if ( cards.Count > 0 )
				cardPrefab.InitCard( cards[0] );

			InputManager.Instance.uiAnimationsPlaying = false;
			if ( mugIconToggle.Count > 0 )
			{
				closeButton.GetComponent<Button>().navigation = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnLeft = null,
					selectOnRight = helpButton.GetComponent<Button>(),
					selectOnUp = mugIconToggle[mugIconToggle.Count - 1],//last item in the list
					selectOnDown = null
				};
			}
		}

		void UpdateVillains( int idx )
		{
			InputManager.Instance.uiAnimationsPlaying = true;
			mugIconToggle.Clear();

			updating = true;//avoid tripping toggle callback

			string expansion = ((Expansion)idx).ToString();//tab 8 (imported) will == "8"
			List<DeploymentCard> cards = new List<DeploymentCard>();

			//show custom villains
			if ( expansion == "8" )
			{
				cards = DataStore.globalImportedCharacters.Where( x => x.deploymentCard.characterType == CharacterType.Villain ).Select( x => x.deploymentCard ).ToList();

				//add embedded characters
				var setup = FindObjectOfType<SagaSetup>();
				cards = cards.Concat( setup.missionCustomVillains ).ToList();
			}

			//finally, add stock villains
			else
				cards = cards.Concat( DataStore.villainCards.Where( x => x.expansion == expansion ) ).ToList();

			for ( int i = 0; i < cards.Count; i++ )
			{
				var mug = Instantiate( groupMugPrefab, mugContainer );
				mug.GetComponent<GroupMugshotToggle>().Init( cards[i], dataMode );
				if ( DataStore.sagaSessionData.EarnedVillains.Contains( cards[i] ) )
				{
					mug.GetComponent<GroupMugshotToggle>().isOn = true;
					mug.GetComponent<GroupMugshotToggle>().UpdateToggle();
				}

				mugIconToggle.Add( mug.transform.GetComponentInChildren<Button>().GetComponent<Selectable>() );
				//add navigation to the toggles for controller/keyboard support
				for ( int mugidx = 0; mugidx < mugIconToggle.Count; mugidx++ )
				{
					mugIconToggle[mugidx].navigation = new Navigation
					{
						mode = Navigation.Mode.Explicit,
						selectOnLeft = mugidx > 0 ? mugIconToggle[mugidx - 1] : null,
						selectOnRight = mugidx < mugIconToggle.Count - 1 ? mugIconToggle[mugidx + 1] : null,
						//set selectOnUp and down based on a row of 7 items
						selectOnUp = mugidx >= 7 ? mugIconToggle[mugidx - 7] : expansionToggles[0],
						selectOnDown = mugidx < mugIconToggle.Count - 7 ? mugIconToggle[mugidx + 7] : closeButton.GetComponent<Button>(),
					};
				}
			}

			updating = false;
			if ( cards.Count > 0 )
				cardPrefab.InitCard( cards[0] );

			InputManager.Instance.uiAnimationsPlaying = false;
			if ( mugIconToggle.Count > 0 )
			{
				closeButton.GetComponent<Button>().navigation = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnLeft = null,
					selectOnRight = helpButton.GetComponent<Button>(),
					selectOnUp = mugIconToggle[mugIconToggle.Count - 1],//last item in the list
					selectOnDown = null
				};
			}
		}

		/// <summary>
		/// Fires when the toggle is ON
		/// </summary>
		public bool OnToggle( DeploymentCard card )
		{
			nameText.text = $"{card.name}";// [{card.id}]";

			if ( dataMode == GroupSelectionMode.Ignored )
			{
				var cardlist = card.IsImported ? DataStore.sagaSessionData.globalImportedCharacters : DataStore.sagaSessionData.MissionIgnored;

				cardPrefab.InitCard( card );

				if ( card.IsImported )
				{
					if ( cardlist.Contains( card ) )
					{
						//globalImportedCharacters is an INCLUSIVE list, so REMOVE it when toggled ON
						Debug.Log( $"{card.name} REMOVED FROM SESSION IMPORTS" );
						cardlist.Remove( card );
						return true;
					}
				}
				else
				{
					if ( !cardlist.Contains( card ) )
					{
						cardlist.Add( card );
						return true;
					}
				}
			}
			else
			{
				if ( !DataStore.sagaSessionData.EarnedVillains.Contains( card ) )
				{
					DataStore.sagaSessionData.EarnedVillains.Add( card );
					cardPrefab.InitCard( card );
					return true;
				}
				else
					return false;
			}

			return false;
		}

		public void UpdateExpansionCounts()
		{
			for ( int i = 0; i < expansionToggles.Length; i++ )
			{
				if ( dataMode == GroupSelectionMode.Ignored && !updating )//ignored mode
				{
					int count = 0;

					//even though all 8 Other groups are ignored by default, only show a number up to the number owned to avoid confusion (ie: owning none of them would still show 8 ignored)
					if ( i == 7 )//Other
					{
						count = DataStore.sagaSessionData.MissionIgnored.Where( x => x.expansion == ((Expansion)i).ToString() ).Count();
						count = Math.Min( count, DataStore.ownedFigurePacks.Count - (8 - count) );
					}
					else if ( i == 8 )//imported
						count = DataStore.globalImportedCharacters.Where( x => x.deploymentCard.characterType == CharacterType.Imperial ).Count() - DataStore.sagaSessionData.globalImportedCharacters.Count;
					else//everything else
						count = DataStore.sagaSessionData.MissionIgnored.Where( x => x.expansion == ((Expansion)i).ToString() ).Count();

					expansionToggles[i].transform.GetChild( 2 ).GetChild( 0 ).GetComponent<TextMeshProUGUI>().text = count.ToString();
				}
				else//villain mode
				{
					int count = 0;
					if ( i == 8 )//custom villains
					{
						count = DataStore.sagaSessionData.EarnedVillains.Where( x => x.IsImported ).Count();
					}
					else
					{
						count = DataStore.sagaSessionData.EarnedVillains.Where( x => !x.IsImported && x.expansion == ((Expansion)i).ToString() ).Count();
					}
					expansionToggles[i].transform.GetChild( 2 ).GetChild( 0 ).GetComponent<TextMeshProUGUI>().text = count.ToString();
				}
			}
		}

		public void OnClose()
		{
			if ( InputManager.Instance.uiAnimationsPlaying )
				return;
			InputManager.Instance.PopFocus();

			FindObjectOfType<Sound>().PlaySound( FX.Click );
			callback?.Invoke();
			popupBase.Close( () =>
			{
				foreach ( Transform item in mugContainer )
				{
					Destroy( item.gameObject );
				}
			} );
		}

		public void OnHelpClick()
		{
			helpPanel.Show( () =>
			{
				EventSystem.current.SetSelectedGameObject( helpButton );
			} );
		}

		private void Update()
		{
			if ( InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.DismissDialog )
				|| InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.Cancel ) )
				OnClose();

			if ( InputManager.Instance.HasFocus()
				&& EventSystem.current.currentSelectedGameObject == null )
			{
				EventSystem.current.SetSelectedGameObject( closeButton );
			}
		}
	}
}
