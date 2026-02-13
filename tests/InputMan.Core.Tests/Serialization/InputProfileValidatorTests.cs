using InputMan.Core.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace InputMan.Core.Tests.Serialization
{
    public class InputProfileValidatorTests
    {
        [Fact]
        public void Validate_NullProfile_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                InputProfileValidator.Validate(null!));
        }

        [Fact]
        public void Validate_MapIdMismatchesKey_Throws()
        {
            var profile = new InputProfile
            {
                Maps = new Dictionary<string, ActionMapDefinition>
                {
                    ["Gameplay"] = new ActionMapDefinition
                    {
                        Id = new ActionMapId("WrongName") // mismatch!
                    }
                }
            };

            var ex = Assert.Throws<InvalidOperationException>(() =>
                InputProfileValidator.Validate(profile));

            Assert.Contains("does not match", ex.Message);
        }

        [Fact]
        public void Validate_ButtonTriggerWithAxisOutput_IsValid()
        {
            // ButtonAxis is valid (WASD)
            var profile = new InputProfile
            {
                Maps = new Dictionary<string, ActionMapDefinition>
                {
                    ["Test"] = new ActionMapDefinition
                    {
                        Id = new ActionMapId("Test"),
                        Bindings =
                        [
                            new Binding
                    {
                        Name = "Test",
                        Trigger = new BindingTrigger
                        {
                            Control = new ControlKey(DeviceKind.Keyboard, 0, 1),
                            Type = TriggerType.Button
                        },
                        Output = new AxisOutput(new AxisId("Axis"), 1f)
                    }
                        ]
                    }
                }
            };

            // Should not throw
            InputProfileValidator.Validate(profile);
        }

        [Fact]
        public void Validate_AxisTriggerWithActionOutput_Throws()
        {
            var profile = new InputProfile
            {
                Maps = new Dictionary<string, ActionMapDefinition>
                {
                    ["Test"] = new ActionMapDefinition
                    {
                        Id = new ActionMapId("Test"),
                        Bindings =
                        [
                            new Binding
                    {
                        Name = "Test",
                        Trigger = new BindingTrigger
                        {
                            Control = new ControlKey(DeviceKind.Keyboard, 0, 1),
                            Type = TriggerType.Axis // Axis trigger
                        },
                        Output = new ActionOutput(new ActionId("Action")) // Action output - INVALID!
                    }
                        ]
                    }
                }
            };

            Assert.Throws<InvalidOperationException>(() =>
                InputProfileValidator.Validate(profile));
        }

    }
}
