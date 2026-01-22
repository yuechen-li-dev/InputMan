using System;
using System.Collections.Generic;
using System.Linq;
using InputMan.Core.Serialization;
using Xunit;

namespace InputMan.Core.Tests;

public sealed class InputProfileJsonTests
{
    [Fact]
    public void RoundTrip_LoadSaveLoad_Preserves_ProfileStructure()
    {
        // Arrange
        var profile1 = CreateSampleProfile();

        // Act
        var json1 = InputProfileJson.Save(profile1, indented: false);
        var profile2 = InputProfileJson.Load(json1);
        var json2 = InputProfileJson.Save(profile2, indented: false);

        // Assert
        // Instead of raw JSON string equality (ordering can differ), compare a stable "flattened" representation.
        Assert.Equal(Flatten(profile1), Flatten(profile2));

        // Also ensure the second serialization still produces valid JSON that round-trips.
        var profile3 = InputProfileJson.Load(json2);
        Assert.Equal(Flatten(profile2), Flatten(profile3));
    }

    [Fact]
    public void Load_Preserves_DeltaAxis_TriggerType()
    {
        // Arrange
        var profile1 = CreateSampleProfile();
        var json = InputProfileJson.Save(profile1, indented: false);

        // Act
        var profile2 = InputProfileJson.Load(json);

        // Assert
        var gameplay = profile2.Maps["Gameplay"];
        var hasDeltaAxis = gameplay.Bindings.Any(b => b.Trigger.Type == TriggerType.DeltaAxis);
        Assert.True(hasDeltaAxis);
    }

    private static InputProfile CreateSampleProfile()
    {
        // Use arbitrary codes here (Core doesn't care what the numbers mean).
        var keyW = new ControlKey(DeviceKind.Keyboard, DeviceIndex: 0, Code: 87);
        var mouseDx = new ControlKey(DeviceKind.Mouse, DeviceIndex: 0, Code: 1001); // pretend MouseDeltaX
        var padA = new ControlKey(DeviceKind.Gamepad, DeviceIndex: 0, Code: 100);   // pretend A button

        var moveY = new ActionId("MoveY");
        var lookX = new AxisId("LookMouseX");
        var jump = new ActionId("Jump");

        var gameplay = new ActionMapDefinition
        {
            Id = new ActionMapId("Gameplay"),
            Priority = 10,
            CanConsume = false,
            Bindings =
            [
                // W -> MoveY (+1)
                new Binding
                {
                    Name = "MoveY",
                    Trigger = new BindingTrigger
                    {
                        Control = keyW,
                        Type = TriggerType.Button,
                        ButtonEdge = ButtonEdge.Down
                    },
                    Output = new ActionOutput(moveY)
                },

                // Mouse delta X -> LookMouseX (unclamped by our new engine rule)
                new Binding
                {
                    Name = "LookMouseX",
                    Trigger = new BindingTrigger
                    {
                        Control = mouseDx,
                        Type = TriggerType.DeltaAxis,
                        Threshold = 0f
                    },
                    Output = new AxisOutput(lookX, 1f)
                },

                // Gamepad A -> Jump action
                new Binding
                {
                    Name = "Jump",
                    Trigger = new BindingTrigger
                    {
                        Control = padA,
                        Type = TriggerType.Button,
                        ButtonEdge = ButtonEdge.Pressed
                    },
                    Output = new ActionOutput(jump)
                }
            ]
        };

        return new InputProfile
        {
            Maps = new Dictionary<string, ActionMapDefinition>
            {
                ["Gameplay"] = gameplay
            },
            Axis2 = new Dictionary<string, Axis2Definition>
            {
                ["LookMouse"] = new Axis2Definition
                {
                    Id = new Axis2Id("LookMouse"),
                    X = new AxisId("LookMouseX"),
                    Y = new AxisId("LookMouseY")
                }
            }
        };
    }

    private static string Flatten(InputProfile profile)
    {
        // Produce a stable, order-independent fingerprint of the profile's important semantics.
        var parts = new List<string>();

        foreach (var mapKvp in profile.Maps.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var mapKey = mapKvp.Key;
            var map = mapKvp.Value;

            parts.Add($"map:{mapKey}|id:{map.Id.Name}|prio:{map.Priority}|consume:{map.CanConsume}");

            // Bindings fingerprint
            foreach (var b in map.Bindings
                         .Select(FlattenBinding)
                         .OrderBy(s => s, StringComparer.Ordinal))
            {
                parts.Add($"  {b}");
            }
        }

        foreach (var a2Kvp in profile.Axis2.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var key = a2Kvp.Key;
            var a2 = a2Kvp.Value;
            parts.Add($"axis2:{key}|id:{a2.Id.Name}|x:{a2.X.Name}|y:{a2.Y.Name}");
        }

        return string.Join("\n", parts);
    }

    private static string FlattenBinding(Binding b)
    {
        var t = b.Trigger;
        var trig =
            $"trig:{t.Type}|dev:{t.Control.Device}|idx:{t.Control.DeviceIndex}|code:{t.Control.Code}|edge:{t.ButtonEdge}|th:{t.Threshold:0.####}";

        var output = b.Output switch
        {
            AxisOutput ax => $"out:Axis|axis:{ax.Axis.Name}|scale:{ax.Scale:0.####}",
            ActionOutput act => $"out:Action|action:{act.Action.Name}",
            _ => $"out:{b.Output.GetType().Name}"
        };

        return $"{trig}|{output}";
    }
}
