using InputMan.Core;
using Xunit;

namespace InputMan.Core.Tests;

public sealed class MapTransitionTests
{
    [Fact]
    public void ReactivatingAMapDoesNotTurnAHeldButtonIntoAPress()
    {
        ActionMapId gameplay = new("Gameplay");
        ActionMapId text = new("Text");
        ActionId attack = new("Attack");
        var engine = new InputManEngine(Input.Profile([
            Input.Map(gameplay, 10, [Bind.Action(Controls.Key(KeyboardKey.J), attack)]),
            Input.Map(text, 100, [])
        ]));
        var held = new InputSnapshot(new Dictionary<ControlKey, bool>
        {
            [Controls.Key(KeyboardKey.J)] = true
        }, new Dictionary<ControlKey, float>());
        engine.SetMaps(text);
        engine.Tick(held, .016f, 0);
        engine.SetMaps(gameplay);
        engine.Tick(held, .016f, .016f);
        Assert.True(engine.CurrentFrame.IsDown(attack));
        Assert.False(engine.CurrentFrame.WasPressed(attack));
        engine.Tick(InputSnapshot.Empty, .016f, .032f);
        engine.Tick(held, .016f, .048f);
        Assert.True(engine.CurrentFrame.WasPressed(attack));
    }

    [Fact]
    public void OpeningUiWithAnAlreadyHeldConfirmButtonDoesNotConfirmAgain()
    {
        ActionMapId gameplay = new("Gameplay");
        ActionMapId ui = new("UI");
        ActionId open = new("Open");
        ActionId confirm = new("Confirm");
        var engine = new InputManEngine(Input.Profile([
            Input.Map(gameplay, 10, [Bind.Action(Controls.Key(KeyboardKey.E), open)]),
            Input.Map(ui, 100, [Bind.Action(Controls.Key(KeyboardKey.E), confirm)])
        ]));
        var held = new InputSnapshot(new Dictionary<ControlKey, bool>
        {
            [Controls.Key(KeyboardKey.E)] = true
        }, new Dictionary<ControlKey, float>());
        engine.SetMaps(gameplay);
        engine.Tick(held, .016f, 0);
        Assert.True(engine.CurrentFrame.WasPressed(open));
        engine.SetMaps(ui);
        engine.Tick(held, .016f, .016f);
        Assert.False(engine.CurrentFrame.WasPressed(confirm));
        engine.SetMaps(ui);
        engine.Tick(held, .016f, .032f);
        Assert.False(engine.CurrentFrame.WasPressed(confirm));
    }
}
