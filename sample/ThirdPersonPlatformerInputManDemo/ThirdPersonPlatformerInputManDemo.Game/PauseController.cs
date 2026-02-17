#nullable enable
using InputMan.Core;
using InputMan.StrideConn;
using Stride.Engine;
using Stride.Input;
using System;
using System.Collections.Generic;

namespace ThirdPersonPlatformerInputManDemo;

/// <summary>
/// Controls pause state and coordinates between InputMan and UI.
/// Updated to use new Core RebindingManager with StrideConn helpers.
/// </summary>
public sealed class PauseController : SyncScript
{
    private IInputMan _inputMan = null!;
    private IProfileStorage _storage = null!;
    private RebindingManager _rebindManager = null!;
    private bool _paused;

    // Map IDs
    private static readonly ActionMapId GameplayMap = new("Gameplay");
    private static readonly ActionMapId UIMap = new("UI");

    // Actions
    private static readonly ActionId Pause = new("Pause");
    private static readonly ActionId RebindJump = new("RebindJump");
    private static readonly ActionId CancelRebind = new("CancelRebind");

    // Forbidden controls (reserved for system use)
    private static readonly HashSet<ControlKey> ForbiddenControls = new()
    {
        new(DeviceKind.Keyboard, 0, (int)Keys.Escape) // Reserved for cancel/menu
    };

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

        // Create storage (same config as InstallInputMan)
        _storage = StrideProfileStorage.CreateDefault(
            appName: "ThirdPersonPlatformerInputManDemo",
            defaultProfileFactory: DefaultPlatformerProfile.Create);

        // Create rebinding manager with storage
        _rebindManager = new RebindingManager(_inputMan, _storage);

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
        if (_rebindManager.IsRebinding && _inputMan.WasPressed(CancelRebind))
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

        Log.Info("Game resumed");
    }

    /// <summary>
    /// Start rebinding the Jump action.
    /// Uses StrideCandidateButtons to build the candidate list.
    /// </summary>
    public void StartRebindJump()
    {
        // Build candidate buttons for keyboard + gamepad (no mouse)
        var candidates = StrideCandidateButtons.KeyboardAndGamepad();

        // Start rebinding with candidates and forbidden controls
        _rebindManager.StartRebind(
            bindingName: BindingNames.JumpKeyboard,
            map: GameplayMap,
            candidateButtons: candidates,
            forbiddenControls: ForbiddenControls,
            disallowConflicts: true);
    }

    /// <summary>
    /// Start rebinding any action by name.
    /// Determines appropriate candidate buttons based on binding name.
    /// </summary>
    public void StartRebind(string bindingName, ActionMapId map)
    {
        // Determine candidates based on binding name
        List<ControlKey> candidates;

        if (bindingName.Contains("Mouse", StringComparison.OrdinalIgnoreCase))
        {
            // Mouse bindings: allow keyboard + mouse
            candidates = StrideCandidateButtons.KeyboardAndMouse();
        }
        else if (bindingName.Contains("Pad", StringComparison.OrdinalIgnoreCase))
        {
            // Gamepad bindings: gamepad only (empty list, auto-detected)
            candidates = new List<ControlKey>();
        }
        else
        {
            // Default: keyboard + gamepad
            candidates = StrideCandidateButtons.KeyboardAndGamepad();
        }

        _rebindManager.StartRebind(
            bindingName: bindingName,
            map: map,
            candidateButtons: candidates,
            forbiddenControls: ForbiddenControls,
            disallowConflicts: true);
    }
}