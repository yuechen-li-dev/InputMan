using InputMan.Core.Rebind;
using System;
using System.Numerics;

namespace InputMan.Core;

/// <summary>
/// Main interface for the InputMan input management system.
/// Provides methods for reading input state, managing action maps, and runtime rebinding.
/// </summary>
/// <remarks>
/// InputMan organizes inputs into action maps with priority-based consumption.
/// Higher-priority maps can block inputs from reaching lower-priority maps.
/// Use <see cref="SetMaps"/> to control which maps are active.
/// </remarks>
public interface IInputMan
{
    /// <summary>
    /// Gets the current frame index. Increments by 1 each time <c>Tick()</c> is called.
    /// </summary>
    /// <remarks>
    /// Useful for frame-based logic and detecting stale input events.
    /// </remarks>
    long FrameIndex { get; }

    /// <summary>
    /// Gets the delta time in seconds from the last frame.
    /// </summary>
    /// <remarks>
    /// This is the value passed to <c>Tick(deltaTimeSeconds, ...)</c>.
    /// Use for frame-rate independent movement calculations.
    /// </remarks>
    float DeltaTimeSeconds { get; }

    /// <summary>
    /// Checks if an action is currently held down.
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <returns>True if the action is currently down, false otherwise.</returns>
    /// <remarks>
    /// Returns true for the entire duration the action is held.
    /// Use <see cref="WasPressed"/> to detect only the initial press frame.
    /// </remarks>
    bool IsDown(ActionId action);

    /// <summary>
    /// Checks if an action was pressed this frame (rising edge).
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <returns>True only on the frame the action transitioned from up to down.</returns>
    /// <remarks>
    /// This returns true for exactly one frame when a button is first pressed.
    /// Use this for jump, fire, or other "press once" actions.
    /// </remarks>
    bool WasPressed(ActionId action);

    /// <summary>
    /// Checks if an action was released this frame (falling edge).
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <returns>True only on the frame the action transitioned from down to up.</returns>
    /// <remarks>
    /// Use this for charged attacks or other mechanics that trigger on button release.
    /// </remarks>
    bool WasReleased(ActionId action);

    /// <summary>
    /// Gets the current value of an axis.
    /// </summary>
    /// <param name="axis">The axis to read.</param>
    /// <returns>
    /// The current axis value. Typically -1 to +1 for analog sticks,
    /// unbounded for mouse delta axes.
    /// </returns>
    /// <remarks>
    /// Multiple bindings to the same axis accumulate their values.
    /// For example, WASD keys and left stick can both contribute to a movement axis.
    /// Analog axes are automatically clamped to [-1, 1]. Delta axes (mouse) are not clamped.
    /// </remarks>
    float GetAxis(AxisId axis);

    /// <summary>
    /// Gets the current value of a 2D axis (combines two axes into a Vector2).
    /// </summary>
    /// <param name="axis2">The 2D axis to read.</param>
    /// <returns>
    /// A Vector2 containing the X and Y axis values.
    /// </returns>
    /// <remarks>
    /// Commonly used for movement (left stick), camera (right stick), or mouse look.
    /// The X and Y axes are defined in your profile's Axis2 definitions.
    /// </remarks>
    Vector2 GetAxis2(Axis2Id axis2);

    /// <summary>
    /// Activates an action map with an optional priority override.
    /// </summary>
    /// <param name="map">The map to activate.</param>
    /// <param name="priorityOverride">
    /// Optional priority override. If null, uses the map's defined priority.
    /// Higher values are evaluated first.
    /// </param>
    /// <remarks>
    /// Maps can be pushed multiple times (reference counted).
    /// Each <see cref="PushMap"/> must be balanced with a <see cref="PopMap"/>.
    /// Use <see cref="SetMaps"/> for simpler scenarios where you want to replace all active maps.
    /// </remarks>
    void PushMap(ActionMapId map, int? priorityOverride = null);

    /// <summary>
    /// Deactivates an action map.
    /// </summary>
    /// <param name="map">The map to deactivate.</param>
    /// <remarks>
    /// Decrements the reference count for the map. The map is fully deactivated when
    /// the reference count reaches zero.
    /// </remarks>
    void PopMap(ActionMapId map);

    /// <summary>
    /// Replaces all active maps with the specified maps.
    /// </summary>
    /// <param name="maps">The maps to activate, in priority order.</param>
    /// <remarks>
    /// This is the simplest way to manage active maps.
    /// Clears all currently active maps and activates only the specified ones.
    /// Maps are evaluated in the order of their defined priorities (not the order passed here).
    /// Common pattern:
    /// <code>
    /// // Show pause menu - only UI active
    /// inputMan.SetMaps(new ActionMapId("UI"));
    /// 
    /// // Resume game - both UI and gameplay active
    /// inputMan.SetMaps(new ActionMapId("UI"), new ActionMapId("Gameplay"));
    /// </code>
    /// </remarks>
    void SetMaps(params ActionMapId[] maps);

    /// <summary>
    /// Event fired when an action changes state (pressed or released).
    /// </summary>
    /// <remarks>
    /// Subscribe to receive notifications for all action state changes.
    /// Use this for debugging, input recording, or UI feedback.
    /// For gameplay code, prefer polling methods like <see cref="WasPressed"/>.
    /// </remarks>
    event Action<ActionEvent>? OnAction;

    /// <summary>
    /// Event fired when an axis value changes.
    /// </summary>
    /// <remarks>
    /// Fires every frame an axis has a non-zero value after processing.
    /// Use for debugging or UI visualization. For gameplay, prefer <see cref="GetAxis"/>.
    /// </remarks>
    event Action<AxisEvent>? OnAxis;

    /// <summary>
    /// Starts a rebinding session to change a control binding at runtime.
    /// </summary>
    /// <param name="request">Configuration for the rebinding session.</param>
    /// <returns>
    /// A rebinding session that monitors for input and completes when a valid control is pressed.
    /// </returns>
    /// <remarks>
    /// Only one rebinding session can be active at a time. Starting a new session
    /// automatically cancels any existing session.
    /// <para>
    /// For most use cases, prefer using <c>RebindingManager</c> from InputMan.Core,
    /// which provides a higher-level API with automatic profile saving.
    /// </para>
    /// <example>
    /// Basic rebinding:
    /// <code>
    /// var request = new RebindRequest
    /// {
    ///     Map = new ActionMapId("Gameplay"),
    ///     BindingNameOrSlot = "Jump.Kb",
    ///     CandidateButtons = StrideCandidateButtons.KeyboardAndGamepad(),
    ///     Timeout = TimeSpan.FromSeconds(10)
    /// };
    /// 
    /// var session = inputMan.StartRebind(request);
    /// session.OnCompleted += result =>
    /// {
    ///     if (result.Succeeded)
    ///         SaveProfile(inputMan.ExportProfile());
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    IRebindSession StartRebind(RebindRequest request);

    /// <summary>
    /// Exports the current input profile, including any runtime changes from rebinding.
    /// </summary>
    /// <returns>A copy of the current profile that can be serialized or modified.</returns>
    /// <remarks>
    /// Use this to save user rebinds or to inspect the current configuration.
    /// The returned profile is a snapshot - modifying it won't affect the running system.
    /// Use <see cref="ImportProfile"/> to apply changes.
    /// </remarks>
    InputProfile ExportProfile();

    /// <summary>
    /// Imports a new input profile, replacing the current configuration.
    /// </summary>
    /// <param name="profile">The profile to import.</param>
    /// <remarks>
    /// This completely replaces the current profile and resets all input state.
    /// Use this to load user profiles or switch between different control schemes.
    /// Active maps are preserved, but their bindings are updated immediately.
    /// </remarks>
    void ImportProfile(InputProfile profile);
}