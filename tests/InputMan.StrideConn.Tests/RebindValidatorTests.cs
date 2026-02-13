using InputMan.Core;
using Stride.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputMan.StrideConn.Tests
{
    public class RebindValidatorTests
    {
        [Fact]
        public void Rebind_ConflictDetection_WorksAfterSequentialRebinds()
        {
            // This is the bug we just fixed!
            // Scenario:
            // 1. Jump is bound to Space
            // 2. Rebind Jump from Space -> J (succeeds)
            // 3. Try to rebind Jump from J -> Space (should FAIL with conflict)

            var Jump = new ActionId("Jump");
            var profile = new InputProfile
            {
                Maps = new Dictionary<string, ActionMapDefinition>
                {
                    ["Gameplay"] = new ActionMapDefinition
                    {
                        Id = new ActionMapId("Gameplay"),
                        Bindings =
                        [
                            Bind.Action(StrideKeys.K(Keys.Space), Jump, name: "Jump")
                        ]
                    }
                }
            };

            var engine = new InputManEngine(profile);

            // First rebind: Space -> J (should succeed)
            var result1 = (RebindResult?)null;
            var session1 = engine.StartRebind(new RebindRequest
            {
                Map = new ActionMapId("Gameplay"),
                BindingNameOrSlot = "Jump",
                DisallowConflictsInSameMap = true,
                CandidateButtons = [StrideKeys.K(Keys.J), StrideKeys.K(Keys.Space)]
            });
            session1.OnCompleted += r => result1 = r;

            engine.Tick(InputSnapshot.Empty, 0.016f, 0f); // seed
            engine.Tick(CreateSnapshot((Keys.J, true)), 0.016f, 0.016f);

            Assert.NotNull(result1);
            Assert.True(result1!.Succeeded);

            // Second rebind: J -> Space (should FAIL - Space is the OLD binding)
            var result2 = (RebindResult?)null;
            var session2 = engine.StartRebind(new RebindRequest
            {
                Map = new ActionMapId("Gameplay"),
                BindingNameOrSlot = "Jump",
                DisallowConflictsInSameMap = true,
                CandidateButtons = [StrideKeys.K(Keys.Space), StrideKeys.K(Keys.K)]
            });
            session2.OnCompleted += r => result2 = r;

            engine.Tick(InputSnapshot.Empty, 0.032f, 0.032f); // seed
            engine.Tick(CreateSnapshot((Keys.Space, true)), 0.048f, 0.048f);

            // THIS IS THE KEY ASSERTION - before the fix, this would pass incorrectly
            Assert.NotNull(result2);
            Assert.False(result2!.Succeeded);
            Assert.Contains("already bound", result2.Error, StringComparison.OrdinalIgnoreCase);
        }

        public InputSnapshot CreateSnapshot(params (Keys key, bool down)[] keys)
        {
            var dict = new Dictionary<ControlKey, bool>();
            foreach (var (key, down) in keys)
            {
                if (down)
                    dict[StrideKeys.K(key)] = true;
            }
            return new InputSnapshot(dict, new Dictionary<ControlKey, float>());
        }
    }
}
