using InputMan.Core;
using Stride.Input;

namespace InputMan.StrideConn;

/// <summary>
/// Stride-specific helpers that convert Stride enums / synthetic codes into ControlKey.
/// Keeps Stride enums out of InputMan.Core.
/// </summary>
public static class StrideKeys
{
    // Keyboard + mouse buttons use Stride enum values directly.
    public static ControlKey K(Keys key) => new(DeviceKind.Keyboard, 0, (int)key);
    public static ControlKey M(MouseButton btn) => new(DeviceKind.Mouse, 0, (int)btn);

    // Gamepad buttons use Stride enum values directly.
    public static ControlKey PadBtn(byte index, GamePadButton btn) => new(DeviceKind.Gamepad, index, (int)btn);

    // Gamepad/mouse analog axes use StrideConn synthetic axis codes.
    public static ControlKey MouseDeltaX => new(DeviceKind.Mouse, 0, StrideControlCodes.MouseDeltaX);
    public static ControlKey MouseDeltaY => new(DeviceKind.Mouse, 0, StrideControlCodes.MouseDeltaY);
    public static ControlKey MouseWheelDelta => new(DeviceKind.Mouse, 0, StrideControlCodes.MouseWheelDelta);

    public static ControlKey PadAxis(byte index, int axisCode) => new(DeviceKind.Gamepad, index, axisCode);

    // Convenience named axes (optional sugar)
    public static ControlKey PadLeftX(byte i) => PadAxis(i, StrideControlCodes.GamepadLeftX);
    public static ControlKey PadLeftY(byte i) => PadAxis(i, StrideControlCodes.GamepadLeftY);
    public static ControlKey PadRightX(byte i) => PadAxis(i, StrideControlCodes.GamepadRightX);
    public static ControlKey PadRightY(byte i) => PadAxis(i, StrideControlCodes.GamepadRightY);
    public static ControlKey PadLeftTrigger(byte i) => PadAxis(i, StrideControlCodes.GamepadLeftTrigger);
    public static ControlKey PadRightTrigger(byte i) => PadAxis(i, StrideControlCodes.GamepadRightTrigger);
}
