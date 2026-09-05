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

/// <summary>Portable keyboard identities owned by InputMan, not an engine adapter.</summary>
public enum KeyboardKey
{
    Unknown = 0,
    A = 4,
    B = 5,
    C = 6,
    D = 7,
    E = 8,
    F = 9,
    I = 12,
    N = 17,
    Q = 20,
    S = 22,
    W = 26,
    Number1 = 30,
    Number2 = 31,
    Number3 = 32,
    Number4 = 33,
    Number5 = 34,
    Number6 = 35,
    Number7 = 36,
    Number8 = 37,
    Enter = 40,
    Escape = 41,
    Space = 44,
    ArrowRight = 79,
    ArrowLeft = 80,
    ArrowDown = 81,
    ArrowUp = 82,
    LeftControl = 224,
    LeftShift = 225,
    LeftAlt = 226,
    RightControl = 228,
    RightShift = 229,
    RightAlt = 230,
}

public enum MouseButton
{
    Primary = 1,
    Secondary = 2,
    Middle = 3,
    Back = 4,
    Forward = 5,
}

public enum MouseAxis
{
    PositionX = 1,
    PositionY = 2,
    DeltaX = 3,
    DeltaY = 4,
    WheelX = 5,
    WheelY = 6,
}

public enum GamepadButton
{
    South = 1,
    East = 2,
    West = 3,
    North = 4,
    LeftShoulder = 5,
    RightShoulder = 6,
    Back = 7,
    Start = 8,
    Guide = 9,
    LeftStick = 10,
    RightStick = 11,
    DpadUp = 12,
    DpadDown = 13,
    DpadLeft = 14,
    DpadRight = 15,
}

public enum GamepadAxis
{
    LeftX = 1,
    LeftY = 2,
    RightX = 3,
    RightY = 4,
    LeftTrigger = 5,
    RightTrigger = 6,
}

/// <summary>Discoverable factories for portable physical controls.</summary>
public static class Controls
{
    public static ControlKey Key(KeyboardKey key) => new(DeviceKind.Keyboard, 0, (int)key);
    public static ControlKey Mouse(MouseButton button) => new(DeviceKind.Mouse, 0, (int)button);
    public static ControlKey Mouse(MouseAxis axis) => new(DeviceKind.Mouse, 0, (int)axis);
    public static ControlKey Gamepad(GamepadButton button, byte deviceIndex = 0) => new(DeviceKind.Gamepad, deviceIndex, (int)button);
    public static ControlKey Gamepad(GamepadAxis axis, byte deviceIndex = 0) => new(DeviceKind.Gamepad, deviceIndex, (int)axis);
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
