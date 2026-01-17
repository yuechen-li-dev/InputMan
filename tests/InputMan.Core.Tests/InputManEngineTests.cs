using System.Collections.Generic;
using InputMan;
using Xunit;

namespace InputMan.Core.Tests;

public sealed class InputManEngineTests
{
    private static readonly ControlKey Space = new(DeviceKind.Keyboard, 0, 32);
    private static readonly ControlKey W = new(DeviceKind.Keyboard, 0, 87);
    private static readonly ControlKey S = new(DeviceKind.Keyboard, 0, 83);

    [Fact]
    public void ActionPressedEdge_Works()
    {
        var profile = new InputProfile
        {
            Maps = new()
            {
                ["Gameplay"] = new ActionMapDefinition
                {
                    Id = new ActionMapId("Gameplay"),
                    Priority = 10,
                    Bindings =
                    [
                        new Binding
                        {
                            Name = "Jump:Space",
                            Trigger = new BindingTrigger
                            {
                                Control = Space,
                                Type = TriggerType.Button,
                                ButtonEdge = ButtonEdge.Pressed,
                            },
                            Output = new ActionOutput(new ActionId("Jump")),
                            Consume = ConsumeMode.None,
                        }
                    ]
                }
            }
        };

        var im = new InputManEngine(profile);
        im.SetMaps(new ActionMapId("Gameplay"));

        // Frame 1: up
        im.Tick(new InputSnapshot(buttons: new Dictionary<ControlKey, bool> { [Space] = false }), 1/60f, 0f);
        Assert.False(im.WasPressed(new ActionId("Jump")));

        // Frame 2: press
        im.Tick(new InputSnapshot(buttons: new Dictionary<ControlKey, bool> { [Space] = true }), 1/60f, 1/60f);
        Assert.True(im.WasPressed(new ActionId("Jump")));
        Assert.True(im.IsDown(new ActionId("Jump")));

        // Frame 3: still down (Pressed should be false)
        im.Tick(new InputSnapshot(buttons: new Dictionary<ControlKey, bool> { [Space] = true }), 1/60f, 2/60f);
        Assert.False(im.WasPressed(new ActionId("Jump")));
        Assert.True(im.IsDown(new ActionId("Jump")));

        // Frame 4: release
        im.Tick(new InputSnapshot(buttons: new Dictionary<ControlKey, bool> { [Space] = false }), 1/60f, 3/60f);
        Assert.True(im.WasReleased(new ActionId("Jump")));
        Assert.False(im.IsDown(new ActionId("Jump")));
    }

    [Fact]
    public void Consumption_UiBeatsGameplay_OnSameControl()
    {
        var profile = new InputProfile
        {
            Maps = new()
            {
                ["UI"] = new ActionMapDefinition
                {
                    Id = new ActionMapId("UI"),
                    Priority = 100,
                    CanConsume = true,
                    Bindings =
                    [
                        new Binding
                        {
                            Name = "UISelect:Space",
                            Trigger = new BindingTrigger
                            {
                                Control = Space,
                                Type = TriggerType.Button,
                                ButtonEdge = ButtonEdge.Pressed,
                            },
                            Output = new ActionOutput(new ActionId("UISelect")),
                            Consume = ConsumeMode.ControlOnly,
                        }
                    ]
                },
                ["Gameplay"] = new ActionMapDefinition
                {
                    Id = new ActionMapId("Gameplay"),
                    Priority = 10,
                    CanConsume = true,
                    Bindings =
                    [
                        new Binding
                        {
                            Name = "Jump:Space",
                            Trigger = new BindingTrigger
                            {
                                Control = Space,
                                Type = TriggerType.Button,
                                ButtonEdge = ButtonEdge.Pressed,
                            },
                            Output = new ActionOutput(new ActionId("Jump")),
                            Consume = ConsumeMode.ControlOnly,
                        }
                    ]
                }
            }
        };

        var im = new InputManEngine(profile);
        im.SetMaps(new ActionMapId("Gameplay"), new ActionMapId("UI"));

        im.Tick(new InputSnapshot(buttons: new Dictionary<ControlKey, bool> { [Space] = true }), 1/60f, 0f);

        Assert.True(im.WasPressed(new ActionId("UISelect")));
        Assert.False(im.WasPressed(new ActionId("Jump")));
    }

    [Fact]
    public void AxisComposite_WasdAccumulatesAndClamps()
    {
        var moveY = new AxisId("MoveY");

        var profile = new InputProfile
        {
            Maps = new()
            {
                ["Gameplay"] = new ActionMapDefinition
                {
                    Id = new ActionMapId("Gameplay"),
                    Priority = 10,
                    Bindings =
                    [
                        // W contributes +1
                        new Binding
                        {
                            Name = "MoveY:W",
                            Trigger = new BindingTrigger { Control = W, Type = TriggerType.Button, ButtonEdge = ButtonEdge.Down },
                            Output = new AxisOutput(moveY, +1f),
                            Consume = ConsumeMode.None,
                        },
                        // S contributes -1
                        new Binding
                        {
                            Name = "MoveY:S",
                            Trigger = new BindingTrigger { Control = S, Type = TriggerType.Button, ButtonEdge = ButtonEdge.Down },
                            Output = new AxisOutput(moveY, -1f),
                            Consume = ConsumeMode.None,
                        },
                    ]
                }
            }
        };

        var im = new InputManEngine(profile);
        im.SetMaps(new ActionMapId("Gameplay"));

        // Hold W => +1
        im.Tick(new InputSnapshot(buttons: new Dictionary<ControlKey, bool> { [W] = true, [S] = false }), 1/60f, 0f);
        Assert.Equal(1f, im.GetAxis(moveY));

        // Hold W+S => cancels to 0
        im.Tick(new InputSnapshot(buttons: new Dictionary<ControlKey, bool> { [W] = true, [S] = true }), 1/60f, 1/60f);
        Assert.Equal(0f, im.GetAxis(moveY));

        // Neither => 0
        im.Tick(new InputSnapshot(buttons: new Dictionary<ControlKey, bool> { [W] = false, [S] = false }), 1/60f, 2/60f);
        Assert.Equal(0f, im.GetAxis(moveY));
    }

}
