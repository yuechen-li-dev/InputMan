using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace InputMan.Core.Tests.Processors
{
    public class ProcessorTests
    {
        [Fact]
        public void DeadzoneProcessor_FiltersSmallValues()
        {
            var processor = new DeadzoneProcessor(0.2f);

            Assert.Equal(0f, processor.Process(0.1f));   // below deadzone
            Assert.Equal(0f, processor.Process(-0.15f)); // below deadzone
            Assert.Equal(1f, processor.Process(1f));     // at max (remapped)

            // At deadzone edge, should start at 0 and remap to 1
            var midpoint = processor.Process(0.6f); // halfway between 0.2 and 1.0
            Assert.Equal(0.5f, midpoint, precision: 2);
        }

        [Fact]
        public void InvertProcessor_FlipsSign()
        {
            var processor = new InvertProcessor();

            Assert.Equal(-1f, processor.Process(1f));
            Assert.Equal(1f, processor.Process(-1f));
            Assert.Equal(0f, processor.Process(0f));
        }

        [Fact]
        public void ScaleProcessor_MultipliesValue()
        {
            var processor = new ScaleProcessor(2.5f);

            Assert.Equal(2.5f, processor.Process(1f));
            Assert.Equal(5f, processor.Process(2f));
            Assert.Equal(-2.5f, processor.Process(-1f));
        }

        [Fact]
        public void Binding_WithProcessors_AppliesInOrder()
        {
            // Test that processors chain correctly
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
                        Trigger = new BindingTrigger
                        {
                            Control = new ControlKey(DeviceKind.Gamepad, 0, 100),
                            Type = TriggerType.Axis
                        },
                        Output = new AxisOutput(new AxisId("TestAxis"), Scale: 1f),
                        Processors =
                        [
                            new DeadzoneProcessor(0.2f),
                            new ScaleProcessor(2f),
                            new InvertProcessor()
                        ]
                    }
                        ]
                    }
                }
            };

            var engine = new InputManEngine(profile);
            engine.SetMaps(new ActionMapId("Test"));

            // Input 0.6 -> deadzone gives ~0.5 -> scale gives 1.0 -> invert gives -1.0
            var snapshot = new InputSnapshot(
                new Dictionary<ControlKey, bool>(),
                new Dictionary<ControlKey, float>
                {
                    [new ControlKey(DeviceKind.Gamepad, 0, 100)] = 0.6f
                }
            );

            engine.Tick(snapshot, 0.016f, 0f);

            var result = engine.GetAxis(new AxisId("TestAxis"));
            Assert.Equal(-1f, result, precision: 1);
        }
    }
}
