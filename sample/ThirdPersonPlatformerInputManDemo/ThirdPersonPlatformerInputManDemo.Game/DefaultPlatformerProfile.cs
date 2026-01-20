using System.Collections.Generic;
using InputMan.Core;
using Stride.Input;
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
                    ButtonAxis(K(Keys.A), MoveX, -1),
                    ButtonAxis(K(Keys.D), MoveX, +1),
                    ButtonAxis(K(Keys.S), MoveY, -1),
                    ButtonAxis(K(Keys.W), MoveY, +1),

                    // --- Jump ---
                    Action(K(Keys.Space), Jump, ButtonEdge.Pressed),

                    // --- Mouse look (delta axes) ---
                    DeltaAxis(MouseDeltaX, LookMouseX, +1),
                    DeltaAxis(MouseDeltaY, LookMouseY, +1),

                    // --- Mouse Lock ---
                    Action(M(MouseButton.Left), LookLock, ButtonEdge.Down),
                    Action(K(Keys.Escape), LookUnlock, ButtonEdge.Pressed),

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

            return new InputProfile
            {
                Maps = new Dictionary<string, ActionMapDefinition>
                {
                    ["Gameplay"] = gameplay
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