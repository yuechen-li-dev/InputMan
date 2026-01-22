using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;

namespace InputMan.Core;

public sealed class RebindRequest
{
    public ActionMapId Map { get; init; }
    public string BindingNameOrSlot { get; init; } = string.Empty;

    /// <summary>Exclude mouse motion / deltas by default to avoid accidental bindings.</summary>
    public bool ExcludeMouseMotion { get; init; } = true;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    // NEW: if provided, rebinding scans these instead of only known controls
    public IReadOnlyList<ControlKey>? CandidateButtons { get; init; }
    public IReadOnlyList<ControlKey>? CandidateAxes { get; init; }

}

public readonly record struct RebindProgress(string Message, float SecondsRemaining);

public sealed class RebindResult
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public ControlKey? BoundControl { get; init; }
}

public interface IRebindSession
{
    event Action<RebindProgress>? OnProgress;
    event Action<RebindResult>? OnCompleted;
    RebindRequest Request { get; }

    void Cancel();
}

internal sealed class RebindSession(
    InputManEngine engine,
    RebindRequest request,
    ActionMapDefinition mapDef,
    int bindingIndex,
    Binding binding,
    ControlKey[] knownButtons,
    ControlKey[] knownAxes,
float startTimeSeconds) : IRebindSession
{
    private readonly InputManEngine _engine = engine;
    private readonly RebindRequest _request = request;
    private readonly ActionMapDefinition _mapDef = mapDef;
    private readonly int _bindingIndex = bindingIndex;
    private readonly Binding _originalBinding = binding;

    private readonly ControlKey[] _knownButtons = knownButtons;
    private readonly ControlKey[] _knownAxes = knownAxes;

    private readonly float _startTimeSeconds = startTimeSeconds;

    private bool _seeded;
    private bool _completed;

    // Track “already down” buttons so we only capture NEW presses
    private readonly HashSet<ControlKey> _downButtons = [];

    // Track prior axis magnitudes so we can detect “crossing a small threshold”
    private readonly Dictionary<ControlKey, float> _prevAxis = [];

    public event Action<RebindProgress>? OnProgress;
    public event Action<RebindResult>? OnCompleted;

    // Expose rebind request for access to all keys
    public RebindRequest Request => _request;
    private IReadOnlyList<ControlKey> ButtonsToWatch =>
            _request.CandidateButtons ?? _knownButtons;

    private IReadOnlyList<ControlKey> AxesToWatch =>
            _request.CandidateAxes ?? _knownAxes;

    public void Cancel()
    {
        if (_completed) return;
        Complete(new RebindResult { Succeeded = false, Error = "Canceled." });
    }

    internal void Update(InputSnapshot snapshot, float timeSeconds)
    {
        if (_completed) return;

        var elapsed = timeSeconds - _startTimeSeconds;
        var remaining = (float)(_request.Timeout.TotalSeconds - elapsed);

        if (remaining <= 0f)
        {
            Complete(new RebindResult { Succeeded = false, Error = "Timed out." });
            return;
        }

        OnProgress?.Invoke(new RebindProgress("Waiting for input…", remaining));

        // Seed state once so held keys don’t instantly bind
        if (!_seeded)
        {
            Seed(snapshot);
            _seeded = true;
            return;
        }

        // Rebind type is determined by the binding we’re editing (button vs axis/delta)
        var trigType = _originalBinding.Trigger.Type;

        if (trigType == TriggerType.Button)
        {
            if (TryCaptureButton(snapshot, out var key))
                ApplyAndComplete(key);
        }
        else // Axis or DeltaAxis
        {
            if (TryCaptureAxis(snapshot, trigType, out var key))
                ApplyAndComplete(key);
        }
    }

    private void Seed(InputSnapshot snapshot)
    {
        _downButtons.Clear();
        foreach (var key in ButtonsToWatch)
        {
            if (snapshot.TryGetButton(key, out var down) && down)
                _downButtons.Add(key);
        }

        _prevAxis.Clear();
        foreach (var key in AxesToWatch)
        {
            if (_request.ExcludeMouseMotion && key.Device == DeviceKind.Mouse)
                continue;

            var v = snapshot.TryGetAxis(key, out var cur) ? cur : 0f;
            _prevAxis[key] = v;
        }
    }

    private bool TryCaptureButton(InputSnapshot snapshot, out ControlKey captured)
    {
        foreach (var key in ButtonsToWatch)
        {
            if (snapshot.TryGetButton(key, out var down) && down && !_downButtons.Contains(key))
            {
                captured = key;
                return true;
            }
        }

        // Update held-set
        foreach (var key in ButtonsToWatch)
        {
            if (snapshot.TryGetButton(key, out var down) && down)
                _downButtons.Add(key);
            else
                _downButtons.Remove(key);
        }

        captured = default;
        return false;
    }

    private bool TryCaptureAxis(InputSnapshot snapshot, TriggerType triggerType, out ControlKey captured)
    {
        // Capture threshold: tiny for DeltaAxis (mouse deltas), larger for Axis
        var captureThreshold = triggerType == TriggerType.DeltaAxis ? 0.001f : 0.25f;

        foreach (var key in AxesToWatch)
        {
            if (_request.ExcludeMouseMotion && key.Device == DeviceKind.Mouse)
                continue;

            var cur = snapshot.TryGetAxis(key, out var v) ? v : 0f;
            _prevAxis.TryGetValue(key, out var prev);

            var curAbs = MathF.Abs(cur);
            var prevAbs = MathF.Abs(prev);

            // Detect “crossing” the threshold to avoid binding on constant noise
            if (prevAbs < captureThreshold && curAbs >= captureThreshold)
            {
                captured = key;
                return true;
            }

            _prevAxis[key] = cur;
        }

        captured = default;
        return false;
    }

    private void ApplyAndComplete(ControlKey newControl)
    {
        // Replace Binding because Binding/Trigger are init-only
        var old = _originalBinding;

        var newBinding = new Binding
        {
            Name = old.Name,
            Trigger = new BindingTrigger
            {
                Control = newControl,
                Type = old.Trigger.Type,
                ButtonEdge = old.Trigger.ButtonEdge,
                Threshold = old.Trigger.Threshold,
            },
            Output = old.Output,
            Consume = old.Consume,
            Processors = old.Processors,
        };

        _mapDef.Bindings[_bindingIndex] = newBinding;

        // Tick up profile revision number to track map update.
        _engine.ProfileRevision++;

        // Rebuild known controls so edge tracking includes the newly bound control
        _engine.RebuildKnownControlsFromRebind();

        Complete(new RebindResult { Succeeded = true, BoundControl = newControl });
    }

    private void Complete(RebindResult result)
    {
        if (_completed) return;
        _completed = true;
        OnCompleted?.Invoke(result);
    }
}
