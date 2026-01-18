using System.Collections.Generic;
using InputMan.Core;
using InputMan.StrideConn;
using Stride.Input;

namespace ThirdPersonPlatformerInputManDemo
{
    public static class DefaultPlatformerProfile
    {
        // IDs used by PlayerInput.cs
        public static readonly ActionId Jump = new("Jump");

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
                Bindings = new List<Binding>
                {
                    // --- Move: WASD (buttons -> axes) ---
                    BtnAxis(DeviceKind.Keyboard, 0, (int)Keys.A, MoveX, -1),
                    BtnAxis(DeviceKind.Keyboard, 0, (int)Keys.D, MoveX, +1),
                    BtnAxis(DeviceKind.Keyboard, 0, (int)Keys.S, MoveY, -1),
                    BtnAxis(DeviceKind.Keyboard, 0, (int)Keys.W, MoveY, +1),

                    // --- Jump ---
                    BtnAction(DeviceKind.Keyboard, 0, (int)Keys.Space, Jump),

                    // --- Mouse look (delta axes) ---
                    DeltaAxis(DeviceKind.Mouse, 0, StrideControlCodes.MouseDeltaX, LookMouseX, +1),
                    DeltaAxis(DeviceKind.Mouse, 0, StrideControlCodes.MouseDeltaY, LookMouseY, +1),
                }
            };

            // Multiple gamepads: add bindings for indices 0..3 (MVP).
            // Later, we can make StrideConn dynamically expand these for connected pads.
            for (byte i = 0; i < 4; i++)
            {
                // Move
                gameplay.Bindings.Add(Axis(DeviceKind.Gamepad, i, StrideControlCodes.GamepadLeftX, MoveX, +1));
                gameplay.Bindings.Add(Axis(DeviceKind.Gamepad, i, StrideControlCodes.GamepadLeftY, MoveY, +1));

                // Look (stick)
                gameplay.Bindings.Add(Axis(DeviceKind.Gamepad, i, StrideControlCodes.GamepadRightX, LookStickX, +1));
                gameplay.Bindings.Add(Axis(DeviceKind.Gamepad, i, StrideControlCodes.GamepadRightY, LookStickY, +1));

                // Jump
                gameplay.Bindings.Add(BtnAction(DeviceKind.Gamepad, i, (int)GamePadButton.A, Jump));
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

        private static Binding BtnAction(DeviceKind device, byte deviceIndex, int code, ActionId action)
            => new Binding
            {
                Name = $"{action.Name}:{device}:{deviceIndex}:{code}",
                Trigger = new BindingTrigger
                {
                    Control = new ControlKey(device, deviceIndex, code),
                    Type = TriggerType.Button,
                    ButtonEdge = ButtonEdge.Pressed,
                },
                Output = new ActionOutput(action),
                Consume = ConsumeMode.None
            };

        private static Binding BtnAxis(DeviceKind device, byte deviceIndex, int code, AxisId axis, float scale)
            => new Binding
            {
                Name = $"{axis.Name}:{device}:{deviceIndex}:{code}:{scale}",
                Trigger = new BindingTrigger
                {
                    Control = new ControlKey(device, deviceIndex, code),
                    Type = TriggerType.Button,
                    ButtonEdge = ButtonEdge.Down,
                },
                Output = new AxisOutput(axis, scale),
                Consume = ConsumeMode.None
            };

        private static Binding Axis(DeviceKind device, byte deviceIndex, int code, AxisId axis, float scale)
            => new Binding
            {
                Name = $"{axis.Name}:{device}:{deviceIndex}:{code}",
                Trigger = new BindingTrigger
                {
                    Control = new ControlKey(device, deviceIndex, code),
                    Type = TriggerType.Axis,
                    Threshold = 0f,
                },
                Output = new AxisOutput(axis, scale),
                Consume = ConsumeMode.None
            };

        private static Binding DeltaAxis(DeviceKind device, byte deviceIndex, int code, AxisId axis, float scale)
            => new Binding
            {
                Name = $"{axis.Name}:{device}:{deviceIndex}:{code}:delta",
                Trigger = new BindingTrigger
                {
                    Control = new ControlKey(device, deviceIndex, code),
                    Type = TriggerType.DeltaAxis,
                    Threshold = 0f,
                },
                Output = new AxisOutput(axis, scale),
                Consume = ConsumeMode.None
            };
    }
}
