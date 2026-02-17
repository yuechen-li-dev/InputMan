using System;

namespace InputMan.Core;

/// <summary>
/// Convenience builders for the most common binding patterns.
/// Keep these engine-agnostic: they operate on ControlKey + IDs only.
/// </summary>
public static class Bind
{
    /// <summary>
    /// Basic action mapping.
    /// </summary>
    public static Binding Action(
        ControlKey key,
        ActionId action,
        ButtonEdge edge = ButtonEdge.Pressed,
        ConsumeMode consume = ConsumeMode.None,
        string? name = null)
        => new()
        {
            Name = name ?? $"{action.Name}:{key}",
            Trigger = new BindingTrigger
            {
                Control = key,
                Type = TriggerType.Button,
                ButtonEdge = edge,
            },
            Output = new ActionOutput(action),
            Consume = consume,
        };

    /// <summary>
    /// Classic WASD-style mapping: a button contributes +/- to an axis while held.
    /// </summary>
    public static Binding ButtonAxis(
        ControlKey key,
        AxisId axis,
        float scale,
        ConsumeMode consume = ConsumeMode.None,
        string? name = null)
        => new()
        {
            Name = name ?? $"{axis.Name}:{key}:{scale}",
            Trigger = new BindingTrigger
            {
                Control = key,
                Type = TriggerType.Button,
                ButtonEdge = ButtonEdge.Down,
            },
            Output = new AxisOutput(axis, scale),
            Consume = consume,
        };

    /// <summary>
    /// Analog axis mapping (sticks/triggers) or any non-delta axis source.
    /// </summary>
    public static Binding Axis(
        ControlKey key,
        AxisId axis,
        float scale = 1f,
        float threshold = 0f,
        ConsumeMode consume = ConsumeMode.None,
        string? name = null)
        => new()
        {
            Name = name ?? $"{axis.Name}:{key}",
            Trigger = new BindingTrigger
            {
                Control = key,
                Type = TriggerType.Axis,
                Threshold = threshold,
            },
            Output = new AxisOutput(axis, scale),
            Consume = consume,
        };

    /// <summary>
    /// Delta axis mapping (mouse deltas). Threshold default is 0 to avoid missing tiny motion.
    /// </summary>
    public static Binding DeltaAxis(
        ControlKey key,
        AxisId axis,
        float scale = 1f,
        ConsumeMode consume = ConsumeMode.None,
        string? name = null)
        => new()
        {
            Name = name ?? $"{axis.Name}:{key}:delta",
            Trigger = new BindingTrigger
            {
                Control = key,
                Type = TriggerType.DeltaAxis,
                Threshold = 0f,
            },
            Output = new AxisOutput(axis, scale),
            Consume = consume,
        };

    //Shortened overload for FSharp
    // ---- Action ----
    public static Binding Action(ControlKey key, ActionId action)
        => Action(key, action, ButtonEdge.Pressed, ConsumeMode.None, null);

    public static Binding Action(ControlKey key, ActionId action, ButtonEdge edge)
        => Action(key, action, edge, ConsumeMode.None, null);

    // ---- ButtonAxis ----
    public static Binding ButtonAxis(ControlKey key, AxisId axis, float scale)
        => ButtonAxis(key, axis, scale, ConsumeMode.None, null);

    // ---- Axis ----
    public static Binding Axis(ControlKey key, AxisId axis)
        => Axis(key, axis, 1f, 0f, ConsumeMode.None, null);

    public static Binding Axis(ControlKey key, AxisId axis, float scale)
        => Axis(key, axis, scale, 0f, ConsumeMode.None, null);

    // ---- DeltaAxis ----
    public static Binding DeltaAxis(ControlKey key, AxisId axis)
        => DeltaAxis(key, axis, 1f, ConsumeMode.None, null);

    public static Binding DeltaAxis(ControlKey key, AxisId axis, float scale)
        => DeltaAxis(key, axis, scale, ConsumeMode.None, null);
}
