namespace InputMan.Core;

public enum DeviceKind : byte
{
    Keyboard = 1,
    Mouse = 2,
    Gamepad = 3,
    Touch = 4,
    Gesture = 5,
}

/// <summary>
/// Identifies a physical control in a device-agnostic way.
/// StrideConn (or other adapters) are responsible for mapping engine-specific inputs to these keys.
/// </summary>
public readonly record struct ControlKey(DeviceKind Device, byte DeviceIndex, int Code)
{
    public override string ToString() => $"{Device}[{DeviceIndex}]:{Code}";
}

public enum TriggerType : byte
{
    Button = 1,
    Axis = 2,
    DeltaAxis = 3,
}

public enum ButtonEdge : byte
{
    /// <summary>True while held.</summary>
    Down = 0,
    /// <summary>True only on the frame it transitions Up -&gt; Down.</summary>
    Pressed = 1,
    /// <summary>True only on the frame it transitions Down -&gt; Up.</summary>
    Released = 2,
}
