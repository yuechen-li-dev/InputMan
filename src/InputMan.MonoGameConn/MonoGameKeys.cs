using InputMan.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace InputMan.MonoGameConn;

/// <summary>
/// Helper methods for creating InputMan ControlKeys from MonoGame input enums.
/// </summary>
public static class MonoGameKeys
{
    // ==================== Keyboard ====================

    /// <summary>
    /// Creates a ControlKey for a keyboard key.
    /// </summary>
    public static ControlKey K(Keys key)
        => new(DeviceKind.Keyboard, 0, (int)key);

    // ==================== Mouse ====================

    /// <summary>
    /// Creates a ControlKey for a mouse button.
    /// </summary>
    public static ControlKey M(MonoGameMouseButton button)
        => new(DeviceKind.Mouse, 0, (int)button);

    /// <summary>
    /// ControlKey for mouse X position.
    /// </summary>
    public static readonly ControlKey MouseX = new(DeviceKind.Mouse, 0, (int)MonoGameMouseAxis.X);

    /// <summary>
    /// ControlKey for mouse Y position.
    /// </summary>
    public static readonly ControlKey MouseY = new(DeviceKind.Mouse, 0, (int)MonoGameMouseAxis.Y);

    /// <summary>
    /// ControlKey for mouse X delta (frame-to-frame movement).
    /// </summary>
    public static readonly ControlKey MouseDeltaX = new(DeviceKind.Mouse, 0, (int)MonoGameMouseAxis.DeltaX);

    /// <summary>
    /// ControlKey for mouse Y delta (frame-to-frame movement).
    /// </summary>
    public static readonly ControlKey MouseDeltaY = new(DeviceKind.Mouse, 0, (int)MonoGameMouseAxis.DeltaY);

    /// <summary>
    /// ControlKey for mouse scroll wheel value.
    /// </summary>
    public static readonly ControlKey MouseScrollWheel = new(DeviceKind.Mouse, 0, (int)MonoGameMouseAxis.ScrollWheel);

    // ==================== GamePad ====================

    /// <summary>
    /// Creates a ControlKey for a gamepad button.
    /// </summary>
    public static ControlKey PadBtn(PlayerIndex player, Buttons button)
        => new(DeviceKind.Gamepad, (byte)player, (int)button);

    /// <summary>
    /// Creates a ControlKey for gamepad left stick X axis.
    /// </summary>
    public static ControlKey PadLeftX(PlayerIndex player)
        => new(DeviceKind.Gamepad, (byte)player, (int)MonoGameGamePadAxis.LeftStickX);

    /// <summary>
    /// Creates a ControlKey for gamepad left stick Y axis.
    /// </summary>
    public static ControlKey PadLeftY(PlayerIndex player)
        => new(DeviceKind.Gamepad, (byte)player, (int)MonoGameGamePadAxis.LeftStickY);

    /// <summary>
    /// Creates a ControlKey for gamepad right stick X axis.
    /// </summary>
    public static ControlKey PadRightX(PlayerIndex player)
        => new(DeviceKind.Gamepad, (byte)player, (int)MonoGameGamePadAxis.RightStickX);

    /// <summary>
    /// Creates a ControlKey for gamepad right stick Y axis.
    /// </summary>
    public static ControlKey PadRightY(PlayerIndex player)
        => new(DeviceKind.Gamepad, (byte)player, (int)MonoGameGamePadAxis.RightStickY);

    /// <summary>
    /// Creates a ControlKey for gamepad left trigger.
    /// </summary>
    public static ControlKey PadLeftTrigger(PlayerIndex player)
        => new(DeviceKind.Gamepad, (byte)player, (int)MonoGameGamePadAxis.LeftTrigger);

    /// <summary>
    /// Creates a ControlKey for gamepad right trigger.
    /// </summary>
    public static ControlKey PadRightTrigger(PlayerIndex player)
        => new(DeviceKind.Gamepad, (byte)player, (int)MonoGameGamePadAxis.RightTrigger);
}
