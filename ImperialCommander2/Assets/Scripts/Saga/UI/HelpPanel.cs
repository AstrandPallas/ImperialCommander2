using System;
using System.Collections.Generic;
using System.Linq;
using Saga;
using UnityEngine;
using UnityEngine.EventSystems;

public class HelpPanel : MonoBehaviour
{
	public string helpOverlayID;//the help ID of the object this panel provides help for
	public GameObject helpButtonPrefab;
	public GameObject textBoxPrefab;
	public Transform[] rootPanels;//panels we want help for, whose UI children have a HelpMeta attached

	PopupBase popupBase, textBoxPopupBase;
	RectTransform closeButton;//this panel's "close help" button
	Canvas theCanvas;
	Transform customRegionsRoot;
	List<GameObject> helpButtons;
	int selectedHelpButtonIndex = 0;

	Action closeCallback;
	bool tbOpen = false;

	private void Awake()
	{
		theCanvas = transform.parent.parent.GetComponent<Canvas>();
		customRegionsRoot = transform.parent.Find( "Custom Regions" );
		textBoxPopupBase = transform.parent.Find( "Textbox PopupBase" ).GetComponent<PopupBase>();
		closeButton = transform.parent.Find( "close help Button" ).GetComponent<RectTransform>();
	}

	public void Show( Action callback = null )
	{
		InputManager.Instance.PushFocus( gameObject );
		popupBase = transform.parent.GetComponent<PopupBase>();
		popupBase.ShowNoZoom();

		closeCallback = callback;

		List<Transform> controlList = new List<Transform>();
		List<Transform> customRegions = new List<Transform>();
		Transform helpActivationButton = null;
		helpButtons = new List<GameObject>();

		EventSystem.current.SetSelectedGameObject( closeButton.gameObject );
		selectedHelpButtonIndex = 0;
		helpButtons.Add( closeButton.gameObject );

		foreach ( Transform rootPanel in rootPanels )
		{
			//discover the controls that have a HelpMeta attached
			controlList = controlList.Concat( rootPanel.GetComponentsInChildren<HelpMeta>().Select( x => x.transform ) ).ToList();
			//find the help activation button on the other panel (should only be 1, even with multiple panels)
			//the panel you want help for should have a "Help Activation Button" prefab on it
			if ( rootPanel.GetComponentsInChildren<Transform>().Where( x => x.name == "Help Activation Button" ).FirstOr( null ) != null )
			{
				helpActivationButton = rootPanel.GetComponentsInChildren<Transform>().Where( x => x.name == "Help Activation Button" ).FirstOr( null );
			}
		}

		//collect any custom regions
		customRegions = customRegionsRoot.GetComponentsInChildren<HelpMeta>().Select( x => x.transform ).ToList();

		//match this object's "close help" button size/position to the other panel's "activate help" button
		if ( helpActivationButton != null )
		{
			var closeBounds = RectTransformUtility.CalculateRelativeRectTransformBounds( theCanvas.transform, helpActivationButton );
			closeButton.anchoredPosition = closeBounds.center;
			closeButton.sizeDelta = closeBounds.size;
		}

		Debug.Log( $"Help Panel Found: {controlList.Count} HelpMeta UI elements" );
		Debug.Log( $"Help Panel Found: {customRegions.Count} custom regions" );

		//create a help button for each control added to this Help Panel and custom regions
		foreach ( var control in controlList.Concat( customRegions ) )
		{
			//var r = control.GetComponent<CanvasRenderer>();
			if ( !control.gameObject.activeInHierarchy )
				continue;
			var bounds = control.GetComponent<RectTransform>();

			GameObject helpButton = Instantiate( helpButtonPrefab, transform );
			//assign help metadata that contains the translation GUID
			var meta = helpButton.GetComponent<HelpMeta>();
			meta.elementID = control.GetComponent<HelpMeta>().elementID;
			meta.helpPanel = this;
			meta.isHidden = control.GetComponent<HelpMeta>().isHidden;
			meta.showIcon = control.GetComponent<HelpMeta>().showIcon;

			if ( !meta.isHidden )
			{
				var goRect = helpButton.GetComponent<RectTransform>();
				var relBounds = RectTransformUtility.CalculateRelativeRectTransformBounds( theCanvas.transform, bounds.transform );
				goRect.anchoredPosition = relBounds.center;
				goRect.sizeDelta = relBounds.size;

				helpButtons.Add( helpButton );

				if ( !meta.showIcon )
				{
					helpButton.transform.Find( "icon" ).gameObject.SetActive( false );
				}
			}
			else
				Destroy( helpButton );
		}

		if ( helpButtons.Count > 0 )
		{
			selectedHelpButtonIndex = 0;
			EventSystem.current.SetSelectedGameObject( helpButtons[selectedHelpButtonIndex] );
		}
		else
			EventSystem.current.SetSelectedGameObject( closeButton.gameObject );
	}

	public void OnHelpRequest( string elementID )
	{
		Debug.Log( $"Help requested for: {elementID}" );

		//EventSystem.current.SetSelectedGameObject( null );

		//uiHelpOverlay will be null if the help.json for the chosen language doesn't exist
		var panelHelp = DataStore.uiLanguage.uiHelpOverlay?.helpOverlayPanels.Where( x => x.panelHelpID == helpOverlayID ).FirstOr( null );
		var backupPanelHelp = DataStore.uiLanguage.uiHelpOverlayBackup?.helpOverlayPanels.Where( x => x.panelHelpID == helpOverlayID ).FirstOr( null );

		if ( panelHelp != null )
		{
			string helpText = panelHelp.helpItems.Where( x => x.id == elementID ).FirstOr( null )?.helpText;
			string helpTextBackup = backupPanelHelp?.helpItems.Where( x => x.id == elementID ).FirstOr( null )?.helpText;

			if ( helpText != null )
			{
				var go = Instantiate( textBoxPrefab, textBoxPopupBase.transform );
				var tb = go.transform.Find( "TextBox" ).GetComponent<TextBox>();
				textBoxPopupBase.ShowNoZoom();
				tb.Show( helpText, CloseTB );
				tbOpen = true;
			}
			else if ( helpTextBackup != null )
			{
				Utils.LogWarning( $"OnHelpRequest()::panelHelp [{helpOverlayID}]::helpText [{elementID}] wasn't found in the translation - using the English backup instead" );
				var go = Instantiate( textBoxPrefab, textBoxPopupBase.transform );
				var tb = go.transform.Find( "TextBox" ).GetComponent<TextBox>();
				textBoxPopupBase.ShowNoZoom();
				tb.Show( helpTextBackup, CloseTB );
				tbOpen = true;
			}
			else
				Utils.LogWarning( $"OnHelpRequest()::panelHelp [{helpOverlayID}]::helpText [{elementID}] wasn't found in the translation or in the English backup" );
		}
		else if ( backupPanelHelp != null )
		{
			Utils.LogWarning( $"OnHelpRequest()::panelHelp [{helpOverlayID}]::helpText [{elementID}] wasn't found in the translation - using the English backup instead" );
			string helpText = backupPanelHelp.helpItems.Where( x => x.id == elementID ).FirstOr( null )?.helpText;

			if ( helpText != null )
			{
				var go = Instantiate( textBoxPrefab, textBoxPopupBase.transform );
				var tb = go.transform.Find( "TextBox" ).GetComponent<TextBox>();
				textBoxPopupBase.ShowNoZoom();
				tb.Show( helpText, CloseTB );
				tbOpen = true;
			}
			else
				Utils.LogWarning( $"OnHelpRequest()::panelHelp [{helpOverlayID}]::helpText [{elementID}] wasn't found in the English backup" );
		}
		else
			Utils.LogTranslationError( $"OnHelpRequest()::panelHelp with ID=[{helpOverlayID}] wasn't found in the translation or in the English backup" );
	}

	/// <summary>
	/// Fired from the overlay Close button
	/// </summary>
	public void Close()
	{
		InputManager.Instance.PopFocus();
		popupBase.CloseNoZoom();
		closeCallback?.Invoke();
		foreach ( Transform t in transform )
		{
			Destroy( t.gameObject );
		}
	}

	/// <summary>
	/// Fired from textbox Close button
	/// </summary>
	public void CloseTB()
	{
		tbOpen = false;
		textBoxPopupBase.CloseNoZoom();
	}

	private void Update()
	{
		if ( InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.Cancel ) )
		{
			if ( tbOpen )
			{
				textBoxPopupBase.transform.Find( "TextBoxPrefab(Clone)" )
					.Find( "TextBox" )
					.GetComponent<TextBox>().OnClose();
				CloseTB();
				return;
			}
			else
				Close();
		}

		if ( tbOpen
			|| helpButtons == null
			|| helpButtons.Count == 0 )
			return;

		if ( InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.Cancel )
			|| InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.DismissDialog ) )
		{
			Close();
			return;
		}

		if ( EventSystem.current.currentSelectedGameObject == null )
		{
			EventSystem.current.SetSelectedGameObject( closeButton.gameObject );
			selectedHelpButtonIndex = 0;
		}

		if ( InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.NavigateUp )
			|| InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.NavigateLeft ) )
		{
			selectedHelpButtonIndex = (selectedHelpButtonIndex - 1 + helpButtons.Count) % helpButtons.Count;
			EventSystem.current.SetSelectedGameObject( helpButtons[selectedHelpButtonIndex] );
		}
		else if ( InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.NavigateDown )
			|| InputManager.Instance.GetFocusedInput( gameObject, FocusedInputType.NavigateRight ) )
		{
			selectedHelpButtonIndex = (selectedHelpButtonIndex + 1) % helpButtons.Count;
			EventSystem.current.SetSelectedGameObject( helpButtons[selectedHelpButtonIndex] );
		}
	}
}