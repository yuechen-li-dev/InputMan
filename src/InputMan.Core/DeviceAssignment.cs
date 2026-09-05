namespace InputMan.Core;

/// <summary>Bounded local-player filtering; connection ownership and join policy remain application concerns.</summary>
public sealed record InputDeviceAssignment(
    int PlayerIndex,
    bool IncludeKeyboardAndMouse,
    IReadOnlySet<byte> Gamepads)
{
    public bool Includes(ControlKey control)
    {
        return control.Device switch
        {
            DeviceKind.Keyboard or DeviceKind.Mouse => IncludeKeyboardAndMouse,
            DeviceKind.Gamepad => Gamepads.Contains(control.DeviceIndex),
            _ => false,
        };
    }

    public InputSnapshot Filter(InputSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new InputSnapshot(
            snapshot.Buttons.Where(pair => Includes(pair.Key)).ToDictionary(),
            snapshot.Axes.Where(pair => Includes(pair.Key)).ToDictionary());
    }
}

public static class CandidateControls
{
    public static IReadOnlyList<ControlKey> Keyboard()
    {
        return Enum.GetValues<KeyboardKey>()
            .Where(key => key != KeyboardKey.Unknown)
            .Select(Controls.Key)
            .ToArray();
    }

    public static IReadOnlyList<ControlKey> MouseButtons()
    {
        return Enum.GetValues<MouseButton>().Select(Controls.Mouse).ToArray();
    }

    public static IReadOnlyList<ControlKey> StandardGamepadButtons(byte deviceIndex = 0)
    {
        return Enum.GetValues<GamepadButton>().Select(button => Controls.Gamepad(button, deviceIndex)).ToArray();
    }

    public static IReadOnlyList<ControlKey> KeyboardMouseAndGamepad(byte deviceIndex = 0)
    {
        return [.. Keyboard(), .. MouseButtons(), .. StandardGamepadButtons(deviceIndex)];
    }
}

public readonly record struct BindingPrompt(string BindingName, string Label, DeviceKind Device, byte DeviceIndex);

public static class BindingPrompts
{
    public static IReadOnlyList<BindingPrompt> ForAction(InputProfile profile, ActionId action)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return profile.Maps.Values
            .SelectMany(map => map.Bindings)
            .Where(binding => binding.Output is ActionOutput output && output.Action == action)
            .OrderBy(binding => binding.Name, StringComparer.Ordinal)
            .Select(binding => new BindingPrompt(
                binding.Name,
                ControlPath.Format(binding.Trigger.Control, binding.Trigger.Type),
                binding.Trigger.Control.Device,
                binding.Trigger.Control.DeviceIndex))
            .ToArray();
    }
}
