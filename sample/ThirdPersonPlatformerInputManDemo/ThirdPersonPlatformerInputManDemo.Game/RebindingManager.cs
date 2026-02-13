#nullable enable
using InputMan.Core;
using Stride.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPersonPlatformerInputManDemo;

/// <summary>
/// Manages all rebinding logic. Pure business logic - no UI, no rendering.
/// Can be reused in settings menus, pause menus, or anywhere else.
/// </summary>
public sealed class RebindingManager(IInputMan inputMan)
{
    private readonly IInputMan _inputMan = inputMan ?? throw new ArgumentNullException(nameof(inputMan));
    private IRebindSession? _session;
    private string _statusMessage = "";

    /// <summary>
    /// Fired whenever the status message changes (for UI to display).
    /// </summary>
    public event Action<string>? OnStatusChanged;

    /// <summary>
    /// Fired when rebinding completes. Parameter is true if successful.
    /// </summary>
    public event Action<bool>? OnCompleted;

    public bool IsRebinding => _session != null;
    public string StatusMessage => _statusMessage;

    /// <summary>
    /// Start rebinding a specific binding by name.
    /// </summary>
    /// <param name="bindingName">The exact binding name from your profile (e.g. "Jump.Kb")</param>
    /// <param name="map">Which map the binding is in</param>
    public void StartRebind(string bindingName, ActionMapId map)
    {
        // Cancel any existing session
        CancelRebind();

        // Create the rebind request
        var request = CreateRebindRequest(bindingName, map);

        // Start the session
        _session = _inputMan.StartRebind(request);

        // Subscribe to events
        _session.OnProgress += HandleProgress;
        _session.OnCompleted += HandleCompleted;

        // Update status
        UpdateStatus("Press a key to bind... (Esc cancels)");
    }

    /// <summary>
    /// Cancel the current rebinding session if active.
    /// </summary>
    public void CancelRebind()
    {
        if (_session == null)
            return;

        _session.Cancel();
        _session = null;
        UpdateStatus("Rebind canceled");
    }

    /// <summary>
    /// Creates a rebind request with appropriate settings for the binding.
    /// </summary>
    private static RebindRequest CreateRebindRequest(string bindingName, ActionMapId map)
    {
        // Determine which preset to use based on binding name
        RebindRequest request;

        if (bindingName.Contains("Mouse", StringComparison.OrdinalIgnoreCase))
        {
            // Mouse bindings: allow keyboard + mouse
            request = RebindPresets.MouseAllowedButton(map, bindingName);
        }
        else if (bindingName.Contains("Pad", StringComparison.OrdinalIgnoreCase))
        {
            // Gamepad bindings: gamepad only
            request = new RebindRequest
            {
                Map = map,
                BindingNameOrSlot = bindingName,
                AllowedDevices = new HashSet<DeviceKind> { DeviceKind.Gamepad },
                DisallowConflictsInSameMap = true,
                ExcludeMouseMotion = true,
                Timeout = TimeSpan.FromSeconds(10)
            };
        }
        else
        {
            // Default: keyboard + gamepad (no mouse)
            request = RebindPresets.GameplayButton(map, bindingName);
        }

        // Always forbid Escape (reserved for cancel)
        request.ForbiddenControls = new HashSet<ControlKey>
        {
            new(DeviceKind.Keyboard, 0, (int)Keys.Escape)
        };

        // Build candidate buttons
        request.CandidateButtons = BuildCandidateButtons(request);

        return request;
    }

    /// <summary>
    /// Builds the list of candidate buttons based on allowed devices.
    /// </summary>
    private static List<ControlKey> BuildCandidateButtons(RebindRequest request)
    {
        var candidates = new List<ControlKey>();

        // Add keyboard keys if allowed
        if (request.AllowedDevices == null ||
            request.AllowedDevices.Contains(DeviceKind.Keyboard))
        {
            candidates.AddRange(
                Enum.GetValues<Keys>()
                    .Where(k => k != Keys.None)
                    .Select(k => new ControlKey(DeviceKind.Keyboard, 0, (int)k)));
        }

        // Add mouse buttons if allowed
        if (request.AllowedDevices != null &&
            request.AllowedDevices.Contains(DeviceKind.Mouse))
        {
            candidates.Add(new ControlKey(DeviceKind.Mouse, 0, (int)MouseButton.Left));
            candidates.Add(new ControlKey(DeviceKind.Mouse, 0, (int)MouseButton.Right));
            candidates.Add(new ControlKey(DeviceKind.Mouse, 0, (int)MouseButton.Middle));
            candidates.Add(new ControlKey(DeviceKind.Mouse, 0, (int)MouseButton.Extended1));
            candidates.Add(new ControlKey(DeviceKind.Mouse, 0, (int)MouseButton.Extended2));
        }

        // Note: Gamepad buttons are automatically detected by InputMan,
        // so we don't need to enumerate them manually

        return candidates;
    }

    private void HandleProgress(RebindProgress progress)
    {
        UpdateStatus($"{progress.Message} ({progress.SecondsRemaining:F1}s)");
    }

    private void HandleCompleted(RebindResult result)
    {
        _session = null;

        if (result.Succeeded)
        {
            UpdateStatus($"Bound to: {result.BoundControl}");
            SaveProfile();
            OnCompleted?.Invoke(true);
        }
        else
        {
            UpdateStatus($"Failed: {result.Error}");
            OnCompleted?.Invoke(false);
        }
    }

    private void UpdateStatus(string message)
    {
        _statusMessage = message;
        OnStatusChanged?.Invoke(message);
    }

    private void SaveProfile()
    {
        try
        {
            var profile = _inputMan.ExportProfile();
            ProfileLoader.SaveUserProfile(profile);
        }
        catch (Exception ex)
        {
            UpdateStatus($"Failed to save: {ex.Message}");
        }
    }
}