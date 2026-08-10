using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Saga
{
	public class ImportCampaignPanel : MonoBehaviour
	{
		public PopupBase popupBase;
		public Text importBtnText, cancelBtnText;
		public Transform container;
		public ImportItem importItemPrefab;
		public ToggleGroup toggleGroup;
		public Button importButton;
		public GameObject cancelButton;
		public CampaignPackage selectedPackage;

		Action callback = null;
		List<Selectable> importButtonList = new List<Selectable>();

		public void Show( Action cb )
		{
			importButtonList.Clear();
			InputManager.Instance.PushFocus( gameObject );
			EventSystem.current.SetSelectedGameObject( cancelButton );
			importBtnText.text = DataStore.uiLanguage.sagaUISetup.importBtn;
			cancelBtnText.text = DataStore.uiLanguage.uiMainApp.cancel;
			callback = cb;

			selectedPackage = null;
			importButton.interactable = false;

			popupBase.Show();

			PopulateList();
		}

		private async void PopulateList()
		{
			foreach ( Transform item in container )
			{
				Destroy( item.gameObject );
			}

			try
			{
				var imports = new List<CampaignPackage>();
				await Task.Run( () =>
				{

					if ( DataStore.Language.ToUpper() == "EN" )
					{
						//do a quick load without deserializing any of the missions
						imports = FileManager.GetCampaignPackageList( true );
					}
					else
					{
						//loading the missions in order to also extract the translated names
						imports = FileManager.GetCampaignPackageList( false );
					}
				} );

				foreach ( var item in imports )
				{
					var import = Instantiate( importItemPrefab, container );
					import.Init( item, this );
					importButtonList.Add( import.theToggle );
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
						selectOnRight = importButton.GetComponent<Selectable>()
					};
					importButton.GetComponent<Selectable>().navigation = new Navigation
					{
						mode = Navigation.Mode.Explicit,
						selectOnUp = importButtonList[importButtonList.Count - 1],
						selectOnDown = null,
						selectOnLeft = cancelButton.GetComponent<Selectable>(),
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
			}
			catch ( Exception e )
			{
				Utils.LogError( $"PopulateList()::Error populating campaign imports list\n{e.Message}" );
				foreach ( Transform item in container )
				{
					Destroy( item.gameObject );
				}
				EventSystem.current.SetSelectedGameObject( cancelButton );
			}
		}

		public void ToggleSelected( CampaignPackage p )
		{
			selectedPackage = p;
			importButton.interactable = p != null;
		}

		public void OnImport()
		{
			InputManager.Instance.PopFocus();
			popupBase.Close();
			callback?.Invoke();
		}

		public void Close()//cancel
		{
			if ( InputManager.Instance.uiAnimationsPlaying )
				return;
			InputManager.Instance.PopFocus();
			selectedPackage = null;
			popupBase.Close();
			callback?.Invoke();
		}

		private void Update()
		{
			if ( InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.Cancel ) )
			{
				Close();
			}

			if ( InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.NavigateRight )
				&& EventSystem.current.currentSelectedGameObject == cancelButton )
			{
				if ( importButton.interactable )
					EventSystem.current.SetSelectedGameObject( importButton.gameObject );
			}

			if ( InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.NavigateUp )
				&& (EventSystem.current.currentSelectedGameObject == cancelButton
				|| EventSystem.current.currentSelectedGameObject == importButton.gameObject) )
			{

			}

			if ( InputManager.Instance.HasFocus()
				&& EventSystem.current.currentSelectedGameObject == null )
			{
				EventSystem.current.SetSelectedGameObject( cancelButton );
			}
		}
	}
}
