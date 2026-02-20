using InputMan.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace InputMan.MonoGameConn;

/// <summary>
/// Helper methods for building candidate button lists for rebinding in MonoGame.
/// </summary>
public static class MonoGameCandidateButtons
{
    /// <summary>
    /// Returns all standard keyboard keys suitable for rebinding.
    /// </summary>
    public static List<ControlKey> AllKeyboardKeys()
    {
        var keys = new List<ControlKey>();

        // Letter keys
        for (var key = Keys.A; key <= Keys.Z; key++)
            keys.Add(MonoGameKeys.K(key));

        // Number keys
        for (var key = Keys.D0; key <= Keys.D9; key++)
            keys.Add(MonoGameKeys.K(key));

        // Function keys
        for (var key = Keys.F1; key <= Keys.F12; key++)
            keys.Add(MonoGameKeys.K(key));

        // Common special keys
        keys.Add(MonoGameKeys.K(Keys.Space));
        keys.Add(MonoGameKeys.K(Keys.Enter));
        keys.Add(MonoGameKeys.K(Keys.Tab));
        keys.Add(MonoGameKeys.K(Keys.LeftShift));
        keys.Add(MonoGameKeys.K(Keys.RightShift));
        keys.Add(MonoGameKeys.K(Keys.LeftControl));
        keys.Add(MonoGameKeys.K(Keys.RightControl));
        keys.Add(MonoGameKeys.K(Keys.LeftAlt));
        keys.Add(MonoGameKeys.K(Keys.RightAlt));

        // Arrow keys
        keys.Add(MonoGameKeys.K(Keys.Left));
        keys.Add(MonoGameKeys.K(Keys.Right));
        keys.Add(MonoGameKeys.K(Keys.Up));
        keys.Add(MonoGameKeys.K(Keys.Down));

        return keys;
    }

    /// <summary>
    /// Returns all mouse buttons suitable for rebinding.
    /// </summary>
    public static List<ControlKey> AllMouseButtons()
    {
        return new List<ControlKey>
        {
            MonoGameKeys.M(MonoGameMouseButton.Left),
            MonoGameKeys.M(MonoGameMouseButton.Right),
            MonoGameKeys.M(MonoGameMouseButton.Middle),
            MonoGameKeys.M(MonoGameMouseButton.XButton1),
            MonoGameKeys.M(MonoGameMouseButton.XButton2),
        };
    }

    /// <summary>
    /// Returns all gamepad buttons for a specific player.
    /// </summary>
    public static List<ControlKey> AllGamepadButtons(PlayerIndex player)
    {
        return new List<ControlKey>
        {
            // Face buttons
            MonoGameKeys.PadBtn(player, Buttons.A),
            MonoGameKeys.PadBtn(player, Buttons.B),
            MonoGameKeys.PadBtn(player, Buttons.X),
            MonoGameKeys.PadBtn(player, Buttons.Y),

            // Shoulder buttons
            MonoGameKeys.PadBtn(player, Buttons.LeftShoulder),
            MonoGameKeys.PadBtn(player, Buttons.RightShoulder),

            // D-Pad
            MonoGameKeys.PadBtn(player, Buttons.DPadUp),
            MonoGameKeys.PadBtn(player, Buttons.DPadDown),
            MonoGameKeys.PadBtn(player, Buttons.DPadLeft),
            MonoGameKeys.PadBtn(player, Buttons.DPadRight),

            // Stick clicks
            MonoGameKeys.PadBtn(player, Buttons.LeftStick),
            MonoGameKeys.PadBtn(player, Buttons.RightStick),

            // System buttons
            MonoGameKeys.PadBtn(player, Buttons.Start),
            MonoGameKeys.PadBtn(player, Buttons.Back),
        };
    }

    /// <summary>
    /// Returns keyboard + gamepad buttons (most common for gameplay rebinding).
    /// </summary>
    public static List<ControlKey> KeyboardAndGamepad(PlayerIndex player = PlayerIndex.One)
    {
        var candidates = new List<ControlKey>();
        candidates.AddRange(AllKeyboardKeys());
        candidates.AddRange(AllGamepadButtons(player));
        return candidates;
    }

    /// <summary>
    /// Returns keyboard + mouse buttons (common for aim/look controls).
    /// </summary>
    public static List<ControlKey> KeyboardAndMouse()
    {
        var candidates = new List<ControlKey>();
        candidates.AddRange(AllKeyboardKeys());
        candidates.AddRange(AllMouseButtons());
        return candidates;
    }

    /// <summary>
    /// Returns all available input buttons across all devices.
    /// </summary>
    public static List<ControlKey> AllDevices(PlayerIndex player = PlayerIndex.One)
    {
        var candidates = new List<ControlKey>();
        candidates.AddRange(AllKeyboardKeys());
        candidates.AddRange(AllMouseButtons());
        candidates.AddRange(AllGamepadButtons(player));
        return candidates;
    }
}
