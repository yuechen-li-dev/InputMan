namespace InputMan.Core;

/// <summary>Intent-first authoring helpers for the common mapping patterns.</summary>
public static class Input
{
    public static InputProfile Profile(
        IEnumerable<ActionMapDefinition> maps,
        IEnumerable<Axis2Definition>? axis2 = null,
        InputOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(maps);

        var profile = new InputProfile
        {
            Options = options ?? new InputOptions(),
            Maps = maps.ToDictionary(map => map.Id.Name, StringComparer.Ordinal),
            Axis2 = (axis2 ?? []).ToDictionary(axis => axis.Id.Name, StringComparer.Ordinal),
        };

        Validation.InputProfileValidator.Validate(profile);
        return profile;
    }

    public static ActionMapDefinition Map(
        ActionMapId id,
        int priority,
        IEnumerable<Binding> bindings,
        bool canConsume = true)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        return new ActionMapDefinition
        {
            Id = id,
            Priority = priority,
            CanConsume = canConsume,
            Bindings = [.. bindings],
        };
    }

    public static Axis2Definition Axis2(Axis2Id id, AxisId x, AxisId y)
    {
        return new Axis2Definition { Id = id, X = x, Y = y };
    }

    public static IEnumerable<Binding> Wasd(
        AxisId x,
        AxisId y,
        string namePrefix = "Move.Keyboard")
    {
        yield return Bind.ButtonAxis(Controls.Key(KeyboardKey.A), x, -1f, name: $"{namePrefix}.Left");
        yield return Bind.ButtonAxis(Controls.Key(KeyboardKey.D), x, 1f, name: $"{namePrefix}.Right");
        yield return Bind.ButtonAxis(Controls.Key(KeyboardKey.S), y, -1f, name: $"{namePrefix}.Down");
        yield return Bind.ButtonAxis(Controls.Key(KeyboardKey.W), y, 1f, name: $"{namePrefix}.Up");
    }

    public static IEnumerable<Binding> GamepadLeftStick(
        AxisId x,
        AxisId y,
        float deadzone = 0.15f,
        byte deviceIndex = 0,
        string namePrefix = "Move.Gamepad")
    {
        yield return Bind.Axis(
            Controls.Gamepad(GamepadAxis.LeftX, deviceIndex),
            x,
            name: $"{namePrefix}.X",
            processors: new DeadzoneProcessor(deadzone));
        yield return Bind.Axis(
            Controls.Gamepad(GamepadAxis.LeftY, deviceIndex),
            y,
            name: $"{namePrefix}.Y",
            processors: new DeadzoneProcessor(deadzone));
    }
}
