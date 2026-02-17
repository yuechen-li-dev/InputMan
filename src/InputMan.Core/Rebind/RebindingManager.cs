using System;
using System.Collections.Generic;

namespace InputMan.Core.Rebind;

/// <summary>
/// Manages all rebinding logic in an engine-agnostic way.
/// Pure business logic - no UI, no rendering, no engine-specific code.
/// Can be reused in any game engine with InputMan.
/// </summary>
/// <remarks>
/// Create a new rebinding manager.
/// </remarks>
/// <param name="inputMan">The InputMan instance to rebind.</param>
/// <param name="storage">Profile storage for saving after successful rebinds.</param>
public sealed class RebindingManager(IInputMan inputMan, IProfileStorage storage)
{
    private readonly IInputMan _inputMan = inputMan ?? throw new ArgumentNullException(nameof(inputMan));
    private readonly IProfileStorage _storage = storage ?? throw new ArgumentNullException(nameof(storage));
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
    /// <param name="candidateButtons">List of buttons that can be bound to (engine-specific)</param>
    /// <param name="forbiddenControls">Optional list of controls that cannot be bound to</param>
    /// <param name="disallowConflicts">If true, prevents binding to already-used controls</param>
    public void StartRebind(
        string bindingName,
        ActionMapId map,
        IReadOnlyList<ControlKey> candidateButtons,
        IReadOnlySet<ControlKey>? forbiddenControls = null,
        bool disallowConflicts = true)
    {
        if (string.IsNullOrWhiteSpace(bindingName))
            throw new ArgumentException("Binding name cannot be empty", nameof(bindingName));
        if (candidateButtons == null || candidateButtons.Count == 0)
            throw new ArgumentException("Must provide at least one candidate button", nameof(candidateButtons));

        // Cancel any existing session
        CancelRebind();

        // Create the rebind request
        var request = new RebindRequest
        {
            Map = map,
            BindingNameOrSlot = bindingName,
            CandidateButtons = candidateButtons,
            ForbiddenControls = forbiddenControls,
            DisallowConflictsInSameMap = disallowConflicts,
            ExcludeMouseMotion = true,
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Start the session
        _session = _inputMan.StartRebind(request);

        // Subscribe to events
        _session.OnProgress += HandleProgress;
        _session.OnCompleted += HandleCompleted;

        // Update status
        UpdateStatus("Press a key to bind... (Esc cancels)");
    }

    /// <summary>
    /// Start rebinding with a pre-configured RebindRequest.
    /// Use this for advanced scenarios with custom request settings.
    /// </summary>
    public void StartRebind(RebindRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Cancel any existing session
        CancelRebind();

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
            _storage.SaveProfile(profile);
        }
        catch (Exception ex)
        {
            UpdateStatus($"Failed to save: {ex.Message}");
        }
    }
}