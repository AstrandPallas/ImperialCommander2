using Saga;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FigurePackPopup : MonoBehaviour
{
	public PopupBase popupBase;
	public Text continueText;
	public Transform layoutContainer;
	public HelpPanel helpPanel;
	public GameObject continueButton;

	public void Show()
	{
		InputManager.Instance.PushFocus( gameObject );
		EventSystem.current.SetSelectedGameObject( continueButton );
		continueText.text = DataStore.uiLanguage.uiTitle.continueBtn;

		foreach ( Transform item in layoutContainer )
		{
			item.GetChild( 1 ).GetComponent<Text>().text = DataStore.GetEnemy( $"DG0{item.name}" ).name.ToUpper();
			item.GetComponent<Toggle>().isOn = PlayerPrefs.GetInt( $"figurepack{item.name}", 0 ) == 1;
		}

		popupBase.Show();
	}

	public void Close()
	{
		InputManager.Instance.PopFocus();
		DataStore.ownedFigurePacks.Clear();
		foreach ( Transform item in layoutContainer )
		{
			if ( item.GetComponent<Toggle>().isOn )
				DataStore.ownedFigurePacks.Add( DataStore.GetEnemy( $"DG0{item.name}" ) );
			PlayerPrefs.SetInt( $"figurepack{item.name}", item.GetComponent<Toggle>().isOn ? 1 : 0 );
		}

		popupBase.Close();
	}

	public void OnHelpClick()
	{
		helpPanel.Show();
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
			EventSystem.current.SetSelectedGameObject( continueButton );
		}
	}
}
