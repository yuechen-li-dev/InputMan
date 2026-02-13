using InputMan.Core;
using Stride.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace InputMan.StrideConn.Tests
{
    public class RebindingTests
    {
        [Fact]
        public void Rebind_Timeout_CompletesWithError()
        {
            var engine = CreateEngineWithJumpBinding();
            var completed = (RebindResult?)null;

            var session = engine.StartRebind(new RebindRequest
            {
                Map = new ActionMapId("Gameplay"),
                BindingNameOrSlot = "Jump:Keyboard[0]:18",
                Timeout = TimeSpan.FromMilliseconds(50) // very short
            });

            session.OnCompleted += r => completed = r;

            // Seed frame
            engine.Tick(InputSnapshot.Empty, 0.016f, 0f);

            // Wait past timeout
            System.Threading.Thread.Sleep(60);
            engine.Tick(InputSnapshot.Empty, 0.016f, 0.060f);

            Assert.NotNull(completed);
            Assert.False(completed!.Succeeded);
            Assert.Contains("timed out", completed.Error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Rebind_ConflictDetection_PreventsBindingToUsedKey()
        {
            // Test the fix we just applied!
            var Jump = new ActionId("Jump");
            var Crouch = new ActionId("Crouch");

            var profile = new InputProfile
            {
                Maps = new Dictionary<string, ActionMapDefinition>
                {
                    ["Gameplay"] = new ActionMapDefinition
                    {
                        Id = new ActionMapId("Gameplay"),
                        Bindings =
                        [
                            Bind.Action(StrideKeys.K(Keys.Space), Jump, name: "Jump"),
                    Bind.Action(StrideKeys.K(Keys.C), Crouch, name: "Crouch")
                        ]
                    }
                }
            };

            var engine = new InputManEngine(profile);
            var completed = (RebindResult?)null;

            // Try to rebind Jump to C (already used by Crouch)
            var session = engine.StartRebind(new RebindRequest
            {
                Map = new ActionMapId("Gameplay"),
                BindingNameOrSlot = "Jump",
                DisallowConflictsInSameMap = true,
                CandidateButtons = [StrideKeys.K(Keys.C), StrideKeys.K(Keys.Space)]
            });

            session.OnCompleted += r => completed = r;

            // Seed + press C
            engine.Tick(InputSnapshot.Empty, 0.016f, 0f);
            engine.Tick(new InputSnapshot(
                new Dictionary<ControlKey, bool> { [StrideKeys.K(Keys.C)] = true },
                new Dictionary<ControlKey, float>()
            ), 0.016f, 0.016f);

            Assert.NotNull(completed);
            Assert.False(completed!.Succeeded);
            Assert.Contains("already bound", completed.Error, StringComparison.OrdinalIgnoreCase);
        }

/*        [Fact]
        public void Rebind_Cancel_StopsSession()
        {
            var engine = CreateEngineWithJumpBinding();
            var completed = (RebindResult?)null;

            var session = engine.StartRebind(CreateSimpleRebindRequest());
            session.OnCompleted += r => completed = r;

            engine.Tick(InputSnapshot.Empty, 0.016f, 0f); // seed

            session.Cancel();

            Assert.NotNull(completed);
            Assert.False(completed!.Succeeded);
            Assert.Contains("cancel", completed.Error, StringComparison.OrdinalIgnoreCase);
        }*/

        [Fact]
        public void Rebind_ForbiddenControls_RejectsBinding()
        {
            var engine = CreateEngineWithJumpBinding();
            var completed = (RebindResult?)null;

            var session = engine.StartRebind(new RebindRequest
            {
                Map = new ActionMapId("Gameplay"),
                BindingNameOrSlot = "Jump",
                ForbiddenControls = new HashSet<ControlKey>
        {
            new(DeviceKind.Keyboard, 0, (int)Keys.Escape)
        },
                CandidateButtons = [
                    new(DeviceKind.Keyboard, 0, (int)Keys.Escape),
            new(DeviceKind.Keyboard, 0, (int)Keys.Space)
                ]
            });

            session.OnCompleted += r => completed = r;

            // Try to bind to Escape (forbidden)
            engine.Tick(InputSnapshot.Empty, 0.016f, 0f);
            engine.Tick(new InputSnapshot(
                new Dictionary<ControlKey, bool>
                {
                    [new(DeviceKind.Keyboard, 0, (int)Keys.Escape)] = true
                },
                new Dictionary<ControlKey, float>()
            ), 0.016f, 0.016f);

            Assert.NotNull(completed);
            Assert.False(completed!.Succeeded);
            Assert.Contains("not allowed", completed.Error, StringComparison.OrdinalIgnoreCase);
        }

        public InputManEngine CreateEngineWithJumpBinding()
        {
            var profile = new InputProfile
            {
                Maps = new Dictionary<string, ActionMapDefinition>
                {
                    ["Gameplay"] = new ActionMapDefinition
                    {
                        Id = new ActionMapId("Gameplay"),
                        Bindings = [Bind.Action(StrideKeys.K(Keys.Space), new ActionId("Jump"))]
                    }
                }
            };
            return new InputManEngine(profile);
        }
    }
}
