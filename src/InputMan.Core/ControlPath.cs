namespace InputMan.Core;

/// <summary>Stable human-readable physical control names used by profile formats and UI.</summary>
public static class ControlPath
{
    public static string Format(ControlKey control) => Format(control, TriggerType.Button);

    public static string Format(ControlKey control, TriggerType triggerType)
    {
        string name = control.Device switch
        {
            DeviceKind.Keyboard => Enum.GetName((KeyboardKey)control.Code) ?? control.Code.ToString(),
            DeviceKind.Mouse when triggerType == TriggerType.Button => Enum.GetName((MouseButton)control.Code) ?? control.Code.ToString(),
            DeviceKind.Mouse => Enum.GetName((MouseAxis)control.Code) ?? control.Code.ToString(),
            DeviceKind.Gamepad when triggerType == TriggerType.Button => Enum.GetName((GamepadButton)control.Code) ?? control.Code.ToString(),
            DeviceKind.Gamepad => Enum.GetName((GamepadAxis)control.Code) ?? control.Code.ToString(),
            _ => control.Code.ToString(),
        };

        return $"{control.Device}.{control.DeviceIndex}.{name}";
    }

    public static ControlKey Parse(string value, TriggerType triggerType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        string[] parts = value.Split('.', StringSplitOptions.TrimEntries);
        if (parts.Length != 3 || !Enum.TryParse(parts[0], true, out DeviceKind device) || !byte.TryParse(parts[1], out byte index))
        {
            throw new FormatException($"Invalid control path '{value}'. Expected Device.Index.Control.");
        }

        int code = device switch
        {
            DeviceKind.Keyboard => ParseCode<KeyboardKey>(parts[2]),
            DeviceKind.Mouse when triggerType == TriggerType.Button => ParseCode<MouseButton>(parts[2]),
            DeviceKind.Mouse => ParseCode<MouseAxis>(parts[2]),
            DeviceKind.Gamepad when triggerType == TriggerType.Button => ParseCode<GamepadButton>(parts[2]),
            DeviceKind.Gamepad => ParseCode<GamepadAxis>(parts[2]),
            _ when int.TryParse(parts[2], out int numeric) => numeric,
            _ => throw new FormatException($"Unsupported control path '{value}'."),
        };

        return new ControlKey(device, index, code);
    }

    private static int ParseCode<T>(string value) where T : struct, Enum
    {
        if (Enum.TryParse(value, true, out T parsed))
        {
            return Convert.ToInt32(parsed);
        }
        if (int.TryParse(value, out int numeric))
        {
            return numeric;
        }
        throw new FormatException($"Unknown {typeof(T).Name} '{value}'.");
    }
}
