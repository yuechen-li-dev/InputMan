#nullable enable

using InputMan.Core;
using InputMan.Core.Serialization;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Profiling;
using System;
using System.IO;

namespace ThirdPersonPlatformerInputManDemo;

public sealed class PauseOverlay : SyncScript
{
    private IInputMan _inputMan = null!;
    private bool _paused;

    // Map ids (must match profile map names)
    private static readonly ActionMapId GameplayMap = new("Gameplay");
    private static readonly ActionMapId UIMap = new("UI");

    // Actions (must match your profile)
    private static readonly ActionId Pause = new("Pause");

    // Rebind state
    private IRebindSession? _rebind;
    private string _rebindStatus = "";

    public override void Start()
    {
        _inputMan = Game.Services.GetService<IInputMan>()
            ?? throw new InvalidOperationException("IInputMan not registered. Did InstallInputMan run?");

        // Start in gameplay mode
        _inputMan.SetMaps(GameplayMap);
    }

    public override void Update()
    {
        // Pause toggle should work in BOTH maps (Pause must be bound in UI map too)
        if (_inputMan.WasPressed(Pause))
            TogglePause();

        if (_paused)
        {
            DrawPausedUI();

            // Demo hotkey: press J to start rebinding Jump
            if (_rebind == null && Input.IsKeyPressed(Keys.J))
                BeginRebind("Jump.Primary"); // change string to match your binding Name
        }

        // Rebind cancel (while paused)
        if (_rebind != null && Input.IsKeyPressed(Keys.Escape))
        {
            _rebind.Cancel();
            _rebind = null;
            _rebindStatus = "Rebind canceled.";
        }
    }

    private void TogglePause()
    {
        _paused = !_paused;

        if (_paused)
        {
            // UI-only: gameplay inputs stop because they’re not mapped anymore
            _inputMan.SetMaps(UIMap);

            // Make cursor usable for “UI”
            Input.UnlockMousePosition();
            Game.IsMouseVisible = true;

            // Optional: clear any stale status
            _rebindStatus = "";
        }
        else
        {
            // Resume gameplay
            _inputMan.SetMaps(GameplayMap);

            // Stop any active rebind session on resume
            _rebind?.Cancel();
            _rebind = null;
        }
    }

    private void BeginRebind(string bindingNameOrSlot)
    {
        // Guard: only one at a time
        _rebind?.Cancel();
        _rebind = null;

        _rebindStatus = "Press a key / button to bind… (Esc cancels)";

        var request = new RebindRequest
        {
            Map = GameplayMap,
            BindingNameOrSlot = bindingNameOrSlot,
            ExcludeMouseMotion = true,
            Timeout = TimeSpan.FromSeconds(10),
        };

        var session = _inputMan.StartRebind(request);
        _rebind = session;

        session.OnProgress += p =>
        {
            // Keep it short; progress is called frequently
            _rebindStatus = $"{p.Message}";
        };

        session.OnCompleted += r =>
        {
            _rebind = null;

            if (r.Succeeded)
            {
                _rebindStatus = $"Bound: {r.BoundControl}";
                SaveUserProfile(_inputMan.ExportProfile());
            }
            else
            {
                _rebindStatus = $"Rebind failed: {r.Error}";
            }
        };
    }

    private void DrawPausedUI()
    {
        DebugText.Print("PAUSED", new Int2(10, 10));
        DebugText.Print("Esc/M/Start: resume", new Int2(10, 30));
        DebugText.Print("J: Rebind Jump", new Int2(10, 50));

        if (!string.IsNullOrWhiteSpace(_rebindStatus))
            DebugText.Print(_rebindStatus, new Int2(10, 80));
    }

    private static void SaveUserProfile(InputProfile profile)
    {
        // Use your existing helper paths (adjust names if yours differ)
        var userPath = DemoProfilePaths.GetUserProfilePath();
        Directory.CreateDirectory(DemoProfilePaths.GetUserProfileDirectory());

        File.WriteAllText(userPath, InputProfileJson.Save(profile));
        System.Diagnostics.Debug.WriteLine($"Saved InputMan profile: {userPath}");
    }
}
