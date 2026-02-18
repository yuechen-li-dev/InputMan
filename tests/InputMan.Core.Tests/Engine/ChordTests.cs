using System.Collections.Generic;
using InputMan.Core;
using InputMan.Core.Validation;
using Xunit;

namespace InputMan.Core.Tests.Engine
{
    public class ChordTests
    {
        private const int KeyW = 87;           // arbitrary stable code
        private const int KeyLeftShift = 1001; // arbitrary stable code

        private static InputProfile MakeChordProfile(ActionId action, ControlKey primary, params ControlKey[] mods)
        {
            var map = new ActionMapDefinition
            {
                Id = new ActionMapId("Gameplay"),
                Priority = 0,
                CanConsume = false,
                Bindings =
            [
                new Binding
                {
                    Name = "Sprint",
                    Trigger = new BindingTrigger
                    {
                        Control = primary,
                        Type = TriggerType.Button,
                        ButtonEdge = ButtonEdge.Down,
                        Modifiers = mods ?? [],
                    },
                    Output = new ActionOutput(action),
                    Consume = ConsumeMode.None
                }
            ]
            };

            var profile = new InputProfile
            {
                Maps = new Dictionary<string, ActionMapDefinition>
                {
                    ["Gameplay"] = map
                }
            };

            // Optional: validate in tests so you catch profile mistakes early
            InputProfileValidator.Validate(profile);

            return profile;
        }

        [Fact]
        public void Chord_ActionDown_RequiresModifiers()
        {
            var sprint = new ActionId("Sprint");
            var shift = new ControlKey(DeviceKind.Keyboard, 0, KeyLeftShift);
            var w = new ControlKey(DeviceKind.Keyboard, 0, KeyW);

            var profile = MakeChordProfile(sprint, w, shift);
            var engine = new InputManEngine(profile);
            engine.SetMaps(new ActionMapId("Gameplay"));

            // Frame 1: W down only => not down
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool> { [w] = true },
                axes: new Dictionary<ControlKey, float>()
            ), 0.016f, 0f);

            Assert.False(engine.IsDown(sprint));

            // Frame 2: Shift + W => down
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool> { [w] = true, [shift] = true },
                axes: new Dictionary<ControlKey, float>()
            ), 0.016f, 0.016f);

            Assert.True(engine.IsDown(sprint));

            // Frame 3: release shift, keep W => should turn off
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool> { [w] = true, [shift] = false },
                axes: new Dictionary<ControlKey, float>()
            ), 0.016f, 0.032f);

            Assert.False(engine.IsDown(sprint));
        }

        [Fact]
        public void Chord_ActionReleased_FiresWhenModifierLifts()
        {
            var sprint = new ActionId("Sprint");
            var shift = new ControlKey(DeviceKind.Keyboard, 0, KeyLeftShift);
            var w = new ControlKey(DeviceKind.Keyboard, 0, KeyW);

            var profile = MakeChordProfile(sprint, w, shift);

            // Change binding edge to Released 
            var map = profile.Maps["Gameplay"];
            var old = map.Bindings[0];

            map.Bindings[0] = new Binding
            {
                Name = old.Name,
                Trigger = new BindingTrigger
                {
                    Control = old.Trigger.Control,
                    Type = old.Trigger.Type,
                    Modifiers = old.Trigger.Modifiers,
                    ButtonEdge = ButtonEdge.Released,
                    Threshold = old.Trigger.Threshold,
                },
                Output = old.Output,
                Consume = old.Consume,
            };

            var engine = new InputManEngine(profile);
            engine.SetMaps(new ActionMapId("Gameplay"));

            ActionEvent? last = null;
            engine.OnAction += e => last = e;

            // Frame 1: chord active
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool> { [w] = true, [shift] = true },
                axes: new Dictionary<ControlKey, float>()
            ), 0.016f, 0f);

            last = null;

            // Frame 2: lift modifier => should count as release
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool> { [w] = true, [shift] = false },
                axes: new Dictionary<ControlKey, float>()
            ), 0.016f, 0.016f);

            Assert.True(last.HasValue);
            Assert.Equal(sprint, last.Value.Action);
            Assert.Equal(ActionPhase.Released, last.Value.Phase);
        }

        [Fact]
        public void Chord_ButtonAxis_OnlyContributesWhileModifiersHeld()
        {
            // Arrange
            var moveY = new AxisId("MoveY");
            var shift = new ControlKey(DeviceKind.Keyboard, 0, KeyLeftShift);
            var w = new ControlKey(DeviceKind.Keyboard, 0, KeyW);

            // Binding: Shift + W => MoveY += +1 while held
            var map = new ActionMapDefinition
            {
                Id = new ActionMapId("Gameplay"),
                Priority = 0,
                CanConsume = false,
                Bindings =
        [
            new Binding
            {
                Name = "MoveFwdChord",
                Trigger = new BindingTrigger
                {
                    Control = w,
                    Type = TriggerType.Button,
                    ButtonEdge = ButtonEdge.Down, // contribute while held
                    Modifiers = [shift]
                },
                Output = new AxisOutput(moveY, Scale: 1f),
                Consume = ConsumeMode.None
            }
        ]
            };

            var profile = new InputProfile
            {
                Maps = new Dictionary<string, ActionMapDefinition>
                {
                    ["Gameplay"] = map
                }
            };

            InputProfileValidator.Validate(profile);

            var engine = new InputManEngine(profile);
            engine.SetMaps(new ActionMapId("Gameplay"));

            // Frame 1: W down only => should NOT contribute
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool> { [w] = true },
                axes: new Dictionary<ControlKey, float>()
            ), 0.016f, 0f);

            Assert.Equal(0f, engine.GetAxis(moveY));

            // Frame 2: Shift + W => should contribute +1
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool> { [w] = true, [shift] = true },
                axes: new Dictionary<ControlKey, float>()
            ), 0.016f, 0.016f);

            Assert.Equal(1f, engine.GetAxis(moveY));

            // Frame 3: release shift, keep W => should stop contributing (back to 0)
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool> { [w] = true, [shift] = false },
                axes: new Dictionary<ControlKey, float>()
            ), 0.016f, 0.032f);

            Assert.Equal(0f, engine.GetAxis(moveY));
        }

        [Fact]
        public void Chord_ActionPressed_OnlyFiresOnceWhenModifierAndPrimaryBothPressed()
        {
            // This is THE critical test - ensures no double-firing
            var sprint = new ActionId("Sprint");
            var shift = new ControlKey(DeviceKind.Keyboard, 0, KeyLeftShift);
            var w = new ControlKey(DeviceKind.Keyboard, 0, KeyW);

            var profile = MakeChordProfile(sprint, w, shift);

            // Change binding edge to Pressed
            var map = profile.Maps["Gameplay"];
            var old = map.Bindings[0];
            map.Bindings[0] = new Binding
            {
                Name = old.Name,
                Trigger = new BindingTrigger
                {
                    Control = old.Trigger.Control,
                    Type = old.Trigger.Type,
                    Modifiers = old.Trigger.Modifiers,
                    ButtonEdge = ButtonEdge.Pressed,
                    Threshold = old.Trigger.Threshold,
                },
                Output = old.Output,
                Consume = old.Consume,
            };

            var engine = new InputManEngine(profile);
            engine.SetMaps(new ActionMapId("Gameplay"));

            int pressedCount = 0;
            engine.OnAction += e =>
            {
                if (e.Phase == ActionPhase.Pressed && e.Action.Equals(sprint))
                    pressedCount++;
            };

            // Frame 1: Nothing pressed
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool>(),
                axes: new Dictionary<ControlKey, float>()
            ), 0.016f, 0f);

            Assert.Equal(0, pressedCount);
            Assert.False(engine.WasPressed(sprint));

            // Frame 2: Press W only (no modifier) => should NOT fire
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool> { [w] = true },
                axes: new Dictionary<ControlKey, float>()
            ), 0.016f, 0.016f);

            Assert.Equal(0, pressedCount);
            Assert.False(engine.WasPressed(sprint));

            // Frame 3: Add Shift (chord complete) => should fire ONCE
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool> { [w] = true, [shift] = true },
                axes: new Dictionary<ControlKey, float>()
            ), 0.016f, 0.032f);

            Assert.Equal(1, pressedCount); // ← THE CRITICAL ASSERTION
            Assert.True(engine.WasPressed(sprint));

            // Frame 4: Keep holding both => should NOT fire again
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool> { [w] = true, [shift] = true },
                axes: new Dictionary<ControlKey, float>()
            ), 0.016f, 0.048f);

            Assert.Equal(1, pressedCount); // Still only 1
            Assert.False(engine.WasPressed(sprint)); // Not pressed THIS frame

            // Frame 5: Release W => should NOT fire (it's a Pressed edge, not Released)
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool> { [shift] = true },
                axes: new Dictionary<ControlKey, float>()
            ), 0.016f, 0.064f);

            Assert.Equal(1, pressedCount); // Still only 1
            Assert.False(engine.WasPressed(sprint));
        }
    }
}