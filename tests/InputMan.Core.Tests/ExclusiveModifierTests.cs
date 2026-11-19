using InputMan.Core.Rebind;
using InputMan.Core.Serialization;
using Xunit;

namespace InputMan.Core.Tests;

public sealed class ExclusiveModifierTests
{
    private static readonly ControlKey Ctrl = Controls.Key(KeyboardKey.LeftControl);
    private static readonly ControlKey Shift = Controls.Key(KeyboardKey.LeftShift);
    private static readonly ControlKey Alt = Controls.Key(KeyboardKey.LeftAlt);
    private static readonly ControlKey K = Controls.Key(KeyboardKey.K);
    private static readonly ControlKey Z = Controls.Key(KeyboardKey.Z);
    private static readonly ControlKey Delete = Controls.Key(KeyboardKey.Delete);
    private static readonly ActionMapId Map = new("Editor");

    private static InputProfile Profile(bool exclusive) => new()
    {
        Options = new InputOptions { ExclusiveModifiers = exclusive ? [Ctrl, Shift, Alt] : [] },
        Maps = new()
        {
            ["Editor"] = new ActionMapDefinition
            {
                Id = Map,
                Bindings =
                [
                    Bind.Action(K, new ActionId("Pause"), name: "Pause"),
                    Bind.ActionChord(K, new ActionId("Split"), ButtonEdge.Pressed, name: "Split", modifiers: Ctrl),
                    Bind.ActionChord(Z, new ActionId("Undo"), ButtonEdge.Pressed, name: "Undo", modifiers: Ctrl),
                    Bind.ActionChord(Z, new ActionId("Redo"), ButtonEdge.Pressed, name: "Redo", modifiers: [Ctrl, Shift]),
                    Bind.Action(Delete, new ActionId("Delete"), name: "Delete"),
                    Bind.ActionChord(Delete, new ActionId("RippleDelete"), ButtonEdge.Pressed, name: "RippleDelete", modifiers: Shift),
                ],
            },
        },
    };

    private static InputManEngine Engine(bool exclusive)
    {
        var engine = new InputManEngine(Profile(exclusive));
        engine.SetMaps(Map);
        return engine;
    }

    private static string[] Press(InputManEngine engine, params ControlKey[] held)
    {
        engine.Tick(new InputSnapshot(held.ToDictionary(k => k, _ => true)), 0, engine.FrameIndex);
        return ["Pause", "Split", "Undo", "Redo", "Delete", "RippleDelete"];
    }

    private static string[] Fired(InputManEngine engine, params ControlKey[] held)
    {
        var names = Press(engine, held);
        return [.. names.Where(n => engine.CurrentFrame.WasPressed(new ActionId(n)))];
    }

    [Fact]
    public void ChordDoesNotFireUnmodifiedBinding()
    {
        var engine = Engine(true);
        Fired(engine);
        Fired(engine, Ctrl);
        Assert.Equal(["Split"], Fired(engine, Ctrl, K));
    }

    [Fact]
    public void LongerChordDoesNotFireShorterChord()
    {
        var engine = Engine(true);
        Fired(engine, Ctrl, Shift);
        Assert.Equal(["Redo"], Fired(engine, Ctrl, Shift, Z));
        Fired(engine);
        Fired(engine, Shift);
        Assert.Equal(["RippleDelete"], Fired(engine, Shift, Delete));
    }

    [Fact]
    public void ReleasingModifierWhileHoldingKeyDoesNotFireUnmodifiedBinding()
    {
        var engine = Engine(true);
        Fired(engine, Ctrl);
        Assert.Equal(["Split"], Fired(engine, Ctrl, K));
        Assert.Empty(Fired(engine, K));
    }

    [Fact]
    public void UnrelatedModifierBlocksBinding()
    {
        var engine = Engine(true);
        Fired(engine, Alt);
        Assert.Empty(Fired(engine, Alt, K));
    }

    [Fact]
    public void PermissiveDefaultKeepsLegacyChordBehaviour()
    {
        var engine = Engine(false);
        Fired(engine, Ctrl);
        Assert.Equal(["Pause", "Split"], Fired(engine, Ctrl, K));
    }

    [Fact]
    public void ExclusiveModifiersAndNewKeysRoundTripThroughJson()
    {
        var profile = Profile(true);
        profile.Maps["Editor"].Bindings.Add(Bind.Action(Controls.Key(KeyboardKey.Comma), new ActionId("Insert"), name: "Insert"));
        var loaded = InputProfileJson.Load(InputProfileJson.Save(profile));
        Assert.Equal([Ctrl, Shift, Alt], loaded.Options.ExclusiveModifiers);
        Assert.Contains(loaded.Maps["Editor"].Bindings, b => b.Trigger.Control == Controls.Key(KeyboardKey.Comma));
    }

    [Theory]
    [InlineData(KeyboardKey.Comma, 54)]
    [InlineData(KeyboardKey.Period, 55)]
    [InlineData(KeyboardKey.Semicolon, 51)]
    [InlineData(KeyboardKey.Apostrophe, 52)]
    [InlineData(KeyboardKey.Backslash, 49)]
    [InlineData(KeyboardKey.Equal, 46)]
    [InlineData(KeyboardKey.Minus, 45)]
    [InlineData(KeyboardKey.Backspace, 42)]
    [InlineData(KeyboardKey.Delete, 76)]
    [InlineData(KeyboardKey.Home, 74)]
    [InlineData(KeyboardKey.End, 77)]
    public void NewKeysUseHidUsageCodes(KeyboardKey key, int code) => Assert.Equal(code, (int)key);

    [Fact]
    public void RebindTreatsChordAndPlainKeyAsDifferentShortcuts()
    {
        var engine = Engine(true);
        RebindResult? result = null;
        var session = engine.StartRebind(new RebindRequest
        {
            Map = Map, BindingNameOrSlot = "Delete", CandidateButtons = [Ctrl, K, Z, Delete],
            AllowChord = true, ModifierControls = new HashSet<ControlKey> { Ctrl }, Timeout = TimeSpan.FromSeconds(5),
        });
        session.OnCompleted += r => result = r;
        engine.Tick(InputSnapshot.Empty, 0, 0);
        engine.Tick(new InputSnapshot(new Dictionary<ControlKey, bool> { [Ctrl] = true }), 0, 1);
        engine.Tick(new InputSnapshot(new Dictionary<ControlKey, bool> { [Ctrl] = true, [Z] = true }), 0, 2);
        // Ctrl+Z is taken by Undo: rejected even though plain Z is free, and the result names the owner.
        Assert.False(result!.Succeeded);
        Assert.Equal(["Undo"], result.ConflictingBindings);
        Assert.Equal(Z, result.AttemptedControl);
        Assert.Equal([Ctrl], result.AttemptedModifiers!);
    }

    [Fact]
    public void RebindSwapMovesTheWholeShortcut()
    {
        var engine = Engine(true);
        RebindResult? result = null;
        var session = engine.StartRebind(new RebindRequest
        {
            Map = Map, BindingNameOrSlot = "Pause", CandidateButtons = [Ctrl, K, Z],
            AllowChord = true, ModifierControls = new HashSet<ControlKey> { Ctrl }, ConflictPolicy = RebindConflictPolicy.ReplaceExisting,
            Timeout = TimeSpan.FromSeconds(5),
        });
        session.OnCompleted += r => result = r;
        engine.Tick(InputSnapshot.Empty, 0, 0);
        engine.Tick(new InputSnapshot(new Dictionary<ControlKey, bool> { [Ctrl] = true }), 0, 1);
        engine.Tick(new InputSnapshot(new Dictionary<ControlKey, bool> { [Ctrl] = true, [Z] = true }), 0, 2);
        Assert.True(result!.Succeeded);
        var bindings = engine.ExportProfile().Maps["Editor"].Bindings;
        var undo = bindings.Single(b => b.Name == "Undo").Trigger;
        Assert.Equal(K, undo.Control);
        Assert.Empty(undo.Modifiers);
    }
}
