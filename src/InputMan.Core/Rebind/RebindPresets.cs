using System;
using System.Collections.Generic;

namespace InputMan.Core.Rebind;

public static class RebindPresets
{
    // Cache common sets (do NOT mutate these)
    private static readonly IReadOnlySet<DeviceKind> KeyboardGamepad =
        new HashSet<DeviceKind> { DeviceKind.Keyboard, DeviceKind.Gamepad };

    private static readonly IReadOnlySet<DeviceKind> KeyboardMouseGamepad =
        new HashSet<DeviceKind> { DeviceKind.Keyboard, DeviceKind.Mouse, DeviceKind.Gamepad };

    private static readonly IReadOnlySet<DeviceKind> KeyboardMouse =
        new HashSet<DeviceKind> { DeviceKind.Keyboard, DeviceKind.Mouse };

    public static RebindRequest GameplayButton(ActionMapId map, string bindingNameOrSlot)
        => new()
        {
            Map = map,
            BindingNameOrSlot = bindingNameOrSlot,

            AllowedDevices = KeyboardGamepad,
            DisallowConflictsInSameMap = true,

            ExcludeMouseMotion = true,
            Timeout = TimeSpan.FromSeconds(10),
        };

    public static RebindRequest UiButton(ActionMapId map, string bindingNameOrSlot)
        => new()
        {
            Map = map,
            BindingNameOrSlot = bindingNameOrSlot,

            AllowedDevices = KeyboardGamepad,
            DisallowConflictsInSameMap = true,

            ExcludeMouseMotion = true,
            Timeout = TimeSpan.FromSeconds(10),
        };

    public static RebindRequest MouseAllowedButton(ActionMapId map, string bindingNameOrSlot)
        => new()
        {
            Map = map,
            BindingNameOrSlot = bindingNameOrSlot,

            AllowedDevices = KeyboardMouse,
            DisallowConflictsInSameMap = true,

            ExcludeMouseMotion = true,
            Timeout = TimeSpan.FromSeconds(10),
        };

    // For "look lock" or "fire": include mouse + gamepad
    public static RebindRequest AnyButton(ActionMapId map, string bindingNameOrSlot)
        => new()
        {
            Map = map,
            BindingNameOrSlot = bindingNameOrSlot,

            AllowedDevices = KeyboardMouseGamepad,
            DisallowConflictsInSameMap = true,

            ExcludeMouseMotion = true,
            Timeout = TimeSpan.FromSeconds(10),
        };
}
