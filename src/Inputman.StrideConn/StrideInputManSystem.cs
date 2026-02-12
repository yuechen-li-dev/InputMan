using InputMan.Core;
using Stride.Core;
using Stride.Engine;
using Stride.Games;
using Stride.Input;


namespace InputMan.StrideConn;

/// <summary>
/// Stride GameSystem that reads Stride input, builds an InputSnapshot, and ticks InputManEngine.
/// Registers IInputMan into Game.Services.
/// </summary>
public sealed class StrideInputManSystem : GameSystem
{
    private readonly InputManEngine _engine;
    private readonly InputManager _input;

    private readonly HashSet<ControlKey> _watchedButtons = [];
    private readonly HashSet<ControlKey> _watchedAxes = [];

    private int _lastProfileRevision;

    public StrideInputManSystem(IServiceRegistry services, InputProfile profile)
    : base(services)
    {
        _input = services.GetService<InputManager>()
            ?? throw new InvalidOperationException("Stride InputManager service is missing.");
        _engine = new InputManEngine(profile);

        //Push map to activate. TODO: maybe make it configuarble later.
        _engine.PushMap(new ActionMapId("Gameplay"));

        // Register service
        services.AddService<IInputMan>(_engine);

        // Tick early so scripts can read stable state
        UpdateOrder = -1;

        // Use a counter to keep track of revision for rebuild. Could be made cleaner in the future. 
        _lastProfileRevision = _engine.ProfileRevision;
        RebuildWatchedControls(_engine.ExportProfile());

        // GO!
        Enabled = true;
    }

    public override void Update(GameTime gameTime)
    {
        var dt = (float)gameTime.Elapsed.TotalSeconds;
        var t = (float)gameTime.Total.TotalSeconds;

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

        var snapshot = StrideInputSnapshotBuilder.Build(_input, buttons, axes);

        _engine.Tick(snapshot, dt, t);

        if (_engine.ProfileRevision != _lastProfileRevision)
        {
            _lastProfileRevision = _engine.ProfileRevision;
            RebuildWatchedControls(_engine.ExportProfile());
        }
    }

    private void RebuildWatchedControls(InputProfile profile)
    {
        _watchedButtons.Clear();
        _watchedAxes.Clear();

        // Watch every trigger control referenced by bindings.
        foreach (var map in profile.Maps.Values)
        {
            foreach (var binding in map.Bindings)
            {
                var control = binding.Trigger.Control;

                // Decide whether it’s a button or axis based on trigger type.
                if (binding.Trigger.Type == TriggerType.Button)
                    _watchedButtons.Add(control);
                else
                    _watchedAxes.Add(control);
                //Debug: Get all bindings
                //System.Diagnostics.Debug.WriteLine($"BIND: map={map.Id.Name} name={binding.Name} type={binding.Trigger.Type} ctrl={binding.Trigger.Control.Device}:{binding.Trigger.Control.DeviceIndex}:{binding.Trigger.Control.Code}");
            }
        }

        // NOTE:
        // If you ever bind Axis2 via derived axes only (not directly via triggers),
        // you’re fine—Axis2 is computed in Core from Axis ids, not controls.
    }
}
