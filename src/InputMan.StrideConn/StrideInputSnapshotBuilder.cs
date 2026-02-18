using InputMan.Core;
using Stride.Input;

namespace InputMan.StrideConn;

public static class StrideInputSnapshotBuilder
{
    public static InputSnapshot Build(
        InputManager input,
        IReadOnlyCollection<ControlKey> watchedButtons,
        IReadOnlyCollection<ControlKey> watchedAxes)
    => Build(new StrideInputSource(input), watchedButtons, watchedAxes);

    public static InputSnapshot Build(
    IStrideInputSource input,
    IReadOnlyCollection<ControlKey> watchedButtons,
    IReadOnlyCollection<ControlKey> watchedAxes)
    {
        var buttons = new Dictionary<ControlKey, bool>(watchedButtons.Count);
        var axes = new Dictionary<ControlKey, float>(Math.Max(16, watchedAxes.Count));

        // Buttons
        foreach (var key in watchedButtons)
        {
            if (TryReadButton(input, key, out var down) && down)
                buttons[key] = true; // sparse: only store true
        }

        // Axes
        foreach (var key in watchedAxes)
        {
            if (TryReadAxis(input, key, out var value) && value != 0f)
                axes[key] = value; // sparse: only store non-zero
        }

        return new InputSnapshot(buttons, axes);
    }

    private static bool TryReadButton(IStrideInputSource input, in ControlKey key, out bool down)
    {
        down = false;

        return key.Device switch
        {
            DeviceKind.Keyboard => ReadKeyboard(input, key, out down),
            DeviceKind.Mouse => ReadMouseButton(input, key, out down),
            DeviceKind.Gamepad => ReadGamepadButton(input, key, out down),
            _ => false
        };
    }

    private static bool TryReadAxis(IStrideInputSource input, in ControlKey key, out float value)
    {
        value = 0f;

        return key.Device switch
        {
            DeviceKind.Mouse => ReadMouseAxis(input, key, out value),
            DeviceKind.Gamepad => ReadGamepadAxis(input, key, out value),
            _ => false
        };
    }

    private static bool ReadKeyboard(IStrideInputSource input, in ControlKey key, out bool down)
    {
        down = input.IsKeyDown((Keys)key.Code);
        return true;
    }

    private static bool ReadMouseButton(IStrideInputSource input, in ControlKey key, out bool down)
    {
        // For mouse BUTTONS, code is the Stride.MouseButton enum int.
        down = input.IsMouseButtonDown((MouseButton)key.Code);
        return true;
    }

    private static bool ReadMouseAxis(IStrideInputSource input, in ControlKey key, out float value)
    {
        // For mouse AXES, code is from StrideControlCodes.
        value = key.Code switch
        {
            StrideControlCodes.MouseDeltaX => input.MouseDelta.X,
            StrideControlCodes.MouseDeltaY => input.MouseDelta.Y,
            StrideControlCodes.MouseWheelDelta => GetMouseWheelDelta(input),
            _ => 0f
        };

        return true;
    }

    private static float GetMouseWheelDelta(IStrideInputSource input)
    {
        // Stride versions differ slightly. If your InputManager exposes MouseWheelDelta as a float, use it.
        // If it’s an int, cast. If it’s a Vector2 or something else, adjust here.
        // Common case:
        return input.MouseWheelDelta;
    }

    private static bool ReadGamepadButton(IStrideInputSource input, in ControlKey key, out bool down)
    {
        if (!input.TryGetGamePadState(key.DeviceIndex, out var st))
        {
            down = false;
            return true; // not connected => not down
        }

        var button = (GamePadButton)key.Code;
        down = (st.Buttons & button) == button;
        return true;
    }

    private static bool ReadGamepadAxis(IStrideInputSource input, in ControlKey key, out float value)
    {
        if (!input.TryGetGamePadState(key.DeviceIndex, out var st))
        {
            value = 0f;
            return true; // not connected => 0
        }

        value = key.Code switch
        {
            StrideControlCodes.GamepadLeftX => st.LeftThumb.X,
            StrideControlCodes.GamepadLeftY => st.LeftThumb.Y,
            StrideControlCodes.GamepadRightX => st.RightThumb.X,
            StrideControlCodes.GamepadRightY => st.RightThumb.Y,
            StrideControlCodes.GamepadLeftTrigger => st.LeftTrigger,
            StrideControlCodes.GamepadRightTrigger => st.RightTrigger,
            _ => 0f
        };

        return true;
    }

}


