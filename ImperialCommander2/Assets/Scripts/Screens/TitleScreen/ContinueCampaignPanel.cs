using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Saga
{
	public class ContinueCampaignPanel : MonoBehaviour
	{
		public PopupBase popupBase;
		public Text startText, cancelText, titleText;
		public CampaignTogglePrefab campaignTogglePrefab;
		public GameObject toggleContainer;
		public Button startButton;
		public GameObject cancelButton;

		Action callback;
		Guid selectedCampaign;
		List<Selectable> importButtonList = new List<Selectable>();

		public void Show( Action onClose )
		{
			importButtonList.Clear();
			InputManager.Instance.PushFocus( gameObject );
			EventSystem.current.SetSelectedGameObject( cancelButton );
			startText.text = DataStore.uiLanguage.sagaUISetup.setupStartBtn;
			cancelText.text = DataStore.uiLanguage.uiSetup.cancel;
			titleText.text = DataStore.uiLanguage.uiTitle.loadCampaign;
			callback = onClose;

			selectedCampaign = Guid.Empty;

			//popuplate existing campaigns
			foreach ( Transform item in toggleContainer.transform )
				Destroy( item.gameObject );
			var clist = FileManager.GetCampaigns();
			if ( clist.Where( x => x.campaignExpansionCode == "Imported" ).FirstOr( null ) != null )
			{
				List<CampaignPackage> packages = new List<CampaignPackage>();

				if ( DataStore.Language.ToUpper() == "EN" )
					packages = FileManager.GetCampaignPackageList( true );
				else
					packages = FileManager.GetCampaignPackageList( false );

				foreach ( var item in clist )
				{
					if ( item.campaignExpansionCode == "Imported" )
					{
						var p = packages.Where( x => x.GUID.ToString() == item.campaignPackage.GUID.ToString() ).FirstOr( null );
						if ( p != null )
							item.campaignImportedName = p.campaignName;
					}
				}
			}
			var toggleGroup = toggleContainer.GetComponent<ToggleGroup>();
			foreach ( var item in clist )
			{
				if ( item.formatVersion == Utils.expectedCampaignFormatVersion )
				{
					var go = Instantiate( campaignTogglePrefab, toggleContainer.transform );
					go.GetComponent<CampaignTogglePrefab>().Init( item, toggleGroup, OnToggleCallback );
					importButtonList.Add( go.GetComponent<Selectable>() );
				}
			}

			//iterate through the list of import buttons and modify its navigation so that it loops through the list
			for ( int i = 0; i < importButtonList.Count; i++ )
			{
				importButtonList[i].navigation = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnUp = importButtonList[(i - 1 + importButtonList.Count) % importButtonList.Count],
					selectOnDown = importButtonList[(i + 1) % importButtonList.Count],
					selectOnLeft = null,
					selectOnRight = null
				};
			}
			//set the last button's down navigation to the cancel button
			if ( importButtonList.Count > 0 )
			{
				//make the cancel button's up navigation select the last import button
				cancelButton.GetComponent<Selectable>().navigation = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnUp = importButtonList[importButtonList.Count - 1],
					selectOnDown = null,
					selectOnLeft = null,
					selectOnRight = null
				};
				var lastNav = importButtonList[importButtonList.Count - 1].navigation;
				lastNav.selectOnDown = cancelButton.GetComponent<Selectable>();
				importButtonList[importButtonList.Count - 1].navigation = lastNav;
			}

			if ( importButtonList.Count > 0 )
			{
				EventSystem.current.SetSelectedGameObject( importButtonList[0].gameObject );
			}

			startButton.interactable = false;

			popupBase.Show();
		}

		public void StartCampaign()
		{
			Close( false );
			if ( selectedCampaign != Guid.Empty )
			{
				var c = SagaCampaign.LoadCampaignState( selectedCampaign );
				if ( c != null )
				{
					//c.FixExpansionCodes();
					FindObjectOfType<TitleController>().NavToCampaignScreen( c );
				}
				else
					Utils.LogError( "StartCampaign()::Campaign state is null" );
			}
		}

		public void Close( bool doCallback = true )
		{
			InputManager.Instance.PopFocus();
			popupBase.Close();
			if ( doCallback )
				callback?.Invoke();
		}

		void OnToggleCallback( CampaignTogglePrefab t )
		{
			startButton.interactable = t.GetComponent<Toggle>().isOn;
			if ( t.GetComponent<Toggle>().isOn )
				selectedCampaign = t.campaignGUID;
		}

		private void Update()
		{
			if ( InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.Cancel ) )
			{
				Close();
			}

			if ( InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.NavigateLeft )
				&& EventSystem.current.currentSelectedGameObject == cancelButton )
				EventSystem.current.SetSelectedGameObject( startButton.gameObject );

			if ( InputManager.Instance.HasFocus()
				&& EventSystem.current.currentSelectedGameObject == null )
			{
				EventSystem.current.SetSelectedGameObject( cancelButton );
			}
		}
	}
}