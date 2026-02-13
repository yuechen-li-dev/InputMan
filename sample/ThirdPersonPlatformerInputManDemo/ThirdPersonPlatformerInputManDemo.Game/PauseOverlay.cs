#nullable enable

using InputMan.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using System;

namespace ThirdPersonPlatformerInputManDemo;

/// <summary>
/// Pause overlay - Step 1: Now uses RebindingManager for rebinding logic.
/// Still uses DebugText temporarily (will be replaced with ImGui in Step 3).
/// </summary>
public sealed class PauseOverlay : SyncScript
{
    private IInputMan _inputMan = null!;
    private RebindingManager _rebindManager = null!;
    private bool _paused;

    // Map IDs
    private static readonly ActionMapId GameplayMap = new("Gameplay");
    private static readonly ActionMapId UIMap = new("UI");

    // Actions - now using InputMan for everything!
    private static readonly ActionId Pause = new("Pause");
    private static readonly ActionId RebindJump = new("RebindJump"); // NEW: J key via InputMan

    public override void Start()
    {
        _inputMan = Game.Services.GetService<IInputMan>()
            ?? throw new InvalidOperationException(
                "IInputMan not found. Add InstallInputMan to your scene.");

        // Create rebinding manager
        _rebindManager = new RebindingManager(_inputMan);

        // Start with both maps active
        _inputMan.SetMaps(UIMap, GameplayMap);
    }

    public override void Update()
    {
        // Toggle pause (InputMan)
        if (_inputMan.WasPressed(Pause))
            TogglePause();

        if (!_paused)
            return;

        // Draw UI
        DrawPauseMenu();

        // Rebind Jump when J is pressed (InputMan!)
        if (!_rebindManager.IsRebinding && _inputMan.WasPressed(RebindJump))
        {
            _rebindManager.StartRebind(BindingNames.JumpKeyboard, GameplayMap);
        }

        // Cancel rebind with Escape (InputMan!)
        // Note: We need to add an Escape action to the UI map for this
        if (_rebindManager.IsRebinding && Input.IsKeyPressed(Keys.Escape))
        {
            // TODO: This still uses Stride Input - will be fixed when we add
            // a proper "Cancel" action to the UI map
            _rebindManager.CancelRebind();
        }
    }

    private void TogglePause()
    {
        _paused = !_paused;

        if (_paused)
        {
            // Pause: UI map only (blocks gameplay)
            _inputMan.SetMaps(UIMap);

            // Show cursor
            Input.UnlockMousePosition();
            Game.IsMouseVisible = true;

            // Cancel any active rebind
            _rebindManager.CancelRebind();
        }
        else
        {
            // Resume: both maps active
            _inputMan.SetMaps(UIMap, GameplayMap);

            // Hide cursor if it was locked before
            // (PlayerInput will handle re-locking when needed)
        }
    }

    private void DrawPauseMenu()
    {
        DebugText.Print("=== PAUSED ===", new Int2(10, 10));
        DebugText.Print("Esc/M/Start: Resume", new Int2(10, 30));
        DebugText.Print("J: Rebind Jump", new Int2(10, 50));

        // Show rebinding status
        if (!string.IsNullOrWhiteSpace(_rebindManager.StatusMessage))
        {
            DebugText.Print(_rebindManager.StatusMessage, new Int2(10, 80));
        }
    }
}