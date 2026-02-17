using InputMan.Core;
using InputMan.StrideConn;
using Stride.Input;
using System.Collections.Generic;
using static InputMan.Core.Bind;
using static InputMan.StrideConn.StrideKeys;

namespace ThirdPersonPlatformerInputManDemo;

/// <summary>
/// Default input profile for third-person platformer.
/// Supports keyboard + mouse + up to 4 gamepads.
/// </summary>
public static class DefaultPlatformerProfile
{
    // Action/Axis IDs (referenced by PlayerInput.cs)
    public static readonly ActionId Jump = new("Jump");
    public static readonly ActionId LookLock = new("LookLock");
    public static readonly ActionId LookUnlock = new("LookUnlock");
    public static readonly ActionId Pause = new("Pause");
    public static readonly ActionId Confirm = new("Confirm");
    public static readonly ActionId CancelRebind = new("CancelRebind");

    public static readonly AxisId MoveX = new("MoveX");
    public static readonly AxisId MoveY = new("MoveY");
    public static readonly AxisId LookStickX = new("LookStickX");
    public static readonly AxisId LookStickY = new("LookStickY");
    public static readonly AxisId LookMouseX = new("LookMouseX");
    public static readonly AxisId LookMouseY = new("LookMouseY");

    public static readonly Axis2Id Move = new("Move");
    public static readonly Axis2Id LookStick = new("LookStick");
    public static readonly Axis2Id LookMouse = new("LookMouse");

    public static readonly ActionId RebindJump = new("RebindJump");

    public static InputProfile Create()
    {
        var gameplay = CreateGameplayMap();
        var ui = CreateUIMap();

        return new InputProfile
        {
            Maps = new Dictionary<string, ActionMapDefinition>
            {
                ["Gameplay"] = gameplay,
                ["UI"] = ui,
            },
            Axis2 = new Dictionary<string, Axis2Definition>
            {
                ["Move"] = new Axis2Definition { Id = Move, X = MoveX, Y = MoveY },
                ["LookStick"] = new Axis2Definition { Id = LookStick, X = LookStickX, Y = LookStickY },
                ["LookMouse"] = new Axis2Definition { Id = LookMouse, X = LookMouseX, Y = LookMouseY },
            }
        };
    }

    private static ActionMapDefinition CreateGameplayMap()
    {
        var map = new ActionMapDefinition
        {
            Id = new ActionMapId("Gameplay"),
            Priority = 10,
            CanConsume = false,
            Bindings =
            [
                // Keyboard movement (WASD)
                ButtonAxis(K(Keys.A), MoveX, -1, name: BindingNames.MoveLeftKeyboard),
                ButtonAxis(K(Keys.D), MoveX, +1, name: BindingNames.MoveRightKeyboard),
                ButtonAxis(K(Keys.S), MoveY, -1, name: BindingNames.MoveBackKeyboard),
                ButtonAxis(K(Keys.W), MoveY, +1, name: BindingNames.MoveForwardKeyboard),

                // Keyboard jump
                Action(K(Keys.Space), Jump, ButtonEdge.Pressed, name: BindingNames.JumpKeyboard),

                // Mouse look (delta axes)
                DeltaAxis(MouseDeltaX, LookMouseX, +1, name: "LookLeftRight.Mouse"),
                DeltaAxis(MouseDeltaY, LookMouseY, +1, name: "LookUpDown.Mouse"),

                // Mouse lock/unlock
                Action(M(MouseButton.Left), LookLock, ButtonEdge.Pressed, name: BindingNames.LookLockMouse),
                Action(K(Keys.Escape), LookUnlock, ButtonEdge.Pressed, name: BindingNames.LookUnlockKeyboard),
            ]
        };

        // Add gamepad bindings for up to 4 controllers
        AddGamepadBindings(map, maxPads: 4);

        return map;
    }

    private static ActionMapDefinition CreateUIMap()
    {
        var map = new ActionMapDefinition
        {
            Id = new ActionMapId("UI"),
            Priority = 100,
            CanConsume = true,
            Bindings =
            [
                // Pause (keyboard) - Escape triggers Pause
                Action(K(Keys.Escape), Pause, ButtonEdge.Pressed,
                    ConsumeMode.All, BindingNames.PauseKeyboard1),
                Action(K(Keys.M), Pause, ButtonEdge.Pressed,
                    ConsumeMode.All, BindingNames.PauseKeyboard2),

                // Cancel (keyboard) - Escape ALSO triggers Cancel (for rebinding)
                Action(K(Keys.Escape), CancelRebind, ButtonEdge.Pressed,
                    ConsumeMode.All, name: "Cancel.Kb.Escape"),

                // Confirm (keyboard)
                Action(K(Keys.Enter), Confirm, ButtonEdge.Pressed, ConsumeMode.All),

                // Rebind Jump hotkey
                Action(K(Keys.J), RebindJump, ButtonEdge.Pressed, ConsumeMode.All, name: "RebindJump.Kb"),
            ]
        };

        // Add UI gamepad bindings
        AddGamepadUIBindings(map, maxPads: 4);

        return map;
    }

    /// <summary>
    /// Adds standard gameplay bindings for multiple gamepads.
    /// </summary>
    private static void AddGamepadBindings(
        ActionMapDefinition map,
        int maxPads)
    {
        for (byte i = 0; i < maxPads; i++)
        {
            // Movement (left stick)
            map.Bindings.Add(Axis(PadLeftX(i), MoveX, +1));
            map.Bindings.Add(Axis(PadLeftY(i), MoveY, +1));

            // Camera (right stick)
            map.Bindings.Add(Axis(PadRightX(i), LookStickX, +1));
            map.Bindings.Add(Axis(PadRightY(i), LookStickY, +1));

            // Jump (A button)
            map.Bindings.Add(Action(
                PadBtn(i, GamePadButton.A),
                Jump,
                ButtonEdge.Pressed,
                name: $"Jump.Pad{i}"));
        }
    }

    /// <summary>
    /// Adds UI bindings for multiple gamepads.
    /// </summary>
    private static void AddGamepadUIBindings(
        ActionMapDefinition map,
        int maxPads)
    {
        for (byte i = 0; i < maxPads; i++)
        {
            map.Bindings.Add(Action(
                PadBtn(i, GamePadButton.Start),
                Pause,
                ButtonEdge.Pressed,
                ConsumeMode.All,
                $"Pause.Pad{i}"));

            map.Bindings.Add(Action(
                PadBtn(i, GamePadButton.A),
                Confirm,
                ButtonEdge.Pressed,
                ConsumeMode.All));

            // Gamepad B = Cancel (consistent with Escape on keyboard)
            map.Bindings.Add(Action(
                PadBtn(i, GamePadButton.B),
                CancelRebind,
                ButtonEdge.Pressed,
                ConsumeMode.All));
        }
    }
}