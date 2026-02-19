using InputMan.Core;
using InputMan.Core.Rebind;
using Stride.Input;

namespace InputMan.StrideConn;

/// <summary>
/// Helper for building candidate button lists for rebinding in Stride.
/// Provides static methods to enumerate keyboard, mouse, and gamepad controls.
/// </summary>
public static class StrideCandidateButtons
{
    /// <summary>
    /// Get all keyboard keys as ControlKeys (excludes Keys.None).
    /// </summary>
    public static List<ControlKey> AllKeyboardKeys()
    {
        return [.. Enum.GetValues<Keys>()
            .Where(k => k != Keys.None)
            .Select(k => new ControlKey(DeviceKind.Keyboard, 0, (int)k))];
    }

    /// <summary>
    /// Get all standard mouse buttons as ControlKeys.
    /// </summary>
    public static List<ControlKey> AllMouseButtons()
    {
        return
        [
            new(DeviceKind.Mouse, 0, (int)MouseButton.Left),
            new(DeviceKind.Mouse, 0, (int)MouseButton.Right),
            new(DeviceKind.Mouse, 0, (int)MouseButton.Middle),
            new(DeviceKind.Mouse, 0, (int)MouseButton.Extended1),
            new(DeviceKind.Mouse, 0, (int)MouseButton.Extended2),
        ];
    }

    /// <summary>
    /// Build a candidate button list based on a RebindRequest's allowed devices.
    /// This is the smart helper that looks at AllowedDevices and builds the appropriate list.
    /// </summary>
    /// <param name="request">The rebind request to build candidates for.</param>
    /// <returns>List of candidate buttons matching the request's allowed devices.</returns>
    public static List<ControlKey> ForRequest(RebindRequest request)
    {
        var candidates = new List<ControlKey>();

        // If no device restrictions, allow keyboard + gamepad (common default)
        if (request.AllowedDevices == null)
        {
            candidates.AddRange(AllKeyboardKeys());
            // Note: Gamepad buttons are auto-detected by InputMan, no need to enumerate
            return candidates;
        }

        // Add keyboard if allowed
        if (request.AllowedDevices.Contains(DeviceKind.Keyboard))
        {
            candidates.AddRange(AllKeyboardKeys());
        }

        // Add mouse if allowed
        if (request.AllowedDevices.Contains(DeviceKind.Mouse))
        {
            candidates.AddRange(AllMouseButtons());
        }

        // Note: Gamepad buttons are automatically detected by InputMan's rebinding system,
        // so we don't need to manually enumerate all possible gamepad buttons.
        // The system will detect whatever gamepad button the user presses.

        return candidates;
    }

    /// <summary>
    /// Build candidate buttons for a standard keyboard + gamepad binding (no mouse).
    /// This is the most common case for gameplay actions.
    /// </summary>
    public static List<ControlKey> KeyboardAndGamepad()
    {
        // Just keyboard - gamepad is auto-detected
        return AllKeyboardKeys();
    }

    /// <summary>
    /// Build candidate buttons for keyboard + mouse binding.
    /// Common for look controls, fire buttons, etc.
    /// </summary>
    public static List<ControlKey> KeyboardAndMouse()
    {
        var candidates = AllKeyboardKeys();
        candidates.AddRange(AllMouseButtons());
        return candidates;
    }

    /// <summary>
    /// Build candidate buttons for keyboard + mouse + gamepad (all devices).
    /// </summary>
    public static List<ControlKey> AllDevices()
    {
        var candidates = AllKeyboardKeys();
        candidates.AddRange(AllMouseButtons());
        // Gamepad auto-detected
        return candidates;
    }
}