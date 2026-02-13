#nullable enable

using Stride.Core.Mathematics;
using Stride.Engine;
using System;
using System.Linq;

namespace ThirdPersonPlatformerInputManDemo;

/// <summary>
/// Pause overlay UI - Step 2: Now just renders UI, all logic is in PauseController.
/// Still uses DebugText temporarily (will be replaced with ImGui in Step 3).
/// </summary>
public sealed class PauseOverlay : SyncScript
{
    /// <summary>
    /// Reference to the PauseController (assign in Stride editor or find automatically).
    /// </summary>
    public PauseController? Controller { get; set; }

    public override void Start()
    {
        // Try to find PauseController if not assigned
        Controller ??= Entity.Scene?.Entities
            .SelectMany(e => e.GetAll<PauseController>())
            .FirstOrDefault();

        if (Controller == null)
        {
            throw new InvalidOperationException(
                "PauseController not found! Make sure it's in the scene.");
        }

        // Subscribe to pause events
        Controller.OnPaused += OnGamePaused;
        Controller.OnResumed += OnGameResumed;

        Log.Info("PauseOverlay initialized");
    }

    public override void Update()
    {
        // Only draw when paused
        if (Controller?.IsPaused == true)
        {
            DrawPauseMenu();
        }
    }

    private void OnGamePaused()
    {
        Log.Info("PauseOverlay: Game paused, showing menu");
    }

    private void OnGameResumed()
    {
        Log.Info("PauseOverlay: Game resumed, hiding menu");
    }

    private void DrawPauseMenu()
    {
        if (Controller == null) return;

        // Draw pause menu with DebugText (temporary)
        DebugText.Print("=== PAUSED ===", new Int2(10, 10));
        DebugText.Print("Esc/M/Start: Resume", new Int2(10, 30));
        DebugText.Print("J: Rebind Jump", new Int2(10, 50));

        // Show rebinding status
        var status = Controller.RebindManager.StatusMessage;
        if (!string.IsNullOrWhiteSpace(status))
        {
            DebugText.Print(status, new Int2(10, 80));
        }
    }
}