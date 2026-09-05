using System;
using System.Collections.Generic;

namespace InputMan.Core.Rebind;

public enum RebindConflictPolicy
{
    Allow,
    Reject,
    ReplaceExisting,
}

public sealed class RebindRequest
{
    public ActionMapId Map { get; init; }
    public string BindingNameOrSlot { get; init; } = string.Empty;

    /// <summary>Exclude mouse motion / deltas by default to avoid accidental bindings.</summary>
    public bool ExcludeMouseMotion { get; init; } = true;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    public IReadOnlyList<ControlKey>? CandidateButtons { get; set; }
    public IReadOnlyList<ControlKey>? CandidateAxes { get; set; }

    // --- Chord capture ---
    /// <summary>
    /// If true, button rebinding can capture chord modifiers (e.g. Shift+K).
    /// Modifiers are determined by <see cref="ModifierControls"/>.
    /// </summary>
    public bool AllowChord { get; set; } = false;

    /// <summary>
    /// Controls treated as chord modifiers during capture.
    /// If null/empty, chord capture is effectively disabled even if <see cref="AllowChord"/> is true.
    /// </summary>
    public IReadOnlySet<ControlKey>? ModifierControls { get; set; }

    /// <summary>Maximum number of modifiers captured into the chord.</summary>
    public int MaxModifiers { get; set; } = 2;


    // --- Guardrails ---
    /// <summary>If provided, only these device kinds can be captured.</summary>
    public IReadOnlySet<DeviceKind>? AllowedDevices { get; set; }

    /// <summary>If provided, these specific controls can never be captured.</summary>
    public IReadOnlySet<ControlKey>? ForbiddenControls { get; set; }

    /// <summary>Reject binding to a control already used by another binding in the same map.</summary>
    public bool DisallowConflictsInSameMap { get; set; } = true;

    /// <summary>Reject binding to a control already used by another binding in ANY map (stricter).</summary>
    public bool DisallowConflictsAcrossAllMaps { get; set; } = false;

    /// <summary>Explicit conflict behavior. Legacy disallow flags define the checked scope.</summary>
    public RebindConflictPolicy ConflictPolicy { get; set; } = RebindConflictPolicy.Reject;
}

public readonly record struct RebindProgress(string Message, float SecondsRemaining);

public sealed class RebindResult
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public ControlKey? BoundControl { get; init; }
    public IReadOnlyList<ControlKey>? BoundModifiers { get; init; }
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

    // Track "already down" buttons so we only capture NEW presses
    private readonly HashSet<ControlKey> _downButtons = [];

    // Track prior axis magnitudes so we can detect "crossing a small threshold"
    private readonly Dictionary<ControlKey, float> _prevAxis = [];

    public event Action<RebindProgress>? OnProgress;
    public event Action<RebindResult>? OnCompleted;

    // Expose rebind request for access to all keys
    public RebindRequest Request => _request;
    private IReadOnlyList<ControlKey> ButtonsToWatch =>
            _request.CandidateButtons ?? _knownButtons;

    private IReadOnlyList<ControlKey> AxesToWatch =>
            _request.CandidateAxes ?? _knownAxes;

    private bool IsAllowedCandidate(ControlKey key, TriggerType triggerType)
    {
        // Device filter
        if (_request.AllowedDevices is not null && !_request.AllowedDevices.Contains(key.Device))
            return false;

        // Forbidden list
        if (_request.ForbiddenControls is not null && _request.ForbiddenControls.Contains(key))
            return false;

        // Existing behavior: exclude mouse motion if requested (applies to axis/delta-axis capture)
        if (_request.ExcludeMouseMotion && triggerType == TriggerType.DeltaAxis && key.Device == DeviceKind.Mouse)
            return false;

        return true;
    }

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

        OnProgress?.Invoke(new RebindProgress("Waiting for input�", remaining));

        // Seed state once so held keys don't instantly bind
        if (!_seeded)
        {
            Seed(snapshot);
            _seeded = true;
            return;
        }

        // Rebind type is determined by the binding we're editing (button vs axis/delta)
        var trigType = _originalBinding.Trigger.Type;

        if (trigType == TriggerType.Button)
        {
            if (TryCaptureButton(snapshot, out var key))
            {
                var mods = CaptureChordModifiers(snapshot, key);
                ApplyAndComplete(key, mods);
            }
        }
        else // Axis or DeltaAxis
        {
            if (TryCaptureAxis(snapshot, trigType, out var key))
                ApplyAndComplete(key, Array.Empty<ControlKey>());
        }
    }

    private void Seed(InputSnapshot snapshot)
    {
        _downButtons.Clear();
        foreach (var key in ButtonsToWatch)
        {
            if (!IsAllowedCandidate(key, TriggerType.Button))
                continue;

            if (snapshot.TryGetButton(key, out var down) && down)
                _downButtons.Add(key);
        }

        _prevAxis.Clear();
        foreach (var key in AxesToWatch)
        {
            if (!IsAllowedCandidate(key, _originalBinding.Trigger.Type))
                continue;

            var v = snapshot.TryGetAxis(key, out var cur) ? cur : 0f;
            _prevAxis[key] = v;
        }
    }

    private bool TryCaptureButton(InputSnapshot snapshot, out ControlKey captured)
    {
        foreach (var key in ButtonsToWatch)
        {
            if (!IsAllowedCandidate(key, TriggerType.Button))
                continue;

            if (snapshot.TryGetButton(key, out var down) && down && !_downButtons.Contains(key))
            {
                // In chord mode, allow "modifier keys" to be pressed first without completing.
                if (_request.AllowChord && _request.ModifierControls is { Count: > 0 } && _request.ModifierControls.Contains(key))
                {
                    _downButtons.Add(key);
                    continue;
                }

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
            if (!IsAllowedCandidate(key, triggerType))
                continue;

            var cur = snapshot.TryGetAxis(key, out var v) ? v : 0f;
            _prevAxis.TryGetValue(key, out var prev);

            var curAbs = MathF.Abs(cur);
            var prevAbs = MathF.Abs(prev);

            // Detect "crossing" the threshold to avoid binding on constant noise
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


    private ControlKey[] CaptureChordModifiers(InputSnapshot snapshot, ControlKey primary)
    {
        if (!_request.AllowChord)
            return Array.Empty<ControlKey>();

        var modSet = _request.ModifierControls;
        if (modSet is null || modSet.Count == 0)
            return Array.Empty<ControlKey>();

        var max = _request.MaxModifiers;
        if (max <= 0)
            return Array.Empty<ControlKey>();

        var mods = new List<ControlKey>(capacity: Math.Min(max, modSet.Count));

        // Deterministic order: iterate ButtonsToWatch.
        foreach (var k in ButtonsToWatch)
        {
            if (mods.Count >= max)
                break;

            if (k.Equals(primary))
                continue;

            if (!modSet.Contains(k))
                continue;

            if (!IsAllowedCandidate(k, TriggerType.Button))
                continue;

            if (snapshot.TryGetButton(k, out var down) && down)
                mods.Add(k);
        }

        return mods.Count == 0 ? Array.Empty<ControlKey>() : mods.ToArray();
    }

    private void ApplyAndComplete(ControlKey newControl, ControlKey[] modifiers)
    {
        if (!IsAllowedCandidate(newControl, _originalBinding.Trigger.Type))
        {
            // Shouldn't happen if capture filtered correctly, but keep it airtight.
            Complete(new RebindResult { Succeeded = false, Error = "That control is not allowed." });
            return;
        }

        List<(ActionMapDefinition Map, int Index)> conflicts = FindConflicts(newControl);
        if (_request.ConflictPolicy == RebindConflictPolicy.Reject && conflicts.Count > 0)
        {
            Complete(new RebindResult
            {
                Succeeded = false,
                Error = "That control is already bound in this map."
            });
            return;
        }

        if (_request.ConflictPolicy == RebindConflictPolicy.ReplaceExisting)
        {
            foreach ((ActionMapDefinition conflictMap, int conflictIndex) in conflicts)
            {
                Binding conflict = conflictMap.Bindings[conflictIndex];
                conflictMap.Bindings[conflictIndex] = CopyWithControl(
                    conflict,
                    _originalBinding.Trigger.Control,
                    conflict.Trigger.Modifiers);
            }
        }

        // Replace Binding because Binding/Trigger are init-only
        var old = _originalBinding;

        Binding newBinding = CopyWithControl(old, newControl, modifiers);

        _mapDef.Bindings[_bindingIndex] = newBinding;

        // Tick up profile revision number to track map update.
        _engine.ProfileRevision++;

        // Rebuild known controls so edge tracking includes the newly bound control
        _engine.RebuildKnownControlsFromRebind();

        Complete(new RebindResult { Succeeded = true, BoundControl = newControl, BoundModifiers = modifiers });
    }

    private void Complete(RebindResult result)
    {
        if (_completed) return;
        _completed = true;
        OnCompleted?.Invoke(result);
    }

    /// <summary>
    /// Check if control conflicts with another binding IN THE SAME MAP.
    /// Now queries the engine's live profile instead of using stale binding list.
    /// </summary>
    private List<(ActionMapDefinition Map, int Index)> FindConflicts(ControlKey key)
    {
        var profile = _engine.GetCurrentProfile();
        var result = new List<(ActionMapDefinition Map, int Index)>();

        foreach (var mapKvp in profile.Maps)
        {
            var mapDef = mapKvp.Value;
            bool inspectMap = mapDef.Id.Equals(_mapDef.Id) || _request.DisallowConflictsAcrossAllMaps;
            if (!inspectMap || (!_request.DisallowConflictsInSameMap && !_request.DisallowConflictsAcrossAllMaps))
            {
                continue;
            }

            for (int i = 0; i < mapDef.Bindings.Count; i++)
            {
                if (mapDef.Id.Equals(_mapDef.Id) && i == _bindingIndex)
                    continue;
                if (mapDef.Bindings[i].Trigger.Control.Equals(key))
                    result.Add((mapDef, i));
            }
        }

        return result;
    }

    private static Binding CopyWithControl(Binding source, ControlKey control, ControlKey[] modifiers)
    {
        return new Binding
        {
            Name = source.Name,
            Trigger = new BindingTrigger
            {
                Control = control,
                Type = source.Trigger.Type,
                ButtonEdge = source.Trigger.ButtonEdge,
                Threshold = source.Trigger.Threshold,
                Modifiers = modifiers,
            },
            Output = source.Output,
            Consume = source.Consume,
            Processors = source.Processors,
        };
    }
}
