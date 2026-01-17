using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InputMan.Core;

/// <summary>
/// Default Core implementation. Feed it an <see cref="InputSnapshot"/> once per frame.
/// </summary>
public sealed class InputManEngine : IInputMan
{
    private InputProfile _profile;

    // Active maps (stack-ish with optional overrides). We keep a refcount to tolerate double pushes.
    private readonly Dictionary<ActionMapId, ActiveMapEntry> _activeMaps = [];
    private ActiveMapEntry[] _activeMapsSorted = [];
    private bool _activeMapsDirty = true;

    // Known controls (from profile) so we can compute edges even if the snapshot omits absent keys.
    private readonly HashSet<ControlKey> _knownButtonControls = [];
    private readonly HashSet<ControlKey> _knownAxisControls = [];

    // Previous control states (for edge detection)
    private readonly Dictionary<ControlKey, bool> _prevButtons = [];

    // Output states
    private readonly Dictionary<ActionId, ActionState> _actions = [];
    private readonly Dictionary<AxisId, float> _axes = [];

    // Consumption within the current frame
    private readonly HashSet<ControlKey> _consumedControls = [];

    public long FrameIndex { get; private set; }
    public float DeltaTimeSeconds { get; private set; }
    private float _timeSeconds;

    public event Action<ActionEvent>? OnAction;
    public event Action<AxisEvent>? OnAxis;

    public InputManEngine(InputProfile? profile = null)
    {
        _profile = profile ?? new InputProfile();
        RebuildKnownControls();
    }

    public void Tick(InputSnapshot snapshot, float deltaTimeSeconds, float timeSeconds)
    {
        FrameIndex++;
        DeltaTimeSeconds = deltaTimeSeconds;
        _timeSeconds = timeSeconds;

        if (_activeMapsDirty)
            RebuildActiveMapsSorted();

        // 1. Reset per-frame state without allocating new arrays
        foreach (var id in _actions.Keys.ToArray())
            _actions[id] = _actions[id] with { PressedThisFrame = false, ReleasedThisFrame = false };

        foreach (var id in _axes.Keys.ToArray())
            _axes[id] = 0f;

        _consumedControls.Clear();

        // 2. Evaluate Map Bindings
        foreach (var active in _activeMapsSorted)
        {
            if (!_profile.Maps.TryGetValue(active.MapId.Name, out var mapDef))
                continue;

            foreach (var binding in mapDef.Bindings)
            {
                var control = binding.Trigger.Control;
                bool shouldConsume = mapDef.CanConsume && binding.Consume is ConsumeMode.ControlOnly or ConsumeMode.All;

                // Early exit if control is already consumed
                if (shouldConsume && _consumedControls.Contains(control))
                    continue;

                if (!TryEvaluateBinding(binding, snapshot, out var actionEvt, out var axisEvt, out var didTrigger))
                    continue;

                // Handle Action Events
                if (actionEvt.HasValue)
                    OnAction?.Invoke(actionEvt.Value);

                // Handle Axis Events
                if (axisEvt.HasValue)
                {
                    var a = axisEvt.Value.Axis;
                    _axes[a] = Clamp(_axes[a], -1f, 1f); //clamp first

                    OnAxis?.Invoke(axisEvt.Value with { Value = _axes[a] });
                }


                // 3. Centralized Consumption Logic
                if (shouldConsume && didTrigger)
                    _consumedControls.Add(control);
            }
        }

        // 4. Update previous state for next frame
        foreach (var key in _knownButtonControls)
        {
            _prevButtons[key] = snapshot.TryGetButton(key, out var cur) && cur;
        }
    }

    public bool IsDown(ActionId action) => _actions.TryGetValue(action, out var st) && st.Down;
    public bool WasPressed(ActionId action) => _actions.TryGetValue(action, out var st) && st.PressedThisFrame;
    public bool WasReleased(ActionId action) => _actions.TryGetValue(action, out var st) && st.ReleasedThisFrame;

    public float GetAxis(AxisId axis) => _axes.TryGetValue(axis, out var v) ? v : 0f;

    public Vector2 GetAxis2(Axis2Id axis2)
    {
        // Use a guard clause or a concise ternary for simple lookups
        if (!_profile.Axis2.TryGetValue(axis2.Name, out var def))
            return Vector2.Zero;

        return new Vector2(GetAxis(def.X), GetAxis(def.Y));
    }


    public void PushMap(ActionMapId map, int? priorityOverride = null)
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_activeMaps, map, out bool exists);

        entry = exists
            ? entry with { RefCount = entry.RefCount + 1, PriorityOverride = priorityOverride ?? entry.PriorityOverride }
            : new ActiveMapEntry(map, 1, priorityOverride);

        _activeMapsDirty = true;
    }

    public void PopMap(ActionMapId map)
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(_activeMaps, map);

        if (Unsafe.IsNullRef(ref entry))
            return;

        // Check if it's the last reference before updating
        if (entry.RefCount <= 1)
        {
            _activeMaps.Remove(map);
        }
        else
        {
            // Reassign the struct reference using 'with' to bypass init-only restrictions
            entry = entry with { RefCount = entry.RefCount - 1 };
        }

        _activeMapsDirty = true;
    }

    public void SetMaps(params ActionMapId[] maps)
    {
        _activeMaps.Clear();
        foreach (var m in maps)
            _activeMaps[m] = new ActiveMapEntry(m, 1, PriorityOverride: null);

        _activeMapsDirty = true;
    }

    public IRebindSession StartRebind(RebindRequest request)
        => throw new NotSupportedException("Rebinding session will be implemented in milestone M4.");

    public InputProfile ExportProfile() => _profile;

    public void ImportProfile(InputProfile profile)
    {
        _profile = profile ?? new InputProfile();

        _actions.Clear();
        _axes.Clear();
        _prevButtons.Clear();

        RebuildKnownControls();
        _activeMapsDirty = true;
    }

    private bool TryEvaluateBinding(
        Binding binding,
        InputSnapshot snapshot,
        out ActionEvent? actionEvent,
        out AxisEvent? axisEvent,
        out bool didTrigger)
    {
        actionEvent = null;
        axisEvent = null;
        didTrigger = false;

        var trig = binding.Trigger;
        var control = trig.Control;

        if (trig.Type != TriggerType.Button) return false;

        // 1. Determine state changes once
        bool curDown = snapshot.TryGetButton(control, out var cur) && cur;
        bool prevDown = _prevButtons.TryGetValue(control, out var prev) && prev;
        bool isPressed = !prevDown && curDown;
        bool isReleased = prevDown && !curDown;

        // 2. Resolve the trigger firing state
        didTrigger = trig.ButtonEdge switch
        {
            ButtonEdge.Down => curDown,
            ButtonEdge.Pressed => isPressed,
            ButtonEdge.Released => isReleased,
            _ => false
        };

        // 3. Handle ActionOutput
        if (binding.Output is ActionOutput ao)
        {
            var existing = _actions.GetValueOrDefault(ao.Action);

            // Determine new state and event phase via pattern matching
            var (newState, phase) = (isPressed, isReleased) switch
            {
                (true, _) => (existing with { Down = true, PressedThisFrame = true }, ActionPhase.Pressed),
                (_, true) => (existing with { Down = false, ReleasedThisFrame = true }, ActionPhase.Released),
                _ => (existing with { Down = curDown }, (ActionPhase?)null)
            };

            _actions[ao.Action] = newState;
            actionEvent = phase.HasValue
                ? new ActionEvent(ao.Action, phase.Value, FrameIndex, _timeSeconds)
                : null;

            axisEvent = null;
            return true;
        }

        // 4. Handle AxisOutput
        if (binding.Output is AxisOutput ax)
        {
            // A simple boolean check: does the current state satisfy the trigger requirement?
            if (!didTrigger || trig.ButtonEdge == ButtonEdge.Released)
                return false;

            _axes[ax.Axis] = _axes.GetValueOrDefault(ax.Axis) + ax.Scale;

            axisEvent = new AxisEvent(ax.Axis, _axes[ax.Axis], FrameIndex, _timeSeconds);
            actionEvent = null;
            return true;
        }

        return false;
    }

    private void RebuildKnownControls()
    {
        // 1. Extract all bindings from the profile maps
        var allBindings = _profile.Maps.Values.SelectMany(m => m.Bindings);

        // 2. Refresh the Button Controls
        _knownButtonControls.Clear();
        _knownButtonControls.UnionWith(
            allBindings.Where(b => b.Trigger.Type == TriggerType.Button)
                       .Select(b => b.Trigger.Control)
        );

        // 3. Refresh the Axis Controls
        _knownAxisControls.Clear();
        _knownAxisControls.UnionWith(
            allBindings.Where(b => b.Trigger.Type != TriggerType.Button)
                       .Select(b => b.Trigger.Control)
        );

        // 4. Ensure previous state tracking exists for all button controls
        foreach (var control in _knownButtonControls)
        {
            _prevButtons.TryAdd(control, false);
        }
    }

    private void RebuildActiveMapsSorted()
    {
        // 1. Efficiently copy values to the array
        if (_activeMapsSorted.Length != _activeMaps.Count)
        {
            _activeMapsSorted = new ActiveMapEntry[_activeMaps.Count];
        }

        int i = 0;
        foreach (var entry in _activeMaps.Values)
        {
            var priority = entry.PriorityOverride ??
                          (_profile.Maps.TryGetValue(entry.MapId.Name, out var def) ? def.Priority : 0);

            _activeMapsSorted[i++] = entry with { ResolvedPriority = priority };
        }

        // 2. Sort in-place to avoid LINQ overhead
        Array.Sort(_activeMapsSorted, (a, b) => b.ResolvedPriority.CompareTo(a.ResolvedPriority));

        _activeMapsDirty = false;
    }

    private static float Clamp(float v, float min, float max)
    {
        if (v < min) return min;
        if (v > max) return max;
        return v;
    }

    private readonly record struct ActionState(bool Down, bool PressedThisFrame, bool ReleasedThisFrame);

    private readonly record struct ActiveMapEntry(ActionMapId MapId, int RefCount, int? PriorityOverride)
    {
        public int ResolvedPriority { get; init; }
    }
}
