using System.Collections.Generic;

namespace InputMan.Core;

public sealed class InputProfile
{
    public int Version { get; init; } = 1;
    public InputOptions Options { get; init; } = new();

    /// <summary>Map id -&gt; map definition.</summary>
    public Dictionary<string, ActionMapDefinition> Maps { get; init; } = [];

    /// <summary>Axis2 id -&gt; definition (how to compose two axes into a vector2).</summary>
    public Dictionary<string, Axis2Definition> Axis2 { get; init; } = [];
}

public sealed class InputOptions
{
    public float DefaultDeadzone { get; init; } = 0.15f;
    public float DefaultAxisEpsilon { get; init; } = 0.0001f;
}

public sealed class Axis2Definition
{
    public Axis2Id Id { get; init; }
    public AxisId X { get; init; }
    public AxisId Y { get; init; }
}

public sealed class ActionMapDefinition
{
    public ActionMapId Id { get; init; }
    public int Priority { get; init; } = 0;
    public bool CanConsume { get; init; } = true;

    public List<Binding> Bindings { get; init; } = [];
}
