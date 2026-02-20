using InputMan.Core;
using Microsoft.Xna.Framework;

namespace InputMan.MonoGameConn;

/// <summary>
/// MonoGame GameComponent that reads MonoGame input, builds an InputSnapshot, and ticks InputManEngine.
/// Registers IInputMan into Game.Services.
/// </summary>
public sealed class MonoGameInputManSystem : GameComponent
{
    private readonly InputManEngine _engine;
    private readonly Game _game;

    private readonly HashSet<ControlKey> _watchedButtons = [];
    private readonly HashSet<ControlKey> _watchedAxes = [];

    private int _lastProfileRevision;
    private Point? _previousMousePosition;

    /// <summary>
    /// Gets the InputMan interface for querying input state.
    /// </summary>
    public IInputMan InputMan => _engine;

    /// <summary>
    /// Creates a new MonoGameInputManSystem with no initial maps activated.
    /// You can activate maps later via IInputMan.SetMaps() or PushMap().
    /// </summary>
    public MonoGameInputManSystem(Game game, InputProfile profile)
        : this(game, profile, null)
    {
    }

    /// <summary>
    /// Creates a new MonoGameInputManSystem with the specified initial maps activated.
    /// Maps are activated in the order provided (first map = lowest priority).
    /// </summary>
    public MonoGameInputManSystem(
        Game game,
        InputProfile profile,
        params ActionMapId[]? initialMaps)
        : base(game)
    {
        _game = game;
        _engine = new InputManEngine(profile);

        // Activate initial maps if provided
        if (initialMaps != null && initialMaps.Length > 0)
            _engine.SetMaps(initialMaps);

        // Register service
        game.Services.AddService(typeof(IInputMan), _engine);

        // Update before game logic
        UpdateOrder = -1;

        // Track profile changes for rebuilding watched controls
        _lastProfileRevision = _engine.ProfileRevision;
        RebuildWatchedControls(_engine.ExportProfile());

        Enabled = true;
    }

    public override void Update(GameTime gameTime)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var totalTime = (float)gameTime.TotalGameTime.TotalSeconds;

        IReadOnlyCollection<ControlKey> buttons = _watchedButtons;
        IReadOnlyCollection<ControlKey> axes = _watchedAxes;

        if (_engine.IsRebinding)
        {
            // Prefer explicit candidate lists when provided
            if (_engine.RebindCandidateButtons is { Count: > 0 } candButtons)
                buttons = candButtons;

            if (_engine.RebindCandidateAxes is { Count: > 0 } candAxes)
                axes = candAxes;
        }

        var snapshot = MonoGameInputSnapshotBuilder.Build(
            buttons,
            axes,
            ref _previousMousePosition);

        _engine.Tick(snapshot, dt, totalTime);

        if (_engine.ProfileRevision != _lastProfileRevision)
        {
            _lastProfileRevision = _engine.ProfileRevision;
            RebuildWatchedControls(_engine.ExportProfile());
        }

        base.Update(gameTime);
    }

    private void RebuildWatchedControls(InputProfile profile)
    {
        _watchedButtons.Clear();
        _watchedAxes.Clear();

        // Watch every trigger control referenced by bindings
        foreach (var map in profile.Maps.Values)
        {
            foreach (var binding in map.Bindings)
            {
                var control = binding.Trigger.Control;

                // Decide whether it's a button or axis based on trigger type
                if (binding.Trigger.Type == TriggerType.Button)
                {
                    _watchedButtons.Add(control);

                    // CRITICAL: Also watch modifier keys (chords)
                    if (binding.Trigger.Modifiers is { Length: > 0 } mods)
                    {
                        foreach (var mod in mods)
                        {
                            _watchedButtons.Add(mod);
                        }
                    }
                }
                else
                {
                    _watchedAxes.Add(control);
                }
            }
        }
    }
}
