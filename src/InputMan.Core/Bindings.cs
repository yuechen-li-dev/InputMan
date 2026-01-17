using System;
using System.Collections.Generic;

namespace InputMan.Core;

public enum ConsumeMode : byte
{
    None = 0,
    ControlOnly = 1,
    ActionOnly = 2,
    All = 3,
}

public sealed class Binding
{
    public string Name { get; init; } = string.Empty;
    public BindingTrigger Trigger { get; init; } = new();
    public List<IProcessor> Processors { get; init; } = [];
    public BindingOutput Output { get; init; } = new ActionOutput(new ActionId("Unnamed"));
    public ConsumeMode Consume { get; init; } = ConsumeMode.ControlOnly;
}

public sealed class BindingTrigger
{
    public ControlKey Control { get; init; }
    public TriggerType Type { get; init; } = TriggerType.Button;

    // Button triggers
    public ButtonEdge ButtonEdge { get; init; } = ButtonEdge.Down;

    // Axis triggers
    public float Threshold { get; init; } = 0f;
}

public abstract record BindingOutput;

public sealed record ActionOutput(ActionId Action) : BindingOutput;
public sealed record AxisOutput(AxisId Axis, float Scale = 1f) : BindingOutput;

public interface IProcessor
{
    float Process(float value);
}

public sealed record ScaleProcessor(float Scale) : IProcessor
{
    public float Process(float value) => value * Scale;
}

public sealed record InvertProcessor() : IProcessor
{
    public float Process(float value) => -value;
}

public sealed record DeadzoneProcessor(float Deadzone) : IProcessor
{
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
