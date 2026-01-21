using System.Collections.Generic;
using InputMan.Core;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Input;
using ThirdPersonPlatformerInputManDemo.Core;
using Vector2 = Stride.Core.Mathematics.Vector2;
using Vector3 = Stride.Core.Mathematics.Vector3;


namespace ThirdPersonPlatformerInputManDemo.Player
{
    public class PlayerInput : SyncScript
    {
        /// <summary>
        /// Raised every frame with the intended direction of movement from the player.
        /// </summary>
        // TODO Should not be static, but allow binding between player and controller
        public static readonly EventKey<Vector3> MoveDirectionEventKey = new();
        public static readonly EventKey<Vector2> CameraDirectionEventKey = new();
        public static readonly EventKey<bool> JumpEventKey = new();

        // InputMan IDs (cache these so we don't allocate each frame)
        private static readonly Axis2Id MoveAxis = new("Move");
        private static readonly Axis2Id LookStickAxis = new("LookStick");
        private static readonly Axis2Id LookMouseAxis = new("LookMouse");
        private static readonly ActionId JumpAction = new("Jump");
        private static readonly ActionId LookLockAction = new("LookLock");   // e.g. LMB
        private static readonly ActionId LookUnlockAction = new("LookUnlock"); // e.g. Escape
        
        private static readonly ActionMapId GameplayMap = new("Gameplay");
        private static readonly ActionMapId UIMap = new("UI");

        private static readonly ActionId PauseAction = new("Pause");
        private bool _paused;

        private IInputMan _inputMan = null!;

        public float DeadZone { get; set; } = 0.25f;

        public CameraComponent Camera { get; set; }



        /// <summary>
        /// Multiplies mouse delta rotation by this amount.
        /// </summary>
        public float MouseSensitivity = 1f;

        public override void Start()
        {
            // don't throw here; allow InstallInputMan to win the race
            _inputMan = Game.Services.GetService<IInputMan>();
        }

        public override void Update()
        {
            _inputMan ??= Game.Services.GetService<IInputMan>();
            if (_inputMan == null)
                return;

            var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;

            // 1) Character movement: camera-aware
            {
                // Move is logic-space X/Y (left/right, forward/back), already aggregated from bindings.
                var moveDirection = _inputMan.GetAxis2(MoveAxis);

                // Keep template behavior: apply deadzone shaping + rescale magnitude
                var moveLength = moveDirection.Length();
                if (moveLength < DeadZone)
                {
                    MoveDirectionEventKey.Broadcast(Vector3.Zero);
                }
                else
                {
                    if (moveLength > 1f)
                        moveLength = 1f;
                    else
                        moveLength = (moveLength - DeadZone) / (1f - DeadZone);

                    // Convert to world based on camera
                    var worldSpeed = (Camera != null)
                        ? Utils.LogicDirectionToWorldDirection(moveDirection, Camera, Vector3.UnitY)
                        : new Vector3(moveDirection.X, 0, moveDirection.Y);

                    // Template expects worldSpeed normalized before applying magnitude
                    if (worldSpeed.LengthSquared() > 0f)
                        worldSpeed.Normalize();

                    worldSpeed *= moveLength;
                    MoveDirectionEventKey.Broadcast(worldSpeed);
                }
            }

            // 2) Camera rotation: stick + mouse delta (mouse only when locked)
            {
                var stickN = _inputMan.GetAxis2(LookStickAxis); // System.Numerics.Vector2
                var stick = new Vector2(stickN.X, stickN.Y);

                // Right stick: normalize (constant speed while tilted), then scale by dt
                if (stick.Length() < DeadZone)
                {
                    stick = Vector2.Zero;
                }
                else
                {
                    stick.Normalize();
                    stick *= dt;
                }

                // Mouse lock/unlock UX stays on Stride Input
                // Cursor lock/unlock is still an engine/window side-effect,
                // but the *decision* comes from InputMan actions.
                if (_inputMan.WasPressed(LookLockAction))
                {
                    Input.LockMousePosition(true);
                    Game.IsMouseVisible = false;
                }
                if (_inputMan.WasPressed(LookUnlockAction))
                {
                    Input.UnlockMousePosition();
                    Game.IsMouseVisible = true;
                }

                // Mouse delta: only applied when locked. Also invert Y like original code.
                var mouse = Vector2.Zero;
                if (Input.IsMousePositionLocked)
                {
                    var md = _inputMan.GetAxis2(LookMouseAxis);
                    mouse = new Vector2(md.X, -md.Y) * MouseSensitivity;
                }

                var cameraDirection = stick + mouse;
                CameraDirectionEventKey.Broadcast(cameraDirection);
            }

            // 3) Jump: just pressed edge
            {
                var didJump = _inputMan.WasPressed(JumpAction);
                JumpEventKey.Broadcast(didJump);
            }

            // 4) Pause
            {                
                if (_inputMan.WasPressed(PauseAction))
                {
                    _paused = !_paused;

                    if (_paused)
                    {
                        _inputMan.SetMaps(UIMap); // only UI active
                        Input.UnlockMousePosition();
                        Game.IsMouseVisible = true;
                    }
                    else
                    {
                        _inputMan.SetMaps(GameplayMap); // back to gameplay
                                                        // (optional) re-lock if you want
                    }
                }
            }
        }
    }
}
