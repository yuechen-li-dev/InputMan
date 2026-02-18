using InputMan.Core.Rebind;
using InputMan.Core.Validation;
using Xunit;

namespace InputMan.Core.Tests
{
    public class RebindChordTests
    {
        // Arbitrary stable key codes for core tests (not Stride codes)
        private const int KeyShift = 1001;
        private const int KeyK = 75;
        private const int KeySpace = 32;

        [Fact]
        public void Rebind_CapturesChord_WhenModifierPressedThenPrimaryPressed()
        {
            var shift = new ControlKey(DeviceKind.Keyboard, 0, KeyShift);
            var k = new ControlKey(DeviceKind.Keyboard, 0, KeyK);
            var space = new ControlKey(DeviceKind.Keyboard, 0, KeySpace);

            var profile = new InputProfile
            {
                Maps = new Dictionary<string, ActionMapDefinition>
                {
                    ["Gameplay"] = new ActionMapDefinition
                    {
                        Id = new ActionMapId("Gameplay"),
                        Priority = 0,
                        CanConsume = false,
                        Bindings = new List<Binding>
                    {
                        new Binding
                        {
                            Name = "Jump",
                            Trigger = new BindingTrigger
                            {
                                Control = space,
                                Type = TriggerType.Button,
                                ButtonEdge = ButtonEdge.Pressed,
                                Modifiers = Array.Empty<ControlKey>()
                            },
                            Output = new ActionOutput(new ActionId("Jump")),
                            Consume = ConsumeMode.None
                        }
                    }
                    }
                }
            };

            InputProfileValidator.Validate(profile);

            var engine = new InputManEngine(profile);
            engine.SetMaps(new ActionMapId("Gameplay"));

            RebindResult? completed = null;

            var request = new RebindRequest
            {
                Map = new ActionMapId("Gameplay"),
                BindingNameOrSlot = "Jump",
                CandidateButtons = new[] { shift, k, space },

                AllowChord = true,
                ModifierControls = new HashSet<ControlKey> { shift },
                MaxModifiers = 2,

                DisallowConflictsInSameMap = false,
                Timeout = TimeSpan.FromSeconds(5),
            };

            var session = engine.StartRebind(request);
            session.OnCompleted += r => completed = r;

            // Tick 0: seed (no input) so held keys don't insta-bind
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool>(),
                axes: new Dictionary<ControlKey, float>()),
                deltaTimeSeconds: 1f / 60f,
                timeSeconds: 0f);

            // Tick 1: press modifier only (should NOT complete)
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool> { [shift] = true },
                axes: new Dictionary<ControlKey, float>()),
                deltaTimeSeconds: 1f / 60f,
                timeSeconds: 1f / 60f);

            Assert.Null(completed);
            Assert.True(engine.IsRebinding);

            // Tick 2: press primary while modifier held (should complete with chord)
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool> { [shift] = true, [k] = true },
                axes: new Dictionary<ControlKey, float>()),
                deltaTimeSeconds: 1f / 60f,
                timeSeconds: 2f / 60f);

            Assert.NotNull(completed);
            Assert.True(completed!.Succeeded);
            Assert.Equal(k, completed.BoundControl);

            Assert.NotNull(completed.BoundModifiers);
            Assert.Single(completed.BoundModifiers!);
            Assert.Equal(shift, completed.BoundModifiers![0]);

            var updated = engine.ExportProfile();
            var updatedBinding = updated.Maps["Gameplay"].Bindings[0];

            Assert.Equal(k, updatedBinding.Trigger.Control);
            Assert.Single(updatedBinding.Trigger.Modifiers);
            Assert.Equal(shift, updatedBinding.Trigger.Modifiers[0]);
        }

        [Fact]
        public void Rebind_ModifierNotListed_IsCapturedAsPrimary()
        {
            var shift = new ControlKey(DeviceKind.Keyboard, 0, KeyShift);
            var k = new ControlKey(DeviceKind.Keyboard, 0, KeyK);
            var space = new ControlKey(DeviceKind.Keyboard, 0, KeySpace);

            var profile = new InputProfile
            {
                Maps = new Dictionary<string, ActionMapDefinition>
                {
                    ["Gameplay"] = new ActionMapDefinition
                    {
                        Id = new ActionMapId("Gameplay"),
                        Bindings = new List<Binding>
                    {
                        new Binding
                        {
                            Name = "Jump",
                            Trigger = new BindingTrigger { Control = space, Type = TriggerType.Button, ButtonEdge = ButtonEdge.Pressed },
                            Output = new ActionOutput(new ActionId("Jump")),
                            Consume = ConsumeMode.None
                        }
                    }
                    }
                }
            };

            InputProfileValidator.Validate(profile);

            var engine = new InputManEngine(profile);
            engine.SetMaps(new ActionMapId("Gameplay"));

            RebindResult? completed = null;

            var request = new RebindRequest
            {
                Map = new ActionMapId("Gameplay"),
                BindingNameOrSlot = "Jump",
                CandidateButtons = new[] { shift, k, space },

                AllowChord = true,
                ModifierControls = new HashSet<ControlKey> { /* empty on purpose */ },
                MaxModifiers = 2,
                DisallowConflictsInSameMap = false,
                Timeout = TimeSpan.FromSeconds(5),
            };

            var session = engine.StartRebind(request);
            session.OnCompleted += r => completed = r;

            engine.Tick(new InputSnapshot(new Dictionary<ControlKey, bool>(), new Dictionary<ControlKey, float>()), 1f / 60f, 0f);

            // Shift is NOT a modifier key here, so pressing it should bind it as primary.
            engine.Tick(new InputSnapshot(
                buttons: new Dictionary<ControlKey, bool> { [shift] = true },
                axes: new Dictionary<ControlKey, float>()),
                1f / 60f,
                1f / 60f);

            Assert.NotNull(completed);
            Assert.True(completed!.Succeeded);
            Assert.Equal(shift, completed.BoundControl);
            Assert.True(completed.BoundModifiers is null || completed.BoundModifiers.Count == 0);
        }
    }
}