using System.Collections.ObjectModel;
using System.Numerics;

namespace InputMan.Core;

public enum ActionValue
{
    Up,
    Pressed,
    Held,
    Released,
}

public readonly record struct ActiveInputDevice(DeviceKind Kind, byte DeviceIndex);

/// <summary>Immutable logical output for one deterministic engine tick.</summary>
public sealed class InputFrame
{
    public InputFrame(
        long sequence,
        float deltaTimeSeconds,
        IReadOnlyDictionary<ActionId, ActionValue> actions,
        IReadOnlyDictionary<AxisId, float> axes,
        IReadOnlyDictionary<Axis2Id, Vector2> axes2,
        ActiveInputDevice? lastActiveDevice)
    {
        Sequence = sequence;
        DeltaTimeSeconds = deltaTimeSeconds;
        Actions = new ReadOnlyDictionary<ActionId, ActionValue>(new Dictionary<ActionId, ActionValue>(actions));
        Axes = new ReadOnlyDictionary<AxisId, float>(new Dictionary<AxisId, float>(axes));
        Axes2 = new ReadOnlyDictionary<Axis2Id, Vector2>(new Dictionary<Axis2Id, Vector2>(axes2));
        LastActiveDevice = lastActiveDevice;
    }

    public long Sequence { get; }
    public float DeltaTimeSeconds { get; }
    public IReadOnlyDictionary<ActionId, ActionValue> Actions { get; }
    public IReadOnlyDictionary<AxisId, float> Axes { get; }
    public IReadOnlyDictionary<Axis2Id, Vector2> Axes2 { get; }
    public ActiveInputDevice? LastActiveDevice { get; }

    public bool WasPressed(ActionId action) => Actions.GetValueOrDefault(action) == ActionValue.Pressed;
    public bool IsDown(ActionId action) => Actions.GetValueOrDefault(action) is ActionValue.Pressed or ActionValue.Held;
    public bool WasReleased(ActionId action) => Actions.GetValueOrDefault(action) == ActionValue.Released;
    public float GetAxis(AxisId axis) => Axes.GetValueOrDefault(axis);
    public Vector2 GetAxis2(Axis2Id axis) => Axes2.GetValueOrDefault(axis);
}
