using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Saga
{
	public class ErrorPanel : MonoBehaviour
	{
		public TextMeshProUGUI message;
		public PopupBase popupBase;
		public Text exitText, continueText;
		public GameObject continueButton, exitButton;

		Action closeCallback = null;

		/// <summary>
		/// centered message
		/// </summary>
		public void Show( string m, Action onCloseCallback = null )
		{
			InputManager.Instance.PushFocus( gameObject );

			exitText.text = DataStore.uiLanguage.uiSettings.quit;
			continueText.text = DataStore.uiLanguage.uiSetup.continueBtn;

			message.text = m;
			closeCallback = onCloseCallback;

			if ( continueButton.gameObject.activeSelf )
				continueButton.GetComponent<Selectable>().Select();
			else
				exitButton.GetComponent<Selectable>().Select();

			popupBase.Show();
		}

		/// <summary>
		/// Centered header, left-aligned message
		/// </summary>
		public void Show( string header, string m, Action onCloseCallback = null )
		{
			InputManager.Instance.PushFocus( gameObject );

			try
			{
				exitText.text = DataStore.uiLanguage.uiSettings.quit;
				continueText.text = DataStore.uiLanguage.uiSetup.continueBtn;
			}
			catch ( Exception )
			{
				exitText.text = "EXIT APP";
				continueText.text = "CONTINUE";
			}

			message.text = $"<color=yellow>{header}</color>\n\n<align=left>{m}</align>";
			closeCallback = onCloseCallback;

			if ( continueButton.gameObject.activeSelf )
				continueButton.GetComponent<Selectable>().Select();
			else
				exitButton.GetComponent<Selectable>().Select();

			popupBase.Show();
		}

		/// <summary>
		/// Centered header, left-aligned message and stack trace
		/// </summary>
		public void Show( string header, Exception e, Action onCloseCallback = null )
		{
			InputManager.Instance.PushFocus( gameObject );

			try
			{
				exitText.text = DataStore.uiLanguage.uiSettings.quit;
				continueText.text = DataStore.uiLanguage.uiSetup.continueBtn;
			}
			catch ( Exception )
			{
				exitText.text = "EXIT APP";
				continueText.text = "CONTINUE";
			}

			message.text = $"<color=yellow>{header}</color>\n\n<align=left><color=orange>{e.Message}</color>\n{e.StackTrace.Replace( " at ", "\nat " )}</align>";
			closeCallback = onCloseCallback;

			if ( continueButton.gameObject.activeSelf )
				continueButton.GetComponent<Selectable>().Select();
			else
				exitButton.GetComponent<Selectable>().Select();

			popupBase.Show();
		}

		public void ContinueApp()
		{
			Hide();
		}

		public void Hide()
		{
			InputManager.Instance.PopFocus();
			popupBase.Close( closeCallback );
		}

		public void OnExitApp()
		{
			Application.Quit();
		}
	}
}
