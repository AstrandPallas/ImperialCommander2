using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Saga
{
	public class MissionCardZoom : MonoBehaviour
	{
		public DynamicMissionCardPrefab dynamicMissionCard;
		public CanvasGroup cg;

		public void Show( MissionCard cd )
		{
			InputManager.Instance.PushFocus( gameObject );
			InputManager.Instance.uiAnimationsPlaying = true;
			EventSystem.current.SetSelectedGameObject( null );

			dynamicMissionCard.InitCard( cd );
			gameObject.SetActive( true );
			cg.DOFade( .95f, .5f );
			transform.GetChild( 1 ).localScale = new Vector3( .85f, .85f, .85f );
			transform.GetChild( 1 ).DOScale( 1, .5f ).SetEase( Ease.OutExpo ).OnComplete( () =>
			{
				InputManager.Instance.uiAnimationsPlaying = false;
			} );
		}

		public void Close()
		{
			InputManager.Instance.PopFocus();
			InputManager.Instance.uiAnimationsPlaying = true;
			FindObjectOfType<Sound>().PlaySound( FX.Click );
			cg.DOFade( 0, .5f ).OnComplete( () =>
			{
				gameObject.SetActive( false );
				InputManager.Instance.uiAnimationsPlaying = false;
			} );
			transform.GetChild( 1 ).DOScale( .85f, .5f ).SetEase( Ease.OutExpo );
		}

		private void Update()
		{
			if ( InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.DismissDialog )
				|| InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.Cancel ) )
				Close();
		}
	}
}
