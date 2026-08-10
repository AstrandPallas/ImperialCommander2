using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MissionTextBox : MonoBehaviour
{
	public Text continueButton;
	public TextMeshProUGUI theText;
	public Image fader;
	public CanvasGroup cg;
	public GameObject continueButtonObject;

	Action callback;

	public void Show( string text, Action action = null )
	{
		InputManager.Instance.PushFocus( gameObject );
		EventSystem.current.SetSelectedGameObject( continueButtonObject );
		callback = action;

		gameObject.SetActive( true );
		theText.text = text;
		fader.color = new Color( 0, 0, 0, 0 );
		fader.DOFade( .95f, 1 );
		cg.DOFade( 1, .5f );

		continueButton.text = DataStore.uiLanguage.uiMainApp.continueBtn.ToUpper();

		theText.transform.parent.localPosition = new Vector3( theText.transform.parent.localPosition.x, -3000, 0 );

		transform.GetChild( 1 ).localScale = new Vector3( .85f, .85f, .85f );
		transform.GetChild( 1 ).DOScale( 1, .5f ).SetEase( Ease.OutExpo );
	}

	public void OnClose()
	{
		InputManager.Instance.PopFocus();
		callback?.Invoke();
		FindObjectOfType<Sound>().PlaySound( FX.Click );
		fader.DOFade( 0, .5f ).OnComplete( () => gameObject.SetActive( false ) );
		cg.DOFade( 0, .2f );
		transform.GetChild( 1 ).DOScale( .85f, .5f ).SetEase( Ease.OutExpo );
	}

	private void Update()
	{
		if ( InputManager.Instance.GetFocusedInput( gameObject, Saga.FocusedInputType.DismissDialog ) )
			OnClose();
	}
}