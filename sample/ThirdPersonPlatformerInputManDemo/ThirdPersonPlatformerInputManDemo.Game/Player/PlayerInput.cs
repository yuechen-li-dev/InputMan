using InputMan.Core;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Input;
using System;
using ThirdPersonPlatformerInputManDemo.Core;
using Vector2 = Stride.Core.Mathematics.Vector2;
using Vector3 = Stride.Core.Mathematics.Vector3;

namespace ThirdPersonPlatformerInputManDemo.Player;

/// <summary>
/// Handles player input using InputMan and broadcasts movement/camera/jump events.
/// Fully integrates with InputMan's action/axis system - no manual input polling.
/// </summary>
public class PlayerInput : SyncScript
{
    #region Events

    /// <summary>
    /// Broadcasts the intended movement direction in world space every frame.
    /// </summary>
    /// <remarks>
    /// Movement is camera-relative (forward = camera forward projected onto ground plane).
    /// Magnitude is normalized with deadzone already applied by InputMan.
    /// </remarks>
    public static readonly EventKey<Vector3> MoveDirectionEventKey = new();

    /// <summary>
    /// Broadcasts camera rotation delta every frame (stick + mouse combined).
    /// </summary>
    /// <remarks>
    /// Already scaled by delta time for stick input. Mouse is frame-rate independent.
    /// </remarks>
    public static readonly EventKey<Vector2> CameraDirectionEventKey = new();

    /// <summary>
    /// Broadcasts true on frames where jump was pressed.
    /// </summary>
    public static readonly EventKey<bool> JumpEventKey = new();

    #endregion

    #region InputMan IDs (cached to avoid allocations)

    private static readonly Axis2Id MoveAxis = new("Move");
    private static readonly Axis2Id LookStickAxis = new("LookStick");
    private static readonly Axis2Id LookMouseAxis = new("LookMouse");
    private static readonly ActionId JumpAction = new("Jump");
    private static readonly ActionId LookLockAction = new("LookLock");
    private static readonly ActionId LookUnlockAction = new("LookUnlock");

    #endregion

    #region Configuration

    /// <summary>
    /// Camera component for determining forward direction.
    /// </summary>
    /// <remarks>
    /// Set in the Stride editor. Used to convert 2D input to camera-relative world movement.
    /// </remarks>
    public CameraComponent? Camera { get; set; }

    /// <summary>
    /// Mouse sensitivity multiplier for look rotation.
    /// </summary>
    /// <remarks>
    /// Higher values = faster camera rotation. Default is 1.0.
    /// </remarks>
    public float MouseSensitivity { get; set; } = 1f;

    #endregion

    #region Private State

    private IInputMan? _inputMan;

    #endregion

    #region Stride Lifecycle

    public override void Start()
    {
        // Acquire InputMan service (installed by InstallInputMan startup script)
        _inputMan = Game.Services.GetService<IInputMan>();

        if (_inputMan == null)
        {
            Log.Warning("PlayerInput: IInputMan not found. Input will not work until InstallInputMan runs.");
        }
    }

    public override void Update()
    {
        // Lazy acquisition in case InstallInputMan runs after this script
        _inputMan ??= Game.Services.GetService<IInputMan>();
        if (_inputMan == null)
            return;

        var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;

        ProcessMovement();
        ProcessCameraRotation(dt);
        ProcessJump();
    }

    #endregion

    #region Input Processing

    /// <summary>
    /// Processes movement input and broadcasts camera-relative world direction.
    /// </summary>
    /// <remarks>
    /// InputMan already handles:
    /// - Deadzone (via DeadzoneProcessor in profile for gamepad sticks)
    /// - Input aggregation (WASD + stick combined)
    /// - Analog scaling (stick magnitude preserved after deadzone)
    /// We just need to:
    /// - Convert 2D input to 3D world space
    /// - Make it camera-relative
    /// </remarks>
    private void ProcessMovement()
    {
        // Get movement input (already deadzone-processed and aggregated)
        var moveInput = _inputMan!.GetAxis2(MoveAxis);

        // Check if there's any input (small epsilon for floating point)
        if (moveInput.LengthSquared() < 0.0001f)
        {
            MoveDirectionEventKey.Broadcast(Vector3.Zero);
            return;
        }

        // Convert 2D logic input to 3D world space (camera-relative)
        var worldDirection = Camera != null
            ? Utils.LogicDirectionToWorldDirection(moveInput, Camera, Vector3.UnitY)
            : new Vector3(moveInput.X, 0, moveInput.Y);

        // Normalize direction (removes magnitude)
        if (worldDirection.LengthSquared() > 0f)
            worldDirection.Normalize();

        // Restore magnitude from input (allows analog stick control of speed)
        // InputMan clamps analog axes to [-1, 1] after deadzone processing
        var inputMagnitude = Math.Min(moveInput.Length(), 1f);
        worldDirection *= inputMagnitude;

        MoveDirectionEventKey.Broadcast(worldDirection);
    }

    /// <summary>
    /// Processes camera rotation from stick and mouse inputs.
    /// </summary>
    /// <remarks>
    /// Stick: Constant rotation speed while held (scaled by delta time)
    /// Mouse: Delta-based (already frame-rate independent)
    /// Both inputs have deadzone applied via processors in the profile.
    /// </remarks>
    private void ProcessCameraRotation(float deltaTime)
    {
        // Get stick input (right stick) - deadzone already applied by processor
        var stickInput = _inputMan!.GetAxis2(LookStickAxis);
        var stick = new Vector2(stickInput.X, stickInput.Y);

        // Check for meaningful input (small epsilon for floating point)
        if (stick.LengthSquared() > 0.0001f)
        {
            // Normalize for constant rotation speed, then scale by delta time
            stick.Normalize();
            stick *= deltaTime;
        }
        else
        {
            stick = Vector2.Zero;
        }

        // Handle mouse lock/unlock
        // Note: InputMan provides the *decision*, but Stride Input handles the cursor state
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

        // Get mouse input (only when cursor is locked)
        var mouse = Vector2.Zero;
        if (Input.IsMousePositionLocked)
        {
            var mouseInput = _inputMan.GetAxis2(LookMouseAxis);
            // Invert Y axis for natural camera controls (up = look up)
            mouse = new Vector2(mouseInput.X, -mouseInput.Y) * MouseSensitivity;
        }

        // Combine stick and mouse rotation
        var cameraRotation = stick + mouse;
        CameraDirectionEventKey.Broadcast(cameraRotation);
    }

    /// <summary>
    /// Processes jump input and broadcasts jump events.
    /// </summary>
    private void ProcessJump()
    {
        var didJump = _inputMan!.WasPressed(JumpAction);
        JumpEventKey.Broadcast(didJump);
    }

    #endregion
}