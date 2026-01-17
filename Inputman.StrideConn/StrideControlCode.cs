namespace InputMan.StrideConn;

/// <summary>
/// Synthetic axis codes for StrideConn. These live in ControlKey.Code when DeviceKind is Mouse/Gamepad.
/// Keyboard/mouse/gamepad buttons use Stride enum int values directly.
/// </summary>
public static class StrideControlCodes
{
    // Mouse (DeviceKind.Mouse)
    public const int MouseDeltaX = 1001;
    public const int MouseDeltaY = 1002;
    public const int MouseWheelDelta = 1003;

    // Gamepad axes (DeviceKind.Gamepad)
    public const int GamepadLeftX = 2001;
    public const int GamepadLeftY = 2002;
    public const int GamepadRightX = 2003;
    public const int GamepadRightY = 2004;
    public const int GamepadLeftTrigger = 2005;
    public const int GamepadRightTrigger = 2006;
}
