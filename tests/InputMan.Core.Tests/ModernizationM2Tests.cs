using System.Numerics;
using InputMan.Core;
using InputMan.Core.Rebind;
using InputMan.Core.Serialization;
using InputMan.Toml;
using Xunit;

namespace InputMan.Core.Tests;

public sealed class ModernizationM2Tests
{
    private static readonly ActionMapId Gameplay = new("Gameplay");
    private static readonly ActionMapId Ui = new("UI");
    private static readonly ActionId Interact = new("Interact");
    private static readonly ActionId Confirm = new("Confirm");
    private static readonly AxisId MoveX = new("MoveX");
    private static readonly AxisId MoveY = new("MoveY");
    private static readonly Axis2Id Move = new("Move");

    [Fact]
    public void ModernAuthoring_KeyboardAndGamepadProduceSameLogicalAxis2()
    {
        InputProfile profile = CreateProfile();
        var keyboard = new InputManEngine(profile);
        keyboard.SetMaps(Gameplay);
        keyboard.Tick(Buttons((Controls.Key(KeyboardKey.W), true)), 1f / 60f, 0f);

        var gamepad = new InputManEngine(profile);
        gamepad.SetMaps(Gameplay);
        gamepad.Tick(Axes((Controls.Gamepad(GamepadAxis.LeftY), 1f)), 1f / 60f, 0f);

        Assert.Equal(new Vector2(0f, 1f), keyboard.CurrentFrame.GetAxis2(Move));
        Assert.Equal(keyboard.CurrentFrame.GetAxis2(Move), gamepad.CurrentFrame.GetAxis2(Move));
        Assert.Equal(DeviceKind.Gamepad, gamepad.CurrentFrame.LastActiveDevice?.Kind);
    }

    [Fact]
    public void ActionEdges_AreDerivedFromSnapshotsAndFocusLossFailsSafe()
    {
        var engine = new InputManEngine(CreateProfile());
        engine.SetMaps(Gameplay);
        ControlKey e = Controls.Key(KeyboardKey.E);

        engine.Tick(Buttons((e, true)), 0.016f, 0f);
        Assert.True(engine.CurrentFrame.WasPressed(Interact));
        engine.Tick(Buttons((e, true)), 0.016f, 0.016f);
        Assert.Equal(ActionValue.Held, engine.CurrentFrame.Actions[Interact]);

        engine.ResetOnFocusLoss();
        Assert.True(engine.CurrentFrame.WasReleased(Interact));
        engine.Tick(InputSnapshot.Empty, 0.016f, 0.032f);
        Assert.False(engine.CurrentFrame.WasPressed(Interact));
        Assert.False(engine.CurrentFrame.IsDown(Interact));
    }

    [Fact]
    public void UiConsumption_BlocksLowerMapRegardlessOfLowerBindingConsumeMode()
    {
        ControlKey e = Controls.Key(KeyboardKey.E);
        InputProfile profile = Input.Profile([
            Input.Map(Ui, 100, [Bind.Action(e, Confirm, consume: ConsumeMode.ControlOnly, name: "Confirm.Keyboard")]),
            Input.Map(Gameplay, 10, [Bind.Action(e, Interact, consume: ConsumeMode.None, name: "Interact.Keyboard")]),
        ]);
        var engine = new InputManEngine(profile);
        engine.SetMaps(Gameplay, Ui);

        engine.Tick(Buttons((e, true)), 0.016f, 0f);

        Assert.True(engine.WasPressed(Confirm));
        Assert.False(engine.WasPressed(Interact));
    }

    [Fact]
    public void ChordModifierRelease_ProducesARelease()
    {
        ActionId sprint = new("Sprint");
        ControlKey w = Controls.Key(KeyboardKey.W);
        ControlKey shift = Controls.Key(KeyboardKey.LeftShift);
        InputProfile profile = Input.Profile([
            Input.Map(Gameplay, 10, [Bind.ActionChord(w, sprint, ButtonEdge.Down, name: "Sprint.Keyboard", modifiers: shift)])
        ]);
        var engine = new InputManEngine(profile);
        engine.SetMaps(Gameplay);
        engine.Tick(Buttons((w, true), (shift, true)), 0.016f, 0f);
        Assert.True(engine.WasPressed(sprint));

        engine.Tick(Buttons((w, true), (shift, false)), 0.016f, 0.016f);
        Assert.True(engine.WasReleased(sprint));
    }

    [Fact]
    public void TomlRoundTrip_IsCanonicalAndRejectsUnknownVersions()
    {
        InputProfile original = CreateProfile();
        string first = InputProfileToml.Save(original);
        InputProfile loaded = InputProfileToml.Load(first);
        string second = InputProfileToml.Save(loaded);

        Assert.Equal(first, second);
        Assert.Contains("formatVersion = 1", first);
        Assert.Contains("Gamepad.0.LeftY", first);
        Assert.Throws<NotSupportedException>(() => InputProfileToml.Load(first.Replace("formatVersion = 1", "formatVersion = 2")));
    }

    [Fact]
    public void RebindingCandidateFrame_IsConsumedAndReplaceExistingSwapsControls()
    {
        ControlKey e = Controls.Key(KeyboardKey.E);
        ControlKey f = Controls.Key(KeyboardKey.F);
        ActionId secondary = new("Secondary");
        InputProfile profile = Input.Profile([
            Input.Map(Gameplay, 10,
            [
                Bind.Action(e, Interact, name: "Interact.Keyboard"),
                Bind.Action(f, secondary, name: "Secondary.Keyboard"),
            ])
        ]);
        var engine = new InputManEngine(profile);
        engine.SetMaps(Gameplay);
        IRebindSession session = engine.StartRebind(new RebindRequest
        {
            Map = Gameplay,
            BindingNameOrSlot = "Interact.Keyboard",
            CandidateButtons = [e, f],
            ConflictPolicy = RebindConflictPolicy.ReplaceExisting,
        });
        RebindResult? result = null;
        session.OnCompleted += value => result = value;

        engine.Tick(InputSnapshot.Empty, 0.016f, 0f);
        engine.Tick(Buttons((f, true)), 0.016f, 0.016f);

        Assert.True(result?.Succeeded);
        Assert.False(engine.WasPressed(Interact));
        Assert.Equal(f, profile.Maps[Gameplay.Name].Bindings[0].Trigger.Control);
        Assert.Equal(e, profile.Maps[Gameplay.Name].Bindings[1].Trigger.Control);
    }

    [Fact]
    public void ClampProcessor_IsDeterministic()
    {
        Assert.Equal(1f, new ClampProcessor().Process(2f));
        Assert.Equal(-0.5f, new ClampProcessor(-0.5f, 0.5f).Process(-2f));
    }

    [Fact]
    public void DeviceAssignment_FiltersPlayerGamepadsDeterministically()
    {
        ControlKey keyboard = Controls.Key(KeyboardKey.W);
        ControlKey pad0 = Controls.Gamepad(GamepadButton.South, 0);
        ControlKey pad1 = Controls.Gamepad(GamepadButton.South, 1);
        var snapshot = Buttons((keyboard, true), (pad0, true), (pad1, true));
        var playerOne = new InputDeviceAssignment(1, false, new HashSet<byte> { 1 });

        InputSnapshot filtered = playerOne.Filter(snapshot);

        Assert.False(filtered.TryGetButton(keyboard, out _));
        Assert.False(filtered.TryGetButton(pad0, out _));
        Assert.True(filtered.TryGetButton(pad1, out bool down) && down);
    }

    [Fact]
    public void BindingPrompts_ExposePortableDisplayMetadata()
    {
        IReadOnlyList<BindingPrompt> prompts = BindingPrompts.ForAction(CreateProfile(), Interact);
        Assert.Contains(prompts, prompt => prompt.Label == "Keyboard.0.E");
        Assert.Contains(prompts, prompt => prompt.Label == "Gamepad.0.South");
    }

    [Fact]
    public void Rebind_PersistsTomlAndReloadChangesTriggeredControl()
    {
        string path = Path.Combine(Path.GetTempPath(), $"inputman-{Guid.NewGuid():N}.toml");
        try
        {
            InputProfile profile = Input.Profile([
                Input.Map(Gameplay, 10, [Bind.Action(Controls.Key(KeyboardKey.E), Interact, name: "Interact.Keyboard")])
            ]);
            var storage = new TomlProfileStorage(path, () => profile);
            var engine = new InputManEngine(profile);
            engine.SetMaps(Gameplay);
            var manager = new RebindingManager(engine, storage);
            manager.StartRebind(new RebindRequest
            {
                Map = Gameplay,
                BindingNameOrSlot = "Interact.Keyboard",
                CandidateButtons = [Controls.Key(KeyboardKey.E), Controls.Key(KeyboardKey.F)],
                ConflictPolicy = RebindConflictPolicy.Reject,
            });
            engine.Tick(InputSnapshot.Empty, 0.016f, 0f);
            engine.Tick(Buttons((Controls.Key(KeyboardKey.F), true)), 0.016f, 0.016f);

            InputProfile reloaded = storage.LoadProfile();
            var reloadedEngine = new InputManEngine(reloaded);
            reloadedEngine.SetMaps(Gameplay);
            reloadedEngine.Tick(Buttons((Controls.Key(KeyboardKey.E), true)), 0.016f, 0f);
            Assert.False(reloadedEngine.WasPressed(Interact));
            reloadedEngine.Tick(InputSnapshot.Empty, 0.016f, 0.016f);
            reloadedEngine.Tick(Buttons((Controls.Key(KeyboardKey.F), true)), 0.016f, 0.032f);
            Assert.True(reloadedEngine.WasPressed(Interact));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void LegacyJson_CanBeImportedAndSavedAsCanonicalToml()
    {
        InputProfile profile = CreateProfile();
        string json = InputProfileJson.Save(profile);
        InputProfile imported = InputProfileJson.Load(json);

        string toml = InputProfileToml.Save(imported);

        Assert.Equal(toml, InputProfileToml.Save(InputProfileToml.Load(toml)));
    }

    private static InputProfile CreateProfile()
    {
        List<Binding> movement = [.. Input.Wasd(MoveX, MoveY), .. Input.GamepadLeftStick(MoveX, MoveY)];
        movement.Add(Bind.Action(Controls.Key(KeyboardKey.E), Interact, name: "Interact.Keyboard"));
        movement.Add(Bind.Action(Controls.Gamepad(GamepadButton.South), Interact, name: "Interact.Gamepad"));
        return Input.Profile(
            [Input.Map(Gameplay, 10, movement, canConsume: false)],
            [Input.Axis2(Move, MoveX, MoveY)]);
    }

    private static InputSnapshot Buttons(params (ControlKey Key, bool Value)[] values)
    {
        return new InputSnapshot(values.ToDictionary(value => value.Key, value => value.Value));
    }

    private static InputSnapshot Axes(params (ControlKey Key, float Value)[] values)
    {
        return new InputSnapshot(axes: values.ToDictionary(value => value.Key, value => value.Value));
    }
}
