using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Saga
{
	public class NewCampaignPanel : MonoBehaviour
	{
		public PopupBase popupBase;
		public TMP_Dropdown campaignExpansionDropdown;
		public TMP_InputField campaignNameInputField;
		public TextMeshProUGUI placeholderText, importedCampaignNameText;
		public Text startText, cancelText, importCampaignBtn;
		public Button startButton;
		public GameObject cancelButton, importButton;
		public Toggle customToggle;
		public ImportCampaignPanel campaignPanel;
		public Image packageSprite;

		Action callback;
		string selectedExpansion;
		List<string> selectedExpansionList, expansionCode;
		bool nameGood, importFuncDoRemove;
		CampaignPackage selectedCampaignPackage;

		public void Show( Action onClose )
		{
			InputManager.Instance.PushFocus( gameObject );
			EventSystem.current.SetSelectedGameObject( cancelButton );
			campaignNameInputField.text = "";
			startText.text = DataStore.uiLanguage.sagaUISetup.setupStartBtn;
			cancelText.text = DataStore.uiLanguage.uiSetup.cancel;
			placeholderText.text = DataStore.uiLanguage.uiCampaign.campaignNameUC;
			importCampaignBtn.text = DataStore.uiLanguage.sagaUISetup.importBtn;

			startButton.interactable = false;
			selectedCampaignPackage = null;
			importFuncDoRemove = false;
			callback = onClose;
			nameGood = false;
			selectedExpansion = "Core";
			campaignExpansionDropdown.value = 0;
			importedCampaignNameText.text = "...";

			packageSprite.gameObject.SetActive( false );

			//populate expansion dropdown
			campaignExpansionDropdown.ClearOptions();
			selectedExpansionList = DataStore.ownedExpansions.Select( x => DataStore.missionCards[x.ToString()][0].expansionText ).ToList();
			expansionCode = DataStore.ownedExpansions.Select( x => x.ToString() ).ToList();
			campaignExpansionDropdown.AddOptions( selectedExpansionList );

			popupBase.Show();
		}

		public void OnEditCampaignName()
		{
			if ( !string.IsNullOrEmpty( campaignNameInputField.text ) )
				nameGood = true;
			else
				nameGood = false;

			startButton.interactable = nameGood;
		}

		public void OnExpansionChanged()
		{
			selectedExpansion = expansionCode[campaignExpansionDropdown.value];

			startButton.interactable = nameGood;
		}

		public void OnCustomToggle()
		{
			campaignExpansionDropdown.interactable = !customToggle.isOn;
		}

		public void StartCampaign()
		{
			//create and save the new campaign
			Close( false );
			if ( selectedCampaignPackage != null )
			{
				var c = SagaCampaign.CreateNewImportedCampaign( campaignNameInputField.text, selectedCampaignPackage );
				c.SaveCampaignState();
				FindObjectOfType<TitleController>().NavToCampaignScreen( c );
			}
			else
			{
				var c = SagaCampaign.CreateNewCampaign( campaignNameInputField.text,
					!customToggle.isOn ? selectedExpansion : "Custom" );
				c.SaveCampaignState();
				FindObjectOfType<TitleController>().NavToCampaignScreen( c );
			}
		}

		public void OnImportCampaign()
		{
			if ( importFuncDoRemove )
				OnRemoveImportedCampaign();
			else
			{
				campaignPanel.Show( () =>
				{
					if ( campaignPanel.selectedPackage != null )
					{
						EventSystem.current.SetSelectedGameObject( importButton );
						selectedCampaignPackage = campaignPanel.selectedPackage;

						var translatedCampaignItem = selectedCampaignPackage.campaignTranslationItems.Where( x => x.fileName.ToLower().Contains( $"_{DataStore.Language.ToLower()}.json" ) ).FirstOr( null );

						if ( translatedCampaignItem != null )
							importedCampaignNameText.text = translatedCampaignItem.translatedMission.missionProperties.campaignName;
						else
							importedCampaignNameText.text = selectedCampaignPackage.campaignName;

						campaignExpansionDropdown.interactable = false;
						customToggle.interactable = false;
						importCampaignBtn.text = DataStore.uiLanguage.uiCampaign.removeUC.ToUpper();
						importFuncDoRemove = true;
						//generate icon sprite from loaded bytes
						Texture2D tex = new Texture2D( 2, 2 );
						if ( tex.LoadImage( selectedCampaignPackage.iconBytesBuffer ) )
						{
							packageSprite.gameObject.SetActive( true );
							Sprite iconSprite = Sprite.Create( tex, new Rect( 0, 0, tex.width, tex.height ), new Vector2( 0, 0 ), 100f );
							packageSprite.sprite = iconSprite;
						}
					}
					else
					{
						selectedCampaignPackage = null;
						EventSystem.current.SetSelectedGameObject( importButton );
					}
				} );
			}
		}

		public void OnRemoveImportedCampaign()
		{
			packageSprite.gameObject.SetActive( false );
			importedCampaignNameText.text = "...";
			campaignExpansionDropdown.interactable = true;
			customToggle.interactable = true;
			importCampaignBtn.text = DataStore.uiLanguage.sagaUISetup.importBtn;
			importFuncDoRemove = false;
			selectedCampaignPackage = null;
		}

		public void Close( bool doCallback = true )
		{
			if ( InputManager.Instance.uiAnimationsPlaying )
				return;
			InputManager.Instance.PopFocus();
			popupBase.Close();
			if ( doCallback )
				callback?.Invoke();
		}

		private void Update()
		{
			if ( InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.Cancel ) )
			{
				Close();
			}

			if ( InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.NavigateLeft )
				&& EventSystem.current.currentSelectedGameObject == cancelButton )
			{
				if ( startButton.interactable )
					EventSystem.current.SetSelectedGameObject( startButton.gameObject );
			}

			if ( InputManager.Instance.HasFocus()
				&& EventSystem.current.currentSelectedGameObject == null )
			{
				EventSystem.current.SetSelectedGameObject( cancelButton );
			}
		}
	}
}