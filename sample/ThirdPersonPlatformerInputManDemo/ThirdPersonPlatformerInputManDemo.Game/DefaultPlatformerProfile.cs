using InputMan.Core;
using InputMan.StrideConn;
using Stride.Input;
using System.Collections.Generic;
using static InputMan.Core.Bind;
using static InputMan.StrideConn.StrideKeys;

namespace ThirdPersonPlatformerInputManDemo
{
    public static class DefaultPlatformerProfile
    {
        // IDs used by PlayerInput.cs
        public static readonly ActionId Jump = new("Jump");

        public static readonly ActionId LookLock = new("LookLock");
        public static readonly ActionId LookUnlock = new("LookUnlock");

        public static readonly AxisId MoveX = new("MoveX");
        public static readonly AxisId MoveY = new("MoveY");

        public static readonly AxisId LookStickX = new("LookStickX");
        public static readonly AxisId LookStickY = new("LookStickY");

        public static readonly AxisId LookMouseX = new("LookMouseX");
        public static readonly AxisId LookMouseY = new("LookMouseY");

        public static readonly Axis2Id Move = new("Move");
        public static readonly Axis2Id LookStick = new("LookStick");
        public static readonly Axis2Id LookMouse = new("LookMouse");

        public static readonly ActionId Pause = new("Pause");
        public static readonly ActionId Confirm = new("Confirm"); // optional
        public static readonly ActionId Cancel = new("Cancel");   // optional

        public static InputProfile Create()
        {
            var gameplay = new ActionMapDefinition
            {
                Id = new ActionMapId("Gameplay"),
                Priority = 10,
                CanConsume = false,
                Bindings =
                [
                    // --- Move: WASD (buttons -> axes) ---
                    ButtonAxis(K(Keys.A), MoveX, -1, name: "MoveLeft.Kb"),
                    ButtonAxis(K(Keys.D), MoveX, +1, name: "MoveRight.Kb"),
                    ButtonAxis(K(Keys.S), MoveY, -1, name: "MoveBack.Kb"),
                    ButtonAxis(K(Keys.W), MoveY, +1, name: "MoveFwd.Kb"),

                    // --- Jump ---
                    Action(K(Keys.Space), Jump, ButtonEdge.Pressed, name: "Jump.Kb"),

                    // --- Mouse look (delta axes) ---
                    DeltaAxis(MouseDeltaX, LookMouseX, +1, name: "LookLeftRight.Kb"),
                    DeltaAxis(MouseDeltaY, LookMouseY, +1, name: "LookUpDown.Kb"),

                    // --- Mouse Lock ---
                    Action(M(MouseButton.Left), LookLock, ButtonEdge.Pressed, name: "Looklock.Mouse"),
                    Action(K(Keys.Escape), LookUnlock, ButtonEdge.Pressed, name: "LookUnlock.Mouse"),

                ]
            };

            // Multiple gamepads: add bindings for indices 0..3 (MVP). Can make StrideConn dynamically expand these for connected pads in the future.
            for (byte i = 0; i < 4; i++)
            {
                // Move
                gameplay.Bindings.Add(Axis(PadLeftX(i), MoveX, +1));
                gameplay.Bindings.Add(Axis(PadLeftY(i), MoveY, +1));
                // Look
                gameplay.Bindings.Add(Axis(PadRightX(i), LookStickX, +1));
                gameplay.Bindings.Add(Axis(PadRightY(i), LookStickY, +1));
                // Jump
                gameplay.Bindings.Add(Action(PadBtn(i, GamePadButton.A), Jump, ButtonEdge.Pressed));
            }

            var ui = new ActionMapDefinition
            {
                Id = new ActionMapId("UI"),
                Priority = 100,
                CanConsume = true,
                Bindings =
                [
                    // Pause toggle (works even when UI is active)
                    Action(K(Keys.Escape), Pause, ButtonEdge.Pressed, consume: ConsumeMode.All),
                    Action(K(Keys.M), Pause, ButtonEdge.Pressed, consume: ConsumeMode.All),
                    Action(PadBtn(0, GamePadButton.Start), Pause, ButtonEdge.Pressed, consume: ConsumeMode.All),

                    // Optional confirm/cancel for menus
                    Action(K(Keys.Enter), Confirm, ButtonEdge.Pressed, consume: ConsumeMode.All),
                    Action(K(Keys.Back), Cancel, ButtonEdge.Pressed, consume: ConsumeMode.All),
                    Action(PadBtn(0, GamePadButton.A), Confirm, ButtonEdge.Pressed, consume: ConsumeMode.All),
                    Action(PadBtn(0, GamePadButton.B), Cancel, ButtonEdge.Pressed, consume: ConsumeMode.All),
                ]
            };

            for (byte i = 0; i < 4; i++)
            {
                ui.Bindings.Add(Action(PadBtn(i, GamePadButton.Start), Pause, ButtonEdge.Pressed, consume: ConsumeMode.All));
                ui.Bindings.Add(Action(PadBtn(i, GamePadButton.A), Confirm, ButtonEdge.Pressed, consume: ConsumeMode.All));
                ui.Bindings.Add(Action(PadBtn(i, GamePadButton.B), Cancel, ButtonEdge.Pressed, consume: ConsumeMode.All));
            }

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
    }
}