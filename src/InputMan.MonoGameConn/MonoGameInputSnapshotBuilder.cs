using InputMan.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace InputMan.MonoGameConn;

/// <summary>
/// Builds InputMan InputSnapshot from MonoGame input states.
/// </summary>
public static class MonoGameInputSnapshotBuilder
{
    public static InputSnapshot Build(
        IReadOnlyCollection<ControlKey> watchedButtons,
        IReadOnlyCollection<ControlKey> watchedAxes,
        ref Point? previousMousePosition)
    {
        var buttons = new Dictionary<ControlKey, bool>();
        var axes = new Dictionary<ControlKey, float>();

        // Get current input states
        var keyboardState = Keyboard.GetState();
        var mouseState = Mouse.GetState();
        var currentMousePosition = new Point(mouseState.X, mouseState.Y);

        // Calculate mouse delta
        Point mouseDelta = Point.Zero;
        if (previousMousePosition.HasValue)
        {
            mouseDelta = new Point(
                currentMousePosition.X - previousMousePosition.Value.X,
                currentMousePosition.Y - previousMousePosition.Value.Y);
        }
        previousMousePosition = currentMousePosition;

        // Poll watched buttons
        foreach (var key in watchedButtons)
        {
            bool isDown = key.Device switch
            {
                DeviceKind.Keyboard => PollKeyboard(keyboardState, key.Code),
                DeviceKind.Mouse => PollMouseButton(mouseState, key.Code),
                DeviceKind.Gamepad => PollGamepadButton(key.DeviceIndex, key.Code),
                _ => false
            };
            buttons[key] = isDown;
        }

        // Poll watched axes
        foreach (var key in watchedAxes)
        {
            float value = key.Device switch
            {
                DeviceKind.Mouse => PollMouseAxis(mouseState, mouseDelta, key.Code),
                DeviceKind.Gamepad => PollGamepadAxis(key.DeviceIndex, key.Code),
                _ => 0f
            };
            axes[key] = value;
        }

        return new InputSnapshot(buttons, axes);
    }

    private static bool PollKeyboard(KeyboardState state, int code)
    {
        var key = (Keys)code;
        return state.IsKeyDown(key);
    }

    private static bool PollMouseButton(MouseState state, int code)
    {
        var button = (MonoGameMouseButton)code;
        return button switch
        {
            MonoGameMouseButton.Left => state.LeftButton == ButtonState.Pressed,
            MonoGameMouseButton.Right => state.RightButton == ButtonState.Pressed,
            MonoGameMouseButton.Middle => state.MiddleButton == ButtonState.Pressed,
            MonoGameMouseButton.XButton1 => state.XButton1 == ButtonState.Pressed,
            MonoGameMouseButton.XButton2 => state.XButton2 == ButtonState.Pressed,
            _ => false
        };
    }

    private static bool PollGamepadButton(byte deviceIndex, int code)
    {
        var playerIndex = (PlayerIndex)deviceIndex;
        var state = GamePad.GetState(playerIndex);

        if (!state.IsConnected)
            return false;

        var button = (Buttons)code;
        return state.IsButtonDown(button);
    }

    private static float PollMouseAxis(MouseState state, Point delta, int code)
    {
        var axis = (MonoGameMouseAxis)code;
        return axis switch
        {
            MonoGameMouseAxis.X => state.X,
            MonoGameMouseAxis.Y => state.Y,
            MonoGameMouseAxis.DeltaX => delta.X,
            MonoGameMouseAxis.DeltaY => delta.Y,
            MonoGameMouseAxis.ScrollWheel => state.ScrollWheelValue,
            _ => 0f
        };
    }

    private static float PollGamepadAxis(byte deviceIndex, int code)
    {
        var playerIndex = (PlayerIndex)deviceIndex;
        var state = GamePad.GetState(playerIndex);

        if (!state.IsConnected)
            return 0f;

        var axis = (MonoGameGamePadAxis)code;
        return axis switch
        {
            MonoGameGamePadAxis.LeftStickX => state.ThumbSticks.Left.X,
            MonoGameGamePadAxis.LeftStickY => state.ThumbSticks.Left.Y,
            MonoGameGamePadAxis.RightStickX => state.ThumbSticks.Right.X,
            MonoGameGamePadAxis.RightStickY => state.ThumbSticks.Right.Y,
            MonoGameGamePadAxis.LeftTrigger => state.Triggers.Left,
            MonoGameGamePadAxis.RightTrigger => state.Triggers.Right,
            _ => 0f
        };
    }
}

/// <summary>
/// Mouse button identifiers for InputMan control keys.
/// </summary>
public enum MonoGameMouseButton
{
    Left = 0,
    Right = 1,
    Middle = 2,
    XButton1 = 3,
    XButton2 = 4
}

/// <summary>
/// Mouse axis identifiers for InputMan control keys.
/// </summary>
public enum MonoGameMouseAxis
{
    X = 100,
    Y = 101,
    DeltaX = 102,
    DeltaY = 103,
    ScrollWheel = 104
}

/// <summary>
/// GamePad axis identifiers for InputMan control keys.
/// </summary>
public enum MonoGameGamePadAxis
{
    LeftStickX = 200,
    LeftStickY = 201,
    RightStickX = 202,
    RightStickY = 203,
    LeftTrigger = 204,
    RightTrigger = 205
}
