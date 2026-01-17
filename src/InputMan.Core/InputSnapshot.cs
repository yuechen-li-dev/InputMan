using System.Collections.Generic;

namespace InputMan.Core;

/// <summary>
/// Immutable(ish) input snapshot for a single frame.
/// Provide only current values; Core computes edges based on previous frame.
/// </summary>
public sealed class InputSnapshot(
    IReadOnlyDictionary<ControlKey, bool>? buttons = null,
    IReadOnlyDictionary<ControlKey, float>? axes = null)
{
    public static readonly InputSnapshot Empty = new();

    private readonly IReadOnlyDictionary<ControlKey, bool> _buttons = buttons ?? new Dictionary<ControlKey, bool>();
    private readonly IReadOnlyDictionary<ControlKey, float> _axes = axes ?? new Dictionary<ControlKey, float>();

    public bool TryGetButton(in ControlKey key, out bool down) => _buttons.TryGetValue(key, out down);
    public bool TryGetAxis(in ControlKey key, out float value) => _axes.TryGetValue(key, out value);

    public IReadOnlyDictionary<ControlKey, bool> Buttons => _buttons;
    public IReadOnlyDictionary<ControlKey, float> Axes => _axes;
}
