using Saga;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ValueAdjuster : MonoBehaviour
{
	public PopupBase popupBase;
	public Text outText;

	MWheelHandler valueAdjusterTarget;

	public void Show( int value, MWheelHandler target )
	{
		InputManager.Instance.PushFocus( gameObject );
		popupBase.Show();
		valueAdjusterTarget = target;

		outText.text = value.ToString();
	}

	public void Hide()
	{
		InputManager.Instance.PopFocus();
		popupBase.Close();

		//let screen handle default selection after this closes
		EventSystem.current.SetSelectedGameObject( null );
	}

	public void OnAdd()
	{
		valueAdjusterTarget?.OnAdd();
	}

	public void OnSubtract()
	{
		valueAdjusterTarget?.OnSubtract();
	}

	public void SetValue( int value )
	{
		outText.text = value.ToString();
	}

	private void Update()
	{
		if ( InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.DismissDialog )
			|| InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.Cancel ) )
		{
			Hide();
		}

		if ( InputManager.Instance.HasFocus()
			&& EventSystem.current.currentSelectedGameObject == null )
		{
			//EventSystem.current.SetSelectedGameObject( gameObject );
		}
	}
}
