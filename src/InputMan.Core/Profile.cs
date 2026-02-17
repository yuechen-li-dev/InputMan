using System.Collections.Generic;

namespace InputMan.Core;

/// <summary>
/// Represents a complete input configuration including action maps, bindings, and axis definitions.
/// </summary>
/// <remarks>
/// A profile defines:
/// <list type="bullet">
/// <item>Action maps with priority-based evaluation</item>
/// <item>Bindings that connect physical controls to actions/axes</item>
/// <item>2D axis compositions (e.g., combining MoveX and MoveY)</item>
/// <item>Global options like default deadzone</item>
/// </list>
/// Profiles can be created in code or loaded from JSON/TOML using serializers.
/// Use <see cref="IInputMan.ExportProfile"/> to save runtime changes from rebinding.
/// </remarks>
public sealed class InputProfile
{
    /// <summary>
    /// Gets the profile format version. Used for backward compatibility.
    /// </summary>
    /// <remarks>
    /// Current version is 1. Future versions may add new fields while maintaining
    /// compatibility with older profiles.
    /// </remarks>
    public int Version { get; init; } = 1;

    /// <summary>
    /// Gets global input options that apply to all maps and bindings.
    /// </summary>
    /// <remarks>
    /// Configure default behavior for deadzone and epsilon values.
    /// Individual bindings can override these with processors.
    /// </remarks>
    public InputOptions Options { get; init; } = new();

    /// <summary>
    /// Gets the collection of action maps, keyed by map name.
    /// </summary>
    /// <remarks>
    /// Each map contains bindings and has a priority that determines evaluation order.
    /// Higher priority maps are evaluated first and can consume inputs to block lower maps.
    /// <para>
    /// Common pattern: UI map (priority 100) blocks Gameplay map (priority 10) when active.
    /// </para>
    /// </remarks>
    public Dictionary<string, ActionMapDefinition> Maps { get; init; } = [];

    /// <summary>
    /// Gets the collection of 2D axis definitions, keyed by Axis2 ID name.
    /// </summary>
    /// <remarks>
    /// Defines how two separate axes (X and Y) combine into a Vector2.
    /// For example, "Move" might combine "MoveX" and "MoveY" axes.
    /// <para>
    /// Access these values using <see cref="IInputMan.GetAxis2"/>.
    /// </para>
    /// </remarks>
    public Dictionary<string, Axis2Definition> Axis2 { get; init; } = [];
}

/// <summary>
/// Global options that control input processing behavior.
/// </summary>
/// <remarks>
/// These are default values. Individual bindings can override them using processors.
/// For example, add a <see cref="DeadzoneProcessor"/> to a specific binding to use
/// a different deadzone than the default.
/// </remarks>
public sealed class InputOptions
{
    /// <summary>
    /// Gets the default deadzone for analog inputs.
    /// </summary>
    /// <remarks>
    /// Default is 0.15 (15%). Values below this threshold are treated as zero.
    /// This helps eliminate stick drift and accidental inputs.
    /// <para>
    /// Note: This is a legacy field. Modern usage prefers explicit <see cref="DeadzoneProcessor"/>
    /// on individual bindings for more control.
    /// </para>
    /// </remarks>
    public float DefaultDeadzone { get; init; } = 0.15f;

    /// <summary>
    /// Gets the epsilon value used for floating-point comparisons.
    /// </summary>
    /// <remarks>
    /// Used to determine if an axis value is effectively zero (to handle floating-point noise).
    /// Default is 0.0001. Axis values below this are considered zero.
    /// </remarks>
    public float DefaultAxisEpsilon { get; init; } = 0.0001f;
}

/// <summary>
/// Defines how two separate axes combine into a 2D vector.
/// </summary>
/// <remarks>
/// Used for inputs that naturally form a 2D vector, such as:
/// <list type="bullet">
/// <item>Movement (left stick, WASD keys)</item>
/// <item>Camera control (right stick, mouse delta)</item>
/// <item>Any other paired X/Y inputs</item>
/// </list>
/// The X and Y axes are defined separately in bindings, then composed here.
/// </remarks>
public sealed class Axis2Definition
{
    /// <summary>
    /// Gets the ID of this 2D axis (e.g., "Move", "Look").
    /// </summary>
    public Axis2Id Id { get; init; }

    /// <summary>
    /// Gets the axis ID for the X component (horizontal).
    /// </summary>
    /// <remarks>
    /// This references an axis that's driven by bindings in your action maps.
    /// For example, "MoveX" might be driven by A/D keys and left stick X.
    /// </remarks>
    public AxisId X { get; init; }

    /// <summary>
    /// Gets the axis ID for the Y component (vertical).
    /// </summary>
    /// <remarks>
    /// This references an axis that's driven by bindings in your action maps.
    /// For example, "MoveY" might be driven by W/S keys and left stick Y.
    /// </remarks>
    public AxisId Y { get; init; }
}

/// <summary>
/// Defines an action map with priority, consumption rules, and bindings.
/// </summary>
/// <remarks>
/// Action maps organize related inputs (e.g., "Gameplay", "UI", "Menu").
/// Maps are evaluated in priority order, with higher values first.
/// Maps with <see cref="CanConsume"/> set to true can block inputs from reaching lower-priority maps.
/// <para>
/// Common pattern:
/// <code>
/// UI Map (Priority: 100, CanConsume: true)  ← Evaluated first, blocks gameplay
/// Gameplay Map (Priority: 10, CanConsume: false)
/// </code>
/// </para>
/// </remarks>
public sealed class ActionMapDefinition
{
    /// <summary>
    /// Gets the unique identifier for this map.
    /// </summary>
    /// <remarks>
    /// Used to activate/deactivate the map with <see cref="IInputMan.SetMaps"/>.
    /// </remarks>
    public ActionMapId Id { get; init; }

    /// <summary>
    /// Gets the priority of this map. Higher values are evaluated first.
    /// </summary>
    /// <remarks>
    /// Typical values:
    /// <list type="bullet">
    /// <item>UI/Menu: 100 (highest priority, blocks gameplay)</item>
    /// <item>Gameplay: 10 (normal priority)</item>
    /// <item>Debug/Cheats: 5 (lowest priority, only when nothing else consumes)</item>
    /// </list>
    /// </remarks>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Gets whether this map can consume inputs, preventing lower-priority maps from seeing them.
    /// </summary>
    /// <remarks>
    /// When true, bindings in this map can consume controls and actions based on their
    /// <see cref="Binding.Consume"/> mode. When false, this map never blocks other maps.
    /// <para>
    /// Typical usage:
    /// <list type="bullet">
    /// <item>UI maps: true (block gameplay when menu is open)</item>
    /// <item>Gameplay maps: false (allow multiple gameplay systems to read input)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool CanConsume { get; init; } = true;

    /// <summary>
    /// Gets the collection of bindings in this map.
    /// </summary>
    /// <remarks>
    /// Each binding connects a physical control (key, button, stick) to an action or axis.
    /// Bindings are evaluated in order, but consumption rules determine which ones fire.
    /// </remarks>
    public List<Binding> Bindings { get; init; } = [];
}