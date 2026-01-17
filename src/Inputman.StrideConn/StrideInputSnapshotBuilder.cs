using System;
using System.Collections.Generic;
using InputMan.Core;
using Stride.Core.Mathematics;
using Stride.Input;

namespace InputMan.StrideConn;

public static class StrideInputSnapshotBuilder
{
    public static InputSnapshot Build(
        InputManager input,
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

    private static bool TryReadButton(InputManager input, in ControlKey key, out bool down)
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

    private static bool TryReadAxis(InputManager input, in ControlKey key, out float value)
    {
        value = 0f;

        return key.Device switch
        {
            DeviceKind.Mouse => ReadMouseAxis(input, key, out value),
            DeviceKind.Gamepad => ReadGamepadAxis(input, key, out value),
            _ => false
        };
    }

    private static bool ReadKeyboard(InputManager input, in ControlKey key, out bool down)
    {
        down = input.IsKeyDown((Keys)key.Code);
        return true;
    }

    private static bool ReadMouseButton(InputManager input, in ControlKey key, out bool down)
    {
        // For mouse BUTTONS, code is the Stride.MouseButton enum int.
        down = input.IsMouseButtonDown((MouseButton)key.Code);
        return true;
    }

    private static bool ReadMouseAxis(InputManager input, in ControlKey key, out float value)
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

    private static float GetMouseWheelDelta(InputManager input)
    {
        // Stride versions differ slightly. If your InputManager exposes MouseWheelDelta as a float, use it.
        // If it’s an int, cast. If it’s a Vector2 or something else, adjust here.
        // Common case:
        return input.MouseWheelDelta;
    }

    private static bool ReadGamepadButton(InputManager input, in ControlKey key, out bool down)
    {
        // For gamepad BUTTONS, code is Stride.GamePadButton enum int.
        // We use DeviceIndex to select which gamepad.
        var pad = GetGamePadByIndex(input, key.DeviceIndex);
        if (pad == null)
        {
            down = false;
            return true;
        }

        var button = (GamePadButton)key.Code;
        down = (pad.State.Buttons & button) == button;
        return true;
    }

    private static bool ReadGamepadAxis(InputManager input, in ControlKey key, out float value)
    {
        var pad = GetGamePadByIndex(input, key.DeviceIndex);
        if (pad == null)
        {
            value = 0f;
            return true;
        }

        // For gamepad AXES, code is from StrideControlCodes.
        var st = pad.State;
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

    private static IGamePadDevice? GetGamePadByIndex(InputManager input, int index)
    {
        // Stride API: returns IGamePadDevice
        var pad = input.GetGamePadByIndex(index);
        if (pad != null)
            return pad;

        // Fallback if needed (should usually be unnecessary)
        foreach (var gp in input.GamePads)
        {
            if (gp.Index == index)
                return gp;
        }

        return null;
    }
}


