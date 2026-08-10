using Saga;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialPanel : MonoBehaviour
{
	public PopupBase popupBase;
	public Text startText, cancelText, titleText;
	public TextMeshProUGUI descriptionText, taglineText;
	public GameObject startButton;

	int tutIndex;

	public void Show( int index )
	{
		InputManager.Instance.PushFocus( gameObject );
		startText.text = DataStore.uiLanguage.sagaUISetup.setupStartBtn;
		cancelText.text = DataStore.uiLanguage.uiSetup.cancel;
		tutIndex = index + 1;

		EventSystem.current.SetSelectedGameObject( startButton );

		//try to load the mission
		TranslatedMission translatedMission = null;
		var json = Resources.Load<TextAsset>( $"SagaTutorials/TUTORIAL0{tutIndex}" );
		if ( json != null )
		{
			DataStore.mission = FileManager.LoadMissionFromString( json.text );

			var pItem = new ProjectItem()
			{
				missionID = $"TUTORIAL0{tutIndex}",
				Description = DataStore.mission.missionProperties.missionDescription,
				AdditionalInfo = DataStore.mission.missionProperties.additionalMissionInfo,
				fullPathWithFilename = $"TUTORIAL0{tutIndex}",
				Title = DataStore.mission.missionProperties.missionName,
				pickerMode = PickerMode.Tutorial
			};
			translatedMission = FileManager.GetTutorialMissionTranslation( pItem, DataStore.Language );

			if ( translatedMission == null )//otherwise fall back to English
			{
				Debug.Log( $"No translation found for TUTORIAL0{tutIndex} in [{DataStore.Language}], falling back to English" );
				translatedMission = FileManager.GetTutorialMissionTranslation( pItem, "EN" );
			}
		}

		if ( DataStore.mission != null && translatedMission != null )
		{
			titleText.text = translatedMission.missionProperties.missionName.ToUpper();
			descriptionText.text = translatedMission.missionProperties.missionDescription;
			taglineText.text = translatedMission.missionProperties.additionalMissionInfo;
			//set Mission name to translated name
			DataStore.mission.missionProperties.missionName = translatedMission.missionProperties.missionName;
		}

		popupBase.Show();
	}

	public void Close()
	{
		InputManager.Instance.PopFocus();
		popupBase.Close();
	}

	public void StartTutorial()
	{
		popupBase.Close();

		if ( DataStore.mission == null )
			return;

		//mission is loaded at this point
		var setupOptions = new SagaSetupOptions()
		{
			isTutorial = true,
			tutorialIndex = tutIndex,
			difficulty = Difficulty.Medium,
			threatLevel = 2,
		};
		DataStore.StartNewSagaSession( setupOptions );
		DataStore.sagaSessionData.MissionHeroes.Add( DataStore.heroCards[2] );
		DataStore.sagaSessionData.MissionHeroes.Add( DataStore.heroCards[4] );

		var pItem = new ProjectItem()
		{
			missionID = $"TUTORIAL0{tutIndex}",//DataStore.mission.missionProperties.missionID,
			Description = DataStore.mission.missionProperties.missionDescription,
			AdditionalInfo = DataStore.mission.missionProperties.additionalMissionInfo,
			fullPathWithFilename = $"TUTORIAL0{tutIndex}",
			Title = DataStore.mission.missionProperties.missionName,
			pickerMode = PickerMode.Tutorial
		};
		DataStore.sagaSessionData.setupOptions.projectItem = pItem;

		//ignore figure packs for Tutorial
		for ( int i = 62; i <= 69; i++ )
		{
			var c = DataStore.allEnemyDeploymentCards.GetDeploymentCard( $"DG0{i}" );
			DataStore.sagaSessionData.MissionIgnored.Add( c );
		}

		FindObjectOfType<TitleController>().StartTutorial();
	}

	private void Update()
	{
		if ( InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.Cancel ) )
		{
			Close();
		}

		if ( InputManager.Instance.HasFocus()
			&& EventSystem.current.currentSelectedGameObject == null )
		{
			EventSystem.current.SetSelectedGameObject( startButton );
		}
	}
}
