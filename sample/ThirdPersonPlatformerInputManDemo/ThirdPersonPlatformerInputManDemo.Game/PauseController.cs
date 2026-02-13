#nullable enable
using InputMan.Core;
using Stride.Engine;
using Stride.Input;
using System;

namespace ThirdPersonPlatformerInputManDemo;

/// <summary>
/// Controls pause state and coordinates between InputMan and UI.
/// This is the "controller" in MVC pattern - no rendering, just logic and state.
/// </summary>
public sealed class PauseController : SyncScript
{
    private IInputMan _inputMan = null!;
    private RebindingManager _rebindManager = null!;
    private bool _paused;

    // Map IDs
    private static readonly ActionMapId GameplayMap = new("Gameplay");
    private static readonly ActionMapId UIMap = new("UI");

    // Actions
    private static readonly ActionId Pause = new("Pause");
    private static readonly ActionId RebindJump = new("RebindJump");

    /// <summary>
    /// Is the game currently paused?
    /// </summary>
    public bool IsPaused => _paused;

    /// <summary>
    /// Access to the rebinding manager for UI to display status.
    /// </summary>
    public RebindingManager RebindManager => _rebindManager;

    /// <summary>
    /// Fired when the game is paused.
    /// </summary>
    public event Action? OnPaused;

    /// <summary>
    /// Fired when the game is resumed.
    /// </summary>
    public event Action? OnResumed;

    public override void Start()
    {
        _inputMan = Game.Services.GetService<IInputMan>()
            ?? throw new InvalidOperationException(
                "IInputMan not found. Add InstallInputMan to your scene.");

        // Create rebinding manager
        _rebindManager = new RebindingManager(_inputMan);

        // Start with both maps active
        _inputMan.SetMaps(UIMap, GameplayMap);

        Log.Info("PauseController initialized");
    }

    public override void Update()
    {
        // Toggle pause
        if (_inputMan.WasPressed(Pause))
        {
            TogglePause();
        }

        // Only process pause-related input when paused
        if (!_paused)
            return;

        // Start rebinding Jump
        if (!_rebindManager.IsRebinding && _inputMan.WasPressed(RebindJump))
        {
            StartRebindJump();
        }

        // Cancel rebinding with Escape
        // TODO: Make this use InputMan too by adding a "Cancel" action
        if (_rebindManager.IsRebinding && Input.IsKeyPressed(Keys.Escape))
        {
            _rebindManager.CancelRebind();
        }
    }

    /// <summary>
    /// Toggle between paused and playing.
    /// </summary>
    public void TogglePause()
    {
        if (_paused)
            Resume();
        else
            PauseGame();
    }

    /// <summary>
    /// Pause the game.
    /// </summary>
    public void PauseGame()
    {
        if (_paused) return;

        _paused = true;

        // Switch to UI-only map (blocks gameplay inputs)
        _inputMan.SetMaps(UIMap);

        // Show and unlock cursor
        Input.UnlockMousePosition();
        Game.IsMouseVisible = true;

        // Cancel any active rebind
        _rebindManager.CancelRebind();

        // Notify listeners (UI will show pause menu)
        OnPaused?.Invoke();

        Log.Info("Game paused");
    }

    /// <summary>
    /// Resume the game.
    /// </summary>
    public void Resume()
    {
        if (!_paused) return;

        _paused = false;

        // Restore both maps
        _inputMan.SetMaps(UIMap, GameplayMap);

        // Cancel any active rebind
        _rebindManager.CancelRebind();

        // Notify listeners (UI will hide pause menu)
        OnResumed?.Invoke();

        // Note: We don't hide/lock cursor here because PlayerInput
        // will handle that based on LookLock/LookUnlock actions

        Log.Info("Game resumed");
    }

    /// <summary>
    /// Start rebinding the Jump action.
    /// </summary>
    public void StartRebindJump()
    {
        _rebindManager.StartRebind(BindingNames.JumpKeyboard, GameplayMap);
    }

    /// <summary>
    /// Start rebinding any action by name.
    /// </summary>
    public void StartRebind(string bindingName, ActionMapId map)
    {
        _rebindManager.StartRebind(bindingName, map);
    }
}