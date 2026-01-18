using InputMan.Core;
using Silk.NET.SDL;
using Stride.Core;
using Stride.Core.DataSerializers;
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

        RebuildWatchedControls(profile);
        
        //GO!
        Enabled = true;
    }

    public override void Update(GameTime gameTime)
    {
        var dt = (float)gameTime.Elapsed.TotalSeconds;
        var t = (float)gameTime.Total.TotalSeconds;

        var snapshot = StrideInputSnapshotBuilder.Build(_input, _watchedButtons, _watchedAxes);
        _engine.Tick(snapshot, dt, t);
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
            }
        }

        // NOTE:
        // If you ever bind Axis2 via derived axes only (not directly via triggers),
        // you’re fine—Axis2 is computed in Core from Axis ids, not controls.
    }
}
