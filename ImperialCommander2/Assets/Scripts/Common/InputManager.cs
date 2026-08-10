using System;
using System.Collections.Generic;
using Saga;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
	private PlayerInput playerInput;

	//UI state
	public bool uiAnimationsPlaying = false;

	//Focus tracking
	private Stack<GameObject> focusStack = new Stack<GameObject>();

	//Input backing fields (populated in Update before focus filtering)
	private bool _settingsOpenCloseInput = false;
	private bool _navUp = false;//arrow up or controller DPad up
	private bool _navDown = false;//arrow down or controller DPad down
	private bool _navLeft = false;//arrow left or controller DPad left
	private bool _navRight = false;//arrow right or controller DPad right
	private bool _analogLeftUp = false;
	private bool _analogLeftDown = false;
	private bool _analogRightUp = false;
	private bool _analogRightDown = false;
	private bool _cancelPressed = false;//ESC key or controller B button
	private bool _increaseValue = false;//arrow up or controller analog right
	private bool _decreaseValue = false;//arrow down or controller analog left
	private bool _dismissDialog = false;//Spacebar key or controller B button

	public static InputManager Instance { get; private set; }

	// Input properties with automatic focus and animation blocking
	// When main screen has focus (stack empty): returns input if animations aren't playing
	// When a panel has focus (stack not empty): returns false to block input to main screen
	// Focused panels should check IsFocused(gameObject) in their Update() before processing input
	public bool settingsOpenCloseInput => !uiAnimationsPlaying && focusStack.Count == 0 && _settingsOpenCloseInput;
	public bool navUp => !uiAnimationsPlaying && focusStack.Count == 0 && _navUp;
	public bool navDown => !uiAnimationsPlaying && focusStack.Count == 0 && _navDown;
	public bool navLeft => !uiAnimationsPlaying && focusStack.Count == 0 && _navLeft;
	public bool navRight => !uiAnimationsPlaying && focusStack.Count == 0 && _navRight;
	public bool analogLeftUp => !uiAnimationsPlaying && focusStack.Count == 0 && _analogLeftUp;
	public bool analogLeftDown => !uiAnimationsPlaying && focusStack.Count == 0 && _analogLeftDown;
	public bool analogRightUp => !uiAnimationsPlaying && focusStack.Count == 0 && _analogRightUp;
	public bool analogRightDown => !uiAnimationsPlaying && focusStack.Count == 0 && _analogRightDown;
	public bool cancelPressed => !uiAnimationsPlaying && focusStack.Count == 0 && _cancelPressed;
	public bool increaseValue => !uiAnimationsPlaying && focusStack.Count == 0 && _increaseValue;
	public bool decreaseValue => !uiAnimationsPlaying && focusStack.Count == 0 && _decreaseValue;
	public bool dismissDialog => !uiAnimationsPlaying && focusStack.Count == 0 && _dismissDialog;

	public bool anyPanelsOpen => focusStack.Count > 0;

	private void Awake()
	{
		if ( Instance == null )
			Instance = this;
		playerInput = GetComponent<PlayerInput>();
	}

	private void Update()
	{
		// Populate backing fields with raw input state (before focus filtering)
		_settingsOpenCloseInput = playerInput.actions["SettingsOpenClose"].WasPressedThisFrame();
		_navUp = playerInput.actions["NavigateUp"].WasPressedThisFrame();
		_navDown = playerInput.actions["NavigateDown"].WasPressedThisFrame();
		_navLeft = playerInput.actions["NavigateLeft"].WasPressedThisFrame();
		_navRight = playerInput.actions["NavigateRight"].WasPressedThisFrame();
		_analogLeftUp = playerInput.actions["AnalogLeftUp"].WasPressedThisFrame();
		_analogLeftDown = playerInput.actions["AnalogLeftDown"].WasPressedThisFrame();
		_analogRightUp = playerInput.actions["AnalogRightUp"].WasPressedThisFrame();
		_analogRightDown = playerInput.actions["AnalogRightDown"].WasPressedThisFrame();
		_cancelPressed = playerInput.actions["Cancel"].WasPressedThisFrame();
		_increaseValue = playerInput.actions["IncreaseValue"].WasPressedThisFrame();
		_decreaseValue = playerInput.actions["DecreaseValue"].WasPressedThisFrame();
		_dismissDialog = playerInput.actions["DismissDialog"].WasPressedThisFrame();
	}

	#region Focus Tracking System
	/// <summary>
	/// Registers a UI panel to receive input focus. Call this when a panel opens.
	/// The panel will be pushed onto the focus stack and become the active input receiver.
	/// Make sure to call PopFocus() when the panel closes to prevent stack corruption.
	/// 
	/// USAGE EXAMPLE:
	/// public void Show() {
	///     InputManager.Instance.PushFocus(gameObject);  // Call at start of Show()
	///     InputManager.Instance.uiAnimationsPlaying = true;
	///     // ... animation code ...
	///     .OnComplete(() => { uiAnimationsPlaying = false; });
	/// }
	/// </summary>
	/// <param name="panel">The GameObject representing the panel (typically 'this.gameObject' from the calling panel)</param>
	public void PushFocus( GameObject panel )
	{
		if ( panel == null )
		{
			Debug.LogWarning( "InputManager.PushFocus: Attempted to push null GameObject to focus stack" );
			return;
		}

		focusStack.Push( panel );
	}

	/// <summary>
	/// Removes the topmost panel from the focus stack. Call this when a panel closes.
	/// WARNING: Always call this in the same panel that called PushFocus(), otherwise the stack will be corrupted.
	/// 
	/// USAGE EXAMPLE:
	/// public void Close() {
	///     InputManager.Instance.uiAnimationsPlaying = true;
	///     // ... close animation ...
	///     .OnComplete(() => {
	///         InputManager.Instance.PopFocus();  // Call after animation completes
	///         InputManager.Instance.uiAnimationsPlaying = false;
	///         gameObject.SetActive(false);
	///     });
	/// }
	/// </summary>
	public void PopFocus()
	{
		if ( focusStack.Count == 0 )
		{
			Debug.LogWarning( "InputManager.PopFocus: Attempted to pop from empty focus stack" );
			return;
		}

		focusStack.Pop();
	}

	/// <summary>
	/// Checks if any panel currently has input focus (i.e., focus stack is not empty).
	/// When the stack is empty, the main game screen has focus.
	/// </summary>
	/// <returns>True if a panel has focus, false if main screen has focus</returns>
	public bool HasFocus()
	{
		CleanupDestroyedObjects();
		return focusStack.Count > 0;
	}

	/// <summary>
	/// Checks if a specific GameObject currently has input focus (is at the top of the focus stack).
	/// 
	/// USAGE EXAMPLE:
	/// void Update() {
	///     if (InputManager.Instance.IsFocused(gameObject)) {
	///         // This panel has focus - safe to process input
	///         if (InputManager.Instance.GetFocusedInput(gameObject, FocusedInputType.Escape))
	///             Close();
	///     }
	/// }
	/// </summary>
	/// <param name="panel">The GameObject to check</param>
	/// <returns>True if this panel is at the top of the focus stack</returns>
	public bool IsFocused( GameObject panel )
	{
		CleanupDestroyedObjects();

		if ( panel == null )
			return false;

		// If stack is empty, only return true if checking for null (main screen focus)
		if ( focusStack.Count == 0 )
			return false;

		return focusStack.Peek() == panel;
	}

	/// <summary>
	/// Gets input state for a specific input type, with focus checking for the specified panel.
	/// Use this in panel Update() methods to check if input was pressed AND the panel has focus.
	/// Example: if (InputManager.Instance.GetFocusedInput(gameObject, FocusedInputType.Escape)) { Close(); }
	/// </summary>
	/// <param name="panel">The panel checking for input (pass 'this.gameObject')</param>
	/// <param name="inputType">The focused input to query</param>
	/// <returns>True if the input was pressed, animations aren't playing, and this panel has focus</returns>
	public bool GetFocusedInput( GameObject panel, FocusedInputType inputType )
	{
		if ( uiAnimationsPlaying || !IsFocused( panel ) )
			return false;

		switch ( inputType )
		{
			case FocusedInputType.SettingsOpenClose:
				return _settingsOpenCloseInput;
			case FocusedInputType.NavigateUp:
				return _navUp;
			case FocusedInputType.NavigateDown:
				return _navDown;
			case FocusedInputType.NavigateLeft:
				return _navLeft;
			case FocusedInputType.NavigateRight:
				return _navRight;
			case FocusedInputType.AnalogLeftUp:
				return _analogLeftUp;
			case FocusedInputType.AnalogLeftDown:
				return _analogLeftDown;
			case FocusedInputType.AnalogRightUp:
				return _analogRightUp;
			case FocusedInputType.AnalogRightDown:
				return _analogRightDown;
			case FocusedInputType.Cancel:
				return _cancelPressed;
			case FocusedInputType.IncreaseValue:
				return _increaseValue;
			case FocusedInputType.DecreaseValue:
				return _decreaseValue;
			case FocusedInputType.DismissDialog:
				return _dismissDialog;
			default:
				return false;
		}
	}

	/// <summary>
	/// Removes any destroyed GameObjects from the focus stack to prevent memory leaks
	/// and corrupted stack state.
	/// </summary>
	private void CleanupDestroyedObjects()
	{
		// Clean up any destroyed objects from the stack
		var tempStack = new Stack<GameObject>();
		while ( focusStack.Count > 0 )
		{
			var obj = focusStack.Pop();
			if ( obj != null )
				tempStack.Push( obj );
		}

		// Rebuild stack in correct order
		while ( tempStack.Count > 0 )
		{
			focusStack.Push( tempStack.Pop() );
		}
	}

	#endregion
}
