using System;
using System.Collections.Generic;

namespace InputMan.Core;

/// <summary>
/// Defines how inputs are consumed to prevent lower-priority maps from seeing them.
/// </summary>
public enum ConsumeMode : byte
{
    /// <summary>No consumption - multiple maps can read the same input.</summary>
    None = 0,

    /// <summary>Consume the physical control (e.g., Space key blocked for other bindings).</summary>
    ControlOnly = 1,

    /// <summary>Consume the action (e.g., Jump action fires only once across all maps).</summary>
    ActionOnly = 2,

    /// <summary>Consume both the control and the action.</summary>
    All = 3,
}

/// <summary>
/// Connects a physical control (key, button, stick) to an action or axis output.
/// Bindings can include processors for input transformation and chord/modifier keys.
/// </summary>
public sealed class Binding
{
    /// <summary>
    /// Gets the unique name of this binding (used for rebinding).
    /// </summary>
    /// <remarks>
    /// Name should be unique within the map. Format convention: "Action.Device.Key" 
    /// (e.g., "Jump.Kb", "Fire.Mouse", "Sprint.Kb.W+Shift").
    /// </remarks>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the trigger configuration (which control activates this binding and how).
    /// </summary>
    public BindingTrigger Trigger { get; init; } = new();

    /// <summary>
    /// Gets the list of processors that transform input values before output.
    /// </summary>
    /// <remarks>
    /// Processors are applied in order. Common processors: DeadzoneProcessor, ScaleProcessor, InvertProcessor.
    /// </remarks>
    public List<IProcessor> Processors { get; init; } = [];

    /// <summary>
    /// Gets the output (which action or axis this binding affects).
    /// </summary>
    public BindingOutput Output { get; init; } = new ActionOutput(new ActionId("Unnamed"));

    /// <summary>
    /// Gets the consumption mode (how this binding blocks lower-priority maps).
    /// </summary>
    public ConsumeMode Consume { get; init; } = ConsumeMode.ControlOnly;
}

/// <summary>
/// Defines what physical control triggers a binding and under what conditions.
/// Supports chord bindings (modifier keys that must be held).
/// </summary>
public sealed class BindingTrigger
{
    /// <summary>
    /// Gets the primary control that triggers this binding.
    /// </summary>
    public ControlKey Control { get; init; }

    /// <summary>
    /// Gets the type of trigger (Button, Axis, or DeltaAxis).
    /// </summary>
    public TriggerType Type { get; init; } = TriggerType.Button;

    /// <summary>
    /// Gets the modifier keys that must all be held for this binding to trigger (chord binding).
    /// </summary>
    /// <remarks>
    /// Empty array means no modifiers required. All modifiers must be held simultaneously
    /// with the primary control for the binding to activate.
    /// Example: Sprint = W + LeftShift modifier.
    /// </remarks>
    public ControlKey[] Modifiers { get; init; } = Array.Empty<ControlKey>();

    /// <summary>
    /// Gets the button edge to detect (for button triggers).
    /// </summary>
    /// <remarks>
    /// Down: Fires while button is held.
    /// Pressed: Fires once on the frame the button is pressed.
    /// Released: Fires once on the frame the button is released.
    /// </remarks>
    public ButtonEdge ButtonEdge { get; init; } = ButtonEdge.Down;

    /// <summary>
    /// Gets the threshold for axis triggers (input must exceed this to trigger).
    /// </summary>
    /// <remarks>
    /// For analog sticks, typical values are 0.15-0.25 to ignore stick drift.
    /// For delta axes (mouse), usually 0 to catch any movement.
    /// </remarks>
    public float Threshold { get; init; } = 0f;
}

/// <summary>
/// Base class for binding outputs (action or axis).
/// </summary>
public abstract record BindingOutput;

/// <summary>
/// Binding output that triggers an action (discrete event: pressed, held, released).
/// </summary>
public sealed record ActionOutput(ActionId Action) : BindingOutput;

/// <summary>
/// Binding output that contributes to an axis value (continuous analog input).
/// </summary>
/// <param name="Axis">The axis to output to.</param>
/// <param name="Scale">Multiplier applied to the input value (default 1.0).</param>
public sealed record AxisOutput(AxisId Axis, float Scale = 1f) : BindingOutput;

/// <summary>
/// Transforms input values before they're output to actions or axes.
/// </summary>
/// <remarks>
/// Processors are applied in order. Built-in processors: DeadzoneProcessor, ScaleProcessor, InvertProcessor.
/// </remarks>
public interface IProcessor
{
    /// <summary>
    /// Processes an input value and returns the transformed result.
    /// </summary>
    /// <param name="value">The raw input value.</param>
    /// <returns>The processed value.</returns>
    float Process(float value);
}

/// <summary>
/// Multiplies input values by a constant scale factor.
/// </summary>
/// <remarks>
/// Use for sensitivity adjustments or inverting axes (negative scale).
/// Example: ScaleProcessor(2.0f) doubles sensitivity.
/// </remarks>
public sealed record ScaleProcessor(float Scale) : IProcessor
{
    /// <summary>Processes the value by multiplying it by the scale factor.</summary>
    public float Process(float value) => value * Scale;
}

/// <summary>
/// Inverts the input value (negates it).
/// </summary>
/// <remarks>
/// Common for inverting Y-axis on cameras or control schemes.
/// Equivalent to ScaleProcessor(-1.0f).
/// </remarks>
public sealed record InvertProcessor() : IProcessor
{
    /// <summary>Processes the value by negating it.</summary>
    public float Process(float value) => -value;
}

/// <summary>
/// Applies a deadzone and remaps values to the full range.
/// </summary>
/// <remarks>
/// Values below the deadzone threshold return 0.
/// Values above are remapped from [deadzone, 1.0] to [0.0, 1.0] to maintain full analog range.
/// Essential for analog sticks to eliminate drift.
/// Example: DeadzoneProcessor(0.15f) ignores inputs below 15%.
/// </remarks>
public sealed record DeadzoneProcessor(float Deadzone) : IProcessor
{
    /// <summary>
    /// Processes the value by applying deadzone and remapping.
    /// </summary>
    public float Process(float value)
    {
        var abs = Math.Abs(value);
        if (abs <= Deadzone) return 0f;

        // Remap so value starts at 0 at the edge of the deadzone and reaches 1 at max.
        var sign = Math.Sign(value);
        var remapped = (abs - Deadzone) / (1f - Deadzone);
        return sign * remapped;
    }
}