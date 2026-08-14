using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Saga
{
	/// <summary>
	/// Mugshot toggle button for earned villains and ignored groups
	/// </summary>
	public class GroupMugshotToggle : MonoBehaviour
	{
		public Button theButton;
		bool _isOn;

		public Image mugImage, outlineImage;

		[HideInInspector]
		public bool isOn
		{
			get { return _isOn; }
			set { _isOn = value; }
		}
		DeploymentCard card;
		GroupSelectionMode dataMode;

		//mode 0=enemies, 1=villains
		public void Init( DeploymentCard cd, GroupSelectionMode mode )
		{
			dataMode = mode;
			card = cd;
			mugImage.sprite = Resources.Load<Sprite>( cd.mugShotPath );

			if ( cd.isElite )
				theButton.colors = new ColorBlock()
				{
					normalColor = new Color( 1, 40f / 255f, 0 ),
					highlightedColor = Utils.Hex2UnityColor( "#9966FF" ),
					pressedColor = Utils.Hex2UnityColor( "#C8C8C8" ),
					selectedColor = Utils.Hex2UnityColor( "#9966FF" ),
					disabledColor = Utils.Hex2UnityColor( "#5C5C5C" ),
					colorMultiplier = 1,
					fadeDuration = .1f
				};
			//mugImage.color = new Color( 1, 40f / 255f, 0 );
			isOn = false;
		}

		public void UpdateToggle()
		{
			//EventSystem.current.SetSelectedGameObject( null );
			if ( isOn )
				outlineImage.color = Color.green;
			else
			{
				outlineImage.color = new Color( 0, 0.6431373f, 1 );
				isOn = false;
			}

			FindObjectOfType<SagaModifyGroupsPanel>().UpdateExpansionCounts();
		}

		public void OnToggle()
		{
			//EventSystem.current.SetSelectedGameObject( null );
			isOn = !isOn;
			if ( isOn && !FindObjectOfType<SagaModifyGroupsPanel>().OnToggle( card ) )
				isOn = false;

			if ( !isOn )
			{
				if ( dataMode == GroupSelectionMode.Ignored )//ignored mode
				{
					var cardlist = card.IsImported ? DataStore.sagaSessionData.globalImportedCharacters : DataStore.sagaSessionData.MissionIgnored;

					//globalImportedCharacters is an INCLUSIVE list, so ADD it when toggled OFF
					if ( card.IsImported )
					{
						Debug.Log( $"{card.name} ADDED TO SESSION IMPORTS" );
						cardlist.Add( card );
					}
					else
						cardlist.Remove( card );
				}
				else
					DataStore.sagaSessionData.EarnedVillains.Remove( card );
			}

			UpdateToggle();
		}

		public void DisableMug()
		{
			transform.GetChild( 0 ).GetChild( 0 ).GetComponent<Button>().interactable = false;
		}
	}
}
